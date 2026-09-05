using System;
using System.Collections.Generic;
using System.Diagnostics;
using Eclipse.Models;
using Eclipse.Service.Search;
using Xunit;
using Xunit.Abstractions;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// VER-SEARCH-018 - the per-keystroke budget, measured over the real path: a keystroke goes
    /// into SearchSession, which parses the query, calls SearchEngine, intersects posting lists,
    /// scores the survivors and republishes its result state.
    ///
    /// The target is 16 ms, so the live row updates within one frame
    /// (docs/plans/text-search.md 5.6). The assertions here are deliberately far looser than
    /// that: a shared build agent under load is not a television, and a test that fails on a
    /// busy machine teaches people to ignore it. The number that matters is the one printed to
    /// the test output, which a person reads.
    ///
    /// The library is synthetic and larger than most real ones - 100,000 games, where the forum
    /// requests that motivated this feature describe "several thousand". Titles are built to be
    /// awkward rather than uniform: heavy prefix sharing, so a short query has a genuinely large
    /// candidate set to intersect and rank.
    /// </summary>
    public class SearchPerformanceTests
    {
        private const int LibrarySize = 100000;

        /// <summary>
        /// Generous next to the 16 ms target. This exists to catch an accidental order of
        /// magnitude - a linear scan reintroduced, a per-keystroke rebuild - not to police
        /// milliseconds on someone's laptop.
        /// </summary>
        private const double SmokeBudgetMilliseconds = 100;

        private readonly ITestOutputHelper output;

        public SearchPerformanceTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private sealed class ReadySource : ISearchIndexSource
        {
            public SearchAvailability Availability => SearchAvailability.Ready;
            public ISearchIndex Index { get; set; }
        }

        /// <summary>
        /// A library whose first words are spread across the alphabet, the way a real one is.
        /// This is the number to quote.
        /// </summary>
        private static List<SearchableGame> BuildLibrary()
        {
            string[] leaders =
            {
                "Super", "Sonic", "Star", "Mega", "Final", "Castle", "Dragon", "Metal",
                "Battle", "Ninja", "Alien", "Crystal", "Golden", "Iron", "Jungle", "King",
                "Legend", "Ocean", "Phantom", "Quantum", "Rocket", "Turbo", "Ultra", "Viper",
                "Wizard", "Zero"
            };

            return Build(leaders);
        }

        /// <summary>
        /// The adversarial case: every title begins with the same letter, so one character
        /// selects the entire library. No real library looks like this - it is here to say what
        /// the ceiling is when a single keystroke has nothing to narrow with.
        /// </summary>
        private static List<SearchableGame> BuildWorstCaseLibrary()
        {
            string[] leaders = { "Super", "Sonic", "Star", "Street", "Space", "Shadow", "Silent", "Samurai" };

            return Build(leaders);
        }

        private static List<SearchableGame> Build(string[] leaders)
        {
            string[] middles = { "Adventure", "Fighter", "Racing", "Quest", "Legend", "Warrior", "Force", "Blast" };
            string[] tails = { "Deluxe", "Championship", "Edition", "Collection", "Remastered", "2", "III", "X" };

            List<SearchableGame> games = new List<SearchableGame>(LibrarySize);

            for (int index = 0; index < LibrarySize; index++)
            {
                string title = string.Join(" ",
                    leaders[index % leaders.Length],
                    middles[(index / leaders.Length) % middles.Length],
                    tails[(index / (leaders.Length * middles.Length)) % tails.Length],
                    index.ToString());

                games.Add(new SearchableGame(index, title, index % (SearchableGame.MaxPopularity + 1)));
            }

            return games;
        }

        [Fact]
        public void A_keystroke_stays_within_the_frame_budget_on_a_very_large_library()
        {
            List<SearchableGame> games = BuildLibrary();

            Stopwatch buildTimer = Stopwatch.StartNew();
            TitleIndex index = TitleIndex.Build(games);
            buildTimer.Stop();

            output.WriteLine($"library      : {games.Count:N0} games");
            output.WriteLine($"index build  : {buildTimer.ElapsedMilliseconds} ms, {index.TermCount:N0} terms");
            output.WriteLine("");

            ISearchIndexSource source = new ReadySource { Index = index };

            // A realistic search, typed one character at a time. The early characters are the
            // expensive ones - the candidate set is largest when the least has been typed, and in
            // this library every title begins with S, so the first keystroke is the worst case
            // there is: a hundred thousand candidates to intersect, score and rank.
            const string typed = "sonic adventure";

            // Cold, on a session that has never run a query - so this pass carries the JIT of the
            // whole engine as well as the work. It is the honest number for the very first
            // keystroke of a Big Box run, and it is reported separately for exactly that reason.
            double coldWorst = TypeAndMeasure(source, typed, "cold");

            output.WriteLine("");

            // Warm, which is every keystroke after the first. This is what the 16 ms target is
            // about: the cost the user feels repeatedly.
            double warmWorst = TypeAndMeasure(source, typed, "warm");

            output.WriteLine("");
            output.WriteLine($"cold worst   : {coldWorst:0.00} ms   (first keystroke of a session - includes JIT)");
            output.WriteLine($"warm worst   : {warmWorst:0.00} ms   (target 16 ms, smoke budget {SmokeBudgetMilliseconds} ms)");

            Assert.True(warmWorst < SmokeBudgetMilliseconds,
                        $"Worst warm keystroke took {warmWorst:0.00} ms, past the {SmokeBudgetMilliseconds} ms smoke budget.");
        }

        /// <summary>
        /// The ceiling, on a library where one character selects everything. Reported rather than
        /// asserted against the target: it is a shape no real library has, and what it measures
        /// is the cost of ranking a candidate set the size of the whole library.
        ///
        /// This is the measurement behind the note in the stage 2 report about a broad
        /// single-character query - see the observation there before optimising anything.
        /// </summary>
        [Fact]
        public void The_ceiling_for_a_single_character_over_a_whole_library_is_reported()
        {
            TitleIndex index = TitleIndex.Build(BuildWorstCaseLibrary());
            ISearchIndexSource source = new ReadySource { Index = index };

            SearchSession warmUp = new SearchSession(source, SearchKeyboardLayout.Alphabetical);
            warmUp.Append('s');

            SearchSession session = new SearchSession(source, SearchKeyboardLayout.Alphabetical);

            Stopwatch keystroke = Stopwatch.StartNew();
            session.Append('s');
            keystroke.Stop();

            output.WriteLine($"one character selecting the whole library: " +
                             $"{keystroke.Elapsed.TotalMilliseconds:0.00} ms for {session.ResultCount:N0} shown " +
                             $"of {LibrarySize:N0} candidates");

            Assert.True(keystroke.Elapsed.TotalMilliseconds < SmokeBudgetMilliseconds,
                        $"Took {keystroke.Elapsed.TotalMilliseconds:0.00} ms, past the {SmokeBudgetMilliseconds} ms smoke budget.");
        }

        /// <summary>
        /// Types a query one character at a time on a fresh session, reporting each keystroke.
        /// Returns the worst.
        /// </summary>
        private double TypeAndMeasure(ISearchIndexSource source, string typed, string label)
        {
            SearchSession session = new SearchSession(source, SearchKeyboardLayout.Alphabetical);

            double worst = 0;
            double total = 0;

            foreach (char character in typed)
            {
                Stopwatch keystroke = Stopwatch.StartNew();
                session.Append(character);
                keystroke.Stop();

                double milliseconds = keystroke.Elapsed.TotalMilliseconds;
                total += milliseconds;
                worst = Math.Max(worst, milliseconds);

                output.WriteLine($"  {label}  \"{session.Query,-16}\" {milliseconds,8:0.00} ms  {session.ResultCount,6:N0} results");
            }

            output.WriteLine($"  {label}  average {total / typed.Length:0.00} ms");
            return worst;
        }

        /// <summary>
        /// Deleting is the other half of typing, and it widens rather than narrows - so the
        /// candidate sets get bigger as the user backs out, which is the direction that would
        /// hurt if anything were being recomputed badly.
        /// </summary>
        [Fact]
        public void Backing_out_of_a_query_stays_within_budget()
        {
            TitleIndex index = TitleIndex.Build(BuildLibrary());

            SearchSession session = new SearchSession(new ReadySource { Index = index },
                                                     SearchKeyboardLayout.Alphabetical);

            foreach (char character in "sonic adventure")
            {
                session.Append(character);
            }

            double worst = 0;

            while (session.HasQuery)
            {
                Stopwatch keystroke = Stopwatch.StartNew();
                session.Backspace();
                keystroke.Stop();

                worst = Math.Max(worst, keystroke.Elapsed.TotalMilliseconds);
            }

            output.WriteLine($"worst delete : {worst:0.00} ms");

            Assert.True(worst < SmokeBudgetMilliseconds,
                        $"Worst backspace took {worst:0.00} ms, past the {SmokeBudgetMilliseconds} ms smoke budget.");
        }

        /// <summary>
        /// Committing materialises the whole ranked set rather than the capped row, so it is the
        /// one operation allowed to be slower - but it happens once, on a button press, not on
        /// every keystroke.
        /// </summary>
        [Fact]
        public void Committing_a_broad_search_is_reported()
        {
            TitleIndex index = TitleIndex.Build(BuildLibrary());

            SearchSession session = new SearchSession(new ReadySource { Index = index },
                                                     SearchKeyboardLayout.Alphabetical);

            foreach (char character in "s")
            {
                session.Append(character);
            }

            Stopwatch commit = Stopwatch.StartNew();
            IReadOnlyList<SearchHit> all = session.AllResults();
            commit.Stop();

            output.WriteLine($"commit       : {commit.ElapsedMilliseconds} ms for {all.Count:N0} results");

            Assert.NotEmpty(all);
        }
    }
}
