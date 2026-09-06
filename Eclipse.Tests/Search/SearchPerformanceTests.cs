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
            public FacetIndex Facets => FacetIndex.Empty;
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

        // ------------------------------------------------------------------
        // VER-SEARCH-027 - suggestion evaluation on a library with high facet
        // cardinality.
        //
        // This is the measurement the metadata filter plan calls for, and the one part of that
        // design whose cost scales with the number of distinct metadata values rather than with
        // the size of the library. SuggestionRanker scans every term and intersects the survivors
        // against the current filter set, on every keystroke.
        //
        // WHAT IT FOUND, AND WHAT WAS DONE ABOUT IT.
        //
        // Suggesting inside a filter first measured at 30-67 ms - four times the frame budget.
        // FilterSet.IntersectCount was merging both lists end to end, so a term carrying ten
        // games cost a full walk of a five thousand game filter set, seven thousand times over.
        // Searching the short list into the long one instead took the worst case from 66.7 ms to
        // 4.1 ms. A second pass removed ten thousand string joins per keystroke by holding
        // FacetTerm.NormalizedValue rather than rebuilding it. Both were measured, not guessed.
        //
        // WHAT REMAINS, AND WHY IT IS LEFT ALONE. The unfiltered pass still shows one query in
        // six at around 20 ms, and *which* query moves between runs - so it is a garbage
        // collection of the several thousand Suggestion objects that pass allocates, not the cost
        // of any particular query. It happens on a library with five thousand developers and two
        // thousand series, which is far past anything real, and the filtered case that the
        // feature actually leans on is uniformly under 4 ms. Reducing the allocation is possible;
        // there is no evidence yet that it is worth doing.
        // ------------------------------------------------------------------

        private const int FacetLibrarySize = 50000;

        /// <summary>
        /// A library with far more distinct metadata values than a real one: five thousand
        /// developers, three thousand publishers, two thousand series. A large real library has
        /// hundreds of each.
        /// </summary>
        private static FacetIndex BuildHighCardinalityFacets()
        {
            string[] genres =
            {
                "Action", "Adventure", "Platform", "Puzzle", "RPG", "Racing", "Shooter", "Sports",
                "Strategy", "Simulation", "Fighting", "Stealth", "Survival", "Sandbox", "Rhythm",
                "Football", "Soccer", "Hockey", "Basketball", "Baseball"
            };

            List<GameFacets> games = new List<GameFacets>(FacetLibrarySize);

            for (int index = 0; index < FacetLibrarySize; index++)
            {
                games.Add(new GameFacets(index, new[]
                {
                    new FacetValue(ListCategoryType.Platform, "Platform " + (index % 30)),
                    new FacetValue(ListCategoryType.ReleaseYear, (1980 + (index % 40)).ToString()),
                    new FacetValue(ListCategoryType.Developer, "Studio " + (index % 5000)),
                    new FacetValue(ListCategoryType.Publisher, "Publisher " + (index % 3000)),
                    new FacetValue(ListCategoryType.Series, "Series " + (index % 2000)),
                    new FacetValue(ListCategoryType.Genre, genres[index % genres.Length]),
                    new FacetValue(ListCategoryType.Genre, genres[(index / 7) % genres.Length])
                }));
            }

            return FacetIndex.Build(games);
        }

        [Fact]
        public void Suggesting_stays_within_budget_on_a_library_with_many_metadata_values()
        {
            Stopwatch buildTimer = Stopwatch.StartNew();
            FacetIndex facets = BuildHighCardinalityFacets();
            buildTimer.Stop();

            output.WriteLine($"library      : {FacetLibrarySize:N0} games");
            output.WriteLine($"facet build  : {buildTimer.ElapsedMilliseconds} ms, {facets.TermCount:N0} metadata terms");
            output.WriteLine("");

            // Warm, so the reported numbers are the cost the user feels repeatedly rather than
            // the JIT of the whole ranker.
            SuggestionRanker.Rank("s", facets, null, null, 8, 3);

            double worst = MeasureSuggestions(facets, null, "no filter");

            // And again inside a filter, which is the expensive case: every candidate term is
            // intersected against the surviving set rather than answered from its own count.
            int[] filterSet = FilterSet.Apply(
                new[] { new SearchFilter(ListCategoryType.Genre, "Action") }, facets);

            output.WriteLine("");
            worst = Math.Max(worst, MeasureSuggestions(facets, filterSet, $"within {filterSet.Length:N0} games"));

            output.WriteLine("");
            output.WriteLine($"worst        : {worst:0.00} ms   (target 16 ms, smoke budget {SmokeBudgetMilliseconds} ms)");

            Assert.True(worst < SmokeBudgetMilliseconds,
                        $"Worst suggestion pass took {worst:0.00} ms, past the {SmokeBudgetMilliseconds} ms smoke budget.");
        }

        private double MeasureSuggestions(FacetIndex facets, int[] filterSet, string label)
        {
            double worst = 0;

            // One character is the worst case - it matches the most terms and narrows the least.
            foreach (string query in new[] { "s", "st", "stu", "stud", "pub", "act" })
            {
                Stopwatch timer = Stopwatch.StartNew();
                IReadOnlyList<Suggestion> offered = SuggestionRanker.Rank(query, facets, null, filterSet, 8, 3);
                timer.Stop();

                double milliseconds = timer.Elapsed.TotalMilliseconds;
                worst = Math.Max(worst, milliseconds);

                output.WriteLine($"  {label,-22} \"{query,-5}\" {milliseconds,8:0.00} ms  {offered.Count} offered");
            }

            return worst;
        }

        // ------------------------------------------------------------------
        // Stage 5a - what leave-one-out rows cost per keystroke.
        //
        // docs/plans/text-search-faceted-rows.md 6 names this as the one open risk of the
        // faceted-rows stage and says to measure before building on it. Five rows means up to
        // five searches per keystroke - but every other number in this file is UNFILTERED, and
        // these rows only exist once two constraints are applied. Which effect wins was not
        // knowable by reading, which is why this is a stage of its own.
        //
        // The measurements above use separate title and facet libraries. This one needs a single
        // library carrying both, because the whole question is what a filter does to the cost of
        // a title search over the same catalog indices.
        //
        // TWO SCENARIOS, because the first one flatters the feature. Four filters leave so few
        // games that the engine ranks almost nothing and every row is nearly free - which is
        // true, and is not the case that could hurt. A leave-one-out row is by construction
        // broader than the primary, so the cost to look for is a row whose own filter set is
        // large. Two filters, one of them a common genre, is that case.
        // ------------------------------------------------------------------

        /// <summary>
        /// Metadata cardinality close to a large real library rather than adversarial. The
        /// high-cardinality case is already measured above; what is asked here is what the
        /// feature costs in the shape a user actually has.
        /// </summary>
        private static FacetIndex BuildFacetsForTheSameLibrary()
        {
            string[] genres =
            {
                "Action", "Adventure", "Platform", "Puzzle", "RPG", "Racing", "Shooter", "Sports",
                "Strategy", "Simulation", "Fighting", "Stealth", "Survival", "Sandbox", "Rhythm",
                "Football", "Soccer", "Hockey", "Basketball", "Baseball"
            };

            List<GameFacets> games = new List<GameFacets>(LibrarySize);

            for (int index = 0; index < LibrarySize; index++)
            {
                games.Add(new GameFacets(index, new[]
                {
                    new FacetValue(ListCategoryType.Platform, "Platform " + (index % 30)),
                    new FacetValue(ListCategoryType.ReleaseYear, (1980 + (index % 40)).ToString()),
                    new FacetValue(ListCategoryType.Developer, "Studio " + (index % 800)),
                    new FacetValue(ListCategoryType.Publisher, "Publisher " + (index % 600)),
                    new FacetValue(ListCategoryType.Series, "Series " + (index % 400)),
                    new FacetValue(ListCategoryType.Genre, genres[index % genres.Length]),
                    new FacetValue(ListCategoryType.Genre, genres[(index / 7) % genres.Length])
                }));
            }

            return FacetIndex.Build(games);
        }

        /// <summary>The constraint sets the secondary rows are built from: each one left out.</summary>
        private static List<SearchFilter[]> LeaveOneOut(IReadOnlyList<SearchFilter> filters)
        {
            List<SearchFilter[]> sets = new List<SearchFilter[]>();

            for (int dropped = 0; dropped < filters.Count; dropped++)
            {
                List<SearchFilter> kept = new List<SearchFilter>(filters.Count - 1);

                for (int index = 0; index < filters.Count; index++)
                {
                    if (index != dropped)
                    {
                        kept.Add(filters[index]);
                    }
                }

                sets.Add(kept.ToArray());
            }

            return sets;
        }

        [Fact]
        public void Building_the_leave_one_out_rows_is_measured()
        {
            TitleIndex titles = TitleIndex.Build(BuildLibrary());
            FacetIndex facets = BuildFacetsForTheSameLibrary();

            // The over-narrowed case the feature exists for: four filters across four facets,
            // leaving few enough games that the user wants to know which one cost them the rest.
            double narrow = Scenario(titles, facets, "four filters, over-narrowed", new[]
            {
                new SearchFilter(ListCategoryType.Platform, "Platform 0"),
                new SearchFilter(ListCategoryType.ReleaseYear, "1980"),
                new SearchFilter(ListCategoryType.Developer, "Studio 0"),
                new SearchFilter(ListCategoryType.Genre, "Action")
            });

            // The case that can actually hurt: two filters, one of them a genre a tenth of the
            // library carries, so leaving the other one out produces a row over ten thousand
            // games wide - and the engine has to rank every one of them that the query reaches.
            double broad = Scenario(titles, facets, "two filters, one very broad", new[]
            {
                new SearchFilter(ListCategoryType.Genre, "Action"),
                new SearchFilter(ListCategoryType.Platform, "Platform 0")
            });

            double worst = Math.Max(narrow, broad);

            output.WriteLine("");
            output.WriteLine($"worst of both: {worst,7:0.00} ms   (target 16 ms, smoke budget {SmokeBudgetMilliseconds} ms)");

            Assert.True(worst < SmokeBudgetMilliseconds,
                        $"Worst keystroke with rows took {worst:0.00} ms, past the {SmokeBudgetMilliseconds} ms smoke budget.");
        }

        /// <summary>
        /// One filter set, measured with and without its leave-one-out rows. Returns the worst
        /// keystroke of the run that builds them.
        /// </summary>
        private double Scenario(TitleIndex titles, FacetIndex facets, string name, SearchFilter[] filters)
        {
            List<SearchFilter[]> secondarySets = LeaveOneOut(filters);
            int[] applied = FilterSet.Apply(filters, facets);

            output.WriteLine("");
            output.WriteLine($"=== {name}");
            output.WriteLine($"    primary row leaves {applied.Length:N0} of {LibrarySize:N0}");

            foreach (SearchFilter[] set in secondarySets)
            {
                output.WriteLine($"    without {Missing(filters, set),-12} : {FilterSet.Apply(set, facets).Length,7:N0} games");
            }

            // "s" is the expensive keystroke on this library - every title begins with one of
            // twenty-six leaders, so a single character reaches a twenty-sixth of it before the
            // filters cut in. The rest of the word is measured too, because a query that narrows
            // to nothing is the common case and should not be reported as the cost.
            const string typed = "sonic adventure";

            // Warmed first, so neither figure carries the JIT of the engine. The comparison is
            // the whole point here, and one of the two paying for the warm-up would make it a lie.
            Fanout(titles, facets, filters, secondarySets, typed, false, false);
            Fanout(titles, facets, filters, secondarySets, typed, true, false);

            output.WriteLine("");
            double primaryOnly = Fanout(titles, facets, filters, secondarySets, typed, false, true);
            output.WriteLine("");
            double withRows = Fanout(titles, facets, filters, secondarySets, typed, true, true);

            output.WriteLine("");
            output.WriteLine($"    primary only : {primaryOnly,7:0.00} ms   worst keystroke");
            output.WriteLine($"    with rows    : {withRows,7:0.00} ms   worst keystroke");
            output.WriteLine($"    cost of rows : {withRows - primaryOnly,7:0.00} ms");

            return withRows;
        }

        /// <summary>
        /// One pass of a query typed a character at a time, building either the primary row alone
        /// or every row. Returns the worst single keystroke.
        /// </summary>
        private double Fanout(TitleIndex titles,
                              FacetIndex facets,
                              IReadOnlyList<SearchFilter> filters,
                              List<SearchFilter[]> secondarySets,
                              string typed,
                              bool buildRows,
                              bool report)
        {
            double worst = 0;

            for (int length = 1; length <= typed.Length; length++)
            {
                string buffer = typed.Substring(0, length);

                Stopwatch keystroke = Stopwatch.StartNew();

                SearchQuery query = SearchQuery.Parse(buffer);
                int[] applied = FilterSet.Apply(filters, facets);
                int primary = SearchEngine.Search(query, titles, SearchEngine.DefaultMaxResults, applied).Count;

                int rows = 0;

                if (buildRows)
                {
                    // One row per filter left out, each still carrying the typed text...
                    foreach (SearchFilter[] set in secondarySets)
                    {
                        int[] surviving = FilterSet.Apply(set, facets);
                        rows += SearchEngine.Search(query, titles, SearchEngine.DefaultMaxResults, surviving).Count;
                    }

                    // ...and the row that leaves the QUERY out instead: every filter and no text.
                    // No title search at all - it is the filter set itself, already computed for
                    // the primary row a few lines above.
                    rows += Math.Min(applied.Length, SearchEngine.DefaultMaxResults);
                }

                keystroke.Stop();

                double elapsed = keystroke.Elapsed.TotalMilliseconds;
                worst = Math.Max(worst, elapsed);

                if (report)
                {
                    output.WriteLine(buildRows
                        ? $"    rows \"{buffer,-16}\" {elapsed,7:0.00} ms   {primary,3} primary, {rows,6} across the rows"
                        : $"    one  \"{buffer,-16}\" {elapsed,7:0.00} ms   {primary,3} primary");
                }
            }

            return worst;
        }

        /// <summary>Which filter a leave-one-out set is missing, for the report.</summary>
        private static string Missing(IReadOnlyList<SearchFilter> all, SearchFilter[] kept)
        {
            foreach (SearchFilter filter in all)
            {
                if (!FilterSet.Contains(kept, filter))
                {
                    return filter.Value;
                }
            }

            return "nothing";
        }
    }
}
