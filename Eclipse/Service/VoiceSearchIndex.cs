using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.Service
{
    // One game a phrase can match, with how well the phrase matched its title.
    public readonly struct VoiceMatch
    {
        public VoiceMatch(GameMatch game, TitleMatchType matchType, string convertedTitle)
        {
            Game = game;
            MatchType = matchType;
            ConvertedTitle = convertedTitle;
        }

        public GameMatch Game { get; }
        public TitleMatchType MatchType { get; }

        // the title the phrase was matched against, which sets how much of the title the
        // phrase covered and therefore the score
        public string ConvertedTitle { get; }
    }

    // Maps every recognisable phrase to the games it can match.
    //
    // This replaces the voice half of the old game bag, where each phrase-game pair was a
    // full GameMatch clone sitting in one flat collection that had to be scanned. A phrase
    // is now a dictionary key, and the per-phrase list is already deduplicated by game.
    public sealed class VoiceSearchIndex
    {
        private static readonly IReadOnlyList<VoiceMatch> NoMatches = new VoiceMatch[0];

        private readonly object setupLock = new object();
        private volatile bool isSetup;
        private Dictionary<string, List<VoiceMatch>> matchesByPhrase;
        private long buildMilliseconds;

        // How long the index took to build. Zero until it has been built.
        public long BuildMilliseconds => buildMilliseconds;

        // Every distinct phrase the recogniser should listen for.
        public IReadOnlyCollection<string> Phrases
        {
            get
            {
                EnsureSetup();
                return matchesByPhrase.Keys;
            }
        }

        // The games a recognised phrase matches, one entry per game, carrying that game's
        // best match type for the phrase.
        public IReadOnlyList<VoiceMatch> Lookup(string phrase)
        {
            EnsureSetup();

            List<VoiceMatch> matches;
            if (phrase != null && matchesByPhrase.TryGetValue(phrase, out matches))
            {
                return matches;
            }
            return NoMatches;
        }

        // Built on first access, which SpeechRecognizerService does from a background task.
        // Safe to call from any thread - a caller that arrives while a build is in flight
        // waits for it rather than starting a second one.
        private void EnsureSetup()
        {
            if (isSetup)
            {
                return;
            }

            lock (setupLock)
            {
                if (!isSetup)
                {
                    Setup();
                }
            }
        }

        private void Setup()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Dictionary<string, List<VoiceMatch>> built = new Dictionary<string, List<VoiceMatch>>(StringComparer.Ordinal);

            if (!EclipseSettingsDataProvider.Instance.EclipseSettings.EnableVoiceSearch)
            {
                Publish(built, stopwatch);
                return;
            }

            IReadOnlyList<GameMatch> games = GameCatalog.Instance.Games;

            // Parsing titles is the expensive part, so do it in parallel - but merge in
            // catalog order afterwards, so the per-phrase game order is reproducible rather
            // than depending on which thread finished first.
            List<PhraseEntry>[] entriesPerGame = new List<PhraseEntry>[games.Count];
            Parallel.For(0, games.Count, index => entriesPerGame[index] = BuildPhrasesForGame(games[index]));

            for (int gameIndex = 0; gameIndex < entriesPerGame.Length; gameIndex++)
            {
                foreach (PhraseEntry entry in entriesPerGame[gameIndex])
                {
                    List<VoiceMatch> matches;
                    if (!built.TryGetValue(entry.Phrase, out matches))
                    {
                        matches = new List<VoiceMatch>();
                        built.Add(entry.Phrase, matches);
                    }

                    matches.Add(new VoiceMatch(games[gameIndex], entry.MatchType, entry.ConvertedTitle));
                }
            }

            Publish(built, stopwatch);
        }

        // Assign the finished index before flipping the flag - isSetup is volatile, so a
        // reader that sees it set is guaranteed to see a fully built dictionary.
        private void Publish(Dictionary<string, List<VoiceMatch>> built, Stopwatch stopwatch)
        {
            stopwatch.Stop();

            matchesByPhrase = built;
            buildMilliseconds = stopwatch.ElapsedMilliseconds;
            isSetup = true;
        }

        // Every phrase one game answers to, deduplicated so a game appears once per phrase
        // carrying its best match type - the same thing the old query did with
        // "group game by game ... Max(TitleMatchType)", done once at build time instead of
        // on every recognition.
        private static List<PhraseEntry> BuildPhrasesForGame(GameMatch gameMatch)
        {
            List<PhraseEntry> entries = new List<PhraseEntry>();
            GameTitleGrammarBuilder gameTitleGrammarBuilder = new GameTitleGrammarBuilder(gameMatch.Game);

            foreach (GameTitleGrammar gameTitleGrammar in gameTitleGrammarBuilder.gameTitleGrammars)
            {
                if (!string.IsNullOrWhiteSpace(gameTitleGrammar.Title))
                {
                    entries.Add(new PhraseEntry(gameTitleGrammar.Title, TitleMatchType.FullTitleMatch, gameTitleGrammar.Title));
                }

                if (!string.IsNullOrWhiteSpace(gameTitleGrammar.MainTitle))
                {
                    entries.Add(new PhraseEntry(gameTitleGrammar.MainTitle, TitleMatchType.MainTitleMatch, gameTitleGrammar.Title));
                }

                if (!string.IsNullOrWhiteSpace(gameTitleGrammar.Subtitle))
                {
                    entries.Add(new PhraseEntry(gameTitleGrammar.Subtitle, TitleMatchType.SubtitleMatch, gameTitleGrammar.Title));
                }

                // every run of consecutive words in the title, so a partial phrase matches
                for (int i = 0; i < gameTitleGrammar.TitleWords.Count; i++)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int j = i; j < gameTitleGrammar.TitleWords.Count; j++)
                    {
                        sb.Append($"{gameTitleGrammar.TitleWords[j]} ");

                        string phrase = sb.ToString().Trim();
                        if (!GameTitleGrammar.IsNoiseWord(phrase))
                        {
                            entries.Add(new PhraseEntry(phrase, TitleMatchType.FullTitleContains, gameTitleGrammar.Title));
                        }
                    }
                }
            }

            // GroupBy keeps first-appearance order for the keys and within each group, so
            // the strongest match wins and ties fall to the phrase that was generated first
            return entries
                .GroupBy(entry => entry.Phrase, StringComparer.Ordinal)
                .Select(group => group.OrderByDescending(entry => entry.MatchType).First())
                .ToList();
        }

        private readonly struct PhraseEntry
        {
            public PhraseEntry(string phrase, TitleMatchType matchType, string convertedTitle)
            {
                Phrase = phrase;
                MatchType = matchType;
                ConvertedTitle = convertedTitle;
            }

            public string Phrase { get; }
            public TitleMatchType MatchType { get; }
            public string ConvertedTitle { get; }
        }

        #region singleton implementation
        public static VoiceSearchIndex Instance => instance;

        private static readonly VoiceSearchIndex instance = new VoiceSearchIndex();

        static VoiceSearchIndex()
        {
        }

        private VoiceSearchIndex()
        {
        }
        #endregion
    }
}
