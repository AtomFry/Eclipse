using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using Eclipse.View;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Eclipse.Diagnostics
{
    // Temporary scaffolding for the game-index refactor (Stage 0).
    //
    // Eclipse has no automated tests, and the refactor that replaces the flat game bag
    // with purpose-built indexes touches the substrate of both browsing and voice search.
    // This class captures the current behaviour in a diffable form so each refactor stage
    // can be proved behaviour-preserving by comparison rather than by inspection.
    //
    // Two files are written per run:
    //
    //   golden-<stamp>.txt   Must be byte-identical after every behaviour-preserving
    //                        stage. Contains list membership and order, the voice
    //                        grammar, and simulated voice-search results.
    //
    //   metrics-<stamp>.txt  Expected to change between stages. Contains object counts
    //                        and timings - the numbers the refactor is meant to improve.
    //
    // Disabled unless a marker file exists, so it costs nothing for ordinary users and
    // needs no rebuild to switch on. Delete this file and its call site once the refactor
    // is complete.
    public static class GameIndexDiagnostics
    {
        private const string MarkerFileName = "diagnostics.on";
        private const string OutputFolderName = "Diagnostics";

        // The live speech engine's confidence value is not reproducible between runs, so
        // voice matching is exercised directly at a fixed confidence instead.
        private const float SimulatedConfidence = 0.85f;

        // Phrases sampled evenly across the sorted grammar, plus the phrases matching the
        // most games - those exercise the grouping and ranking hardest.
        private const int SampledPhraseCount = 20;
        private const int BusiestPhraseCount = 10;

        public static bool IsEnabled
        {
            get
            {
                try
                {
                    return File.Exists(Path.Combine(DirectoryInfoHelper.Instance.EclipseFolder, MarkerFileName));
                }
                catch
                {
                    return false;
                }
            }
        }

        // Note: the voice sections wait for the background index build to finish, so a
        // capture is complete even though voice search is no longer part of startup.
        public static void Capture(MainWindowViewModel viewModel, long catalogMilliseconds, long createGameListsMilliseconds)
        {
            try
            {
                if (viewModel == null || !IsEnabled)
                {
                    return;
                }

                string outputFolder = Path.Combine(DirectoryInfoHelper.Instance.EclipseFolder, OutputFolderName);
                Directory.CreateDirectory(outputFolder);

                string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

                File.WriteAllText(Path.Combine(outputFolder, $"golden-{stamp}.txt"),
                                  BuildGolden(viewModel), Encoding.UTF8);

                File.WriteAllText(Path.Combine(outputFolder, $"metrics-{stamp}.txt"),
                                  BuildMetrics(viewModel, catalogMilliseconds, createGameListsMilliseconds), Encoding.UTF8);

                LogHelper.Log($"Index diagnostics written to {outputFolder} (stamp {stamp})");
            }
            catch (Exception ex)
            {
                // Diagnostics must never take the plugin down.
                LogHelper.LogException(ex, "capture game index diagnostics");
            }
        }

        #region golden capture

        private static string BuildGolden(MainWindowViewModel viewModel)
        {
            StringBuilder sb = new StringBuilder();
            EclipseSettings settings = EclipseSettingsDataProvider.Instance.EclipseSettings;

            sb.AppendLine("# Eclipse game index - golden capture");
            sb.AppendLine("# format-version: 1");
            sb.AppendLine("#");
            sb.AppendLine("# Byte-identical output is the acceptance criterion for behaviour-preserving");
            sb.AppendLine("# refactor stages. Counts and timings live in the matching metrics file.");
            sb.AppendLine("#");
            sb.AppendLine("# Order is recorded exactly as produced. Index construction runs in parallel, so");
            sb.AppendLine("# any collection merged without a deterministic order leaves entries that tie on");
            sb.AppendLine("# a sort key in thread-completion order. Compare with Tools/Compare-GoldenCapture.ps1,");
            sb.AppendLine("# which canonicalises ties before diffing rather than requiring a byte match.");
            sb.AppendLine();

            sb.AppendLine("[settings]");
            AppendSetting(sb, "IncludeBrokenGames", settings.IncludeBrokenGames);
            AppendSetting(sb, "IncludeHiddenGames", settings.IncludeHiddenGames);
            AppendSetting(sb, "EnableVoiceSearch", settings.EnableVoiceSearch);
            AppendSetting(sb, "ShowGameCountInList", settings.ShowGameCountInList);
            AppendSetting(sb, "RepeatGamesToFillScreen", settings.RepeatGamesToFillScreen);
            sb.AppendLine();

            AppendCategoryLists(sb, viewModel);
            AppendVoiceGrammar(sb);
            AppendVoiceMatching(sb);

            return sb.ToString();
        }

        private static void AppendCategoryLists(StringBuilder sb, MainWindowViewModel viewModel)
        {
            sb.AppendLine("[category-lists]");

            List<GameListSet> gameListSets = viewModel.GameListSets;
            if (gameListSets == null)
            {
                sb.AppendLine("(no game list sets)");
                sb.AppendLine();
                return;
            }

            foreach (GameListSet gameListSet in gameListSets)
            {
                List<GameList> gameLists = gameListSet.GameLists ?? new List<GameList>();

                sb.AppendLine($"## {gameListSet.ListCategoryType} lists={Count(gameLists.Count)} games={Count(gameListSet.TotalGameCount)}");

                foreach (GameList gameList in gameLists)
                {
                    // ListTypeValue is the raw list name; ListDescription has the game count
                    // appended when ShowGameCountInList is on, which would make the capture
                    // depend on that setting.
                    sb.AppendLine($"- {gameList.ListTypeValue} ({Count(gameList.MatchCount)}) sort={Count(gameList.SortOrder)}");

                    List<GameMatch> matchingGames = gameList.MatchingGames ?? new List<GameMatch>();
                    foreach (GameMatch gameMatch in matchingGames)
                    {
                        sb.AppendLine($"    {Describe(gameMatch)}");
                    }
                }
            }

            sb.AppendLine();
        }

        private static void AppendVoiceGrammar(StringBuilder sb)
        {
            sb.AppendLine("[voice-grammar]");

            List<string> phrases = GetGrammarPhrases();
            if (phrases == null)
            {
                sb.AppendLine("(voice search disabled)");
                sb.AppendLine();
                return;
            }

            sb.AppendLine($"phrases={Count(phrases.Count)}");
            foreach (string phrase in phrases)
            {
                sb.AppendLine(phrase);
            }

            sb.AppendLine();
        }

        private static void AppendVoiceMatching(StringBuilder sb)
        {
            sb.AppendLine("[voice-matching]");
            sb.AppendLine($"# Simulated at fixed confidence {SimulatedConfidence.ToString("0.000", CultureInfo.InvariantCulture)}.");

            List<string> phrases = GetGrammarPhrases();
            if (phrases == null)
            {
                sb.AppendLine("(voice search disabled)");
                return;
            }

            foreach (string phrase in SelectPhrasesToProbe(phrases))
            {
                List<GameMatch> matches = MatchPhrase(phrase);

                sb.AppendLine($"## \"{phrase}\" matches={Count(matches.Count)}");
                foreach (GameMatch gameMatch in matches)
                {
                    sb.AppendLine($"    {Count(gameMatch.MatchPercentage)}% {gameMatch.TitleMatchType} {Describe(gameMatch)}");
                }
            }
        }

        // Mirrors VoiceRecognitionState.RecognizeCompleted exactly - same lookup, same
        // scoring, same ordering. If that method changes, change this too.
        private static List<GameMatch> MatchPhrase(string phrase)
        {
            IReadOnlyList<VoiceMatch> voiceMatches = VoiceSearchIndex.Instance.Lookup(phrase);

            List<GameMatch> matches = new List<GameMatch>(voiceMatches.Count);
            foreach (VoiceMatch voiceMatch in voiceMatches)
            {
                GameMatch match = GameMatch.CloneForVoiceResult(voiceMatch.Game, voiceMatch.MatchType, voiceMatch.ConvertedTitle);
                match.SetupVoiceMatchPercentage(SimulatedConfidence, phrase);
                matches.Add(match);
            }

            return matches.OrderByDescending(match => match.MatchPercentage).ToList();
        }

        // A fixed, library-independent sample: evenly spaced phrases across the sorted
        // grammar, plus the phrases matching the most games.
        private static List<string> SelectPhrasesToProbe(List<string> sortedPhrases)
        {
            HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);

            if (sortedPhrases.Count > 0)
            {
                int sampleCount = Math.Min(SampledPhraseCount, sortedPhrases.Count);
                for (int i = 0; i < sampleCount; i++)
                {
                    int index = sampleCount == 1 ? 0 : (int)((long)i * (sortedPhrases.Count - 1) / (sampleCount - 1));
                    selected.Add(sortedPhrases[index]);
                }
            }

            IEnumerable<string> busiestPhrases = sortedPhrases
                .Select(phrase => new { Phrase = phrase, GameCount = VoiceSearchIndex.Instance.Lookup(phrase).Count })
                .OrderByDescending(entry => entry.GameCount)
                .ThenBy(entry => entry.Phrase, StringComparer.Ordinal)
                .Take(BusiestPhraseCount)
                .Select(entry => entry.Phrase);

            foreach (string phrase in busiestPhrases)
            {
                selected.Add(phrase);
            }

            return selected.OrderBy(phrase => phrase, StringComparer.Ordinal).ToList();
        }

        #endregion

        #region metrics

        private static string BuildMetrics(MainWindowViewModel viewModel, long catalogMilliseconds, long createGameListsMilliseconds)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("# Eclipse game index - metrics");
            sb.AppendLine("# These numbers are expected to change between refactor stages.");
            sb.AppendLine();

            int catalogGames = viewModel.gameCatalog?.Games?.Count ?? 0;

            sb.AppendLine("[objects]");
            AppendMetric(sb, "catalogGames", catalogGames);
            AppendMetric(sb, "gameFilesEntries", viewModel.gameFilesBag?.Count ?? 0);

            // GameMatch objects now exist only for real games. Voice search holds lightweight
            // index entries, and creates a decorated copy only for games a search returns.
            AppendMetric(sb, "totalGameMatchObjects", catalogGames);
            sb.AppendLine($"objectsPerGame={Ratio(catalogGames, catalogGames)}");
            sb.AppendLine();

            sb.AppendLine("[voice-index]");
            if (EclipseSettingsDataProvider.Instance.EclipseSettings.EnableVoiceSearch)
            {
                IReadOnlyCollection<string> phrases = VoiceSearchIndex.Instance.Phrases;
                int voiceIndexEntries = phrases.Sum(phrase => VoiceSearchIndex.Instance.Lookup(phrase).Count);

                AppendMetric(sb, "phrases", phrases.Count);
                AppendMetric(sb, "phraseGameEntries", voiceIndexEntries);
                sb.AppendLine($"entriesPerGame={Ratio(voiceIndexEntries, catalogGames)}");
            }
            else
            {
                sb.AppendLine("(voice search disabled)");
            }
            sb.AppendLine();

            // Category membership is references into the catalog now, not copies. These
            // counts are what used to be clone counts, so they stay comparable.
            sb.AppendLine("[category-index-entries]");
            if (viewModel.gameCatalog != null)
            {
                IEnumerable<ListCategoryType> indexed = new[]
                {
                    ListCategoryType.Platform, ListCategoryType.ReleaseYear, ListCategoryType.Genre,
                    ListCategoryType.Publisher, ListCategoryType.Developer, ListCategoryType.Series,
                    ListCategoryType.PlayMode, ListCategoryType.Playlist
                };

                IEnumerable<KeyValuePair<ListCategoryType, int>> counts = indexed
                    .Select(categoryType => new KeyValuePair<ListCategoryType, int>(
                        categoryType,
                        viewModel.gameCatalog.ByCategory(categoryType).Sum(grouping => grouping.Count())))
                    .OrderByDescending(entry => entry.Value);

                foreach (KeyValuePair<ListCategoryType, int> entry in counts)
                {
                    sb.AppendLine($"{entry.Key}={Count(entry.Value)} ({Ratio(entry.Value, catalogGames)} per game)");
                }
            }
            sb.AppendLine();

            sb.AppendLine("[lists]");
            List<GameListSet> gameListSets = viewModel.GameListSets ?? new List<GameListSet>();
            foreach (GameListSet gameListSet in gameListSets)
            {
                int listCount = gameListSet.GameLists?.Count ?? 0;
                sb.AppendLine($"{gameListSet.ListCategoryType}={Count(listCount)} lists, {Count(gameListSet.TotalGameCount)} games");
            }
            sb.AppendLine();

            sb.AppendLine("[timings-ms]");
            AppendMetric(sb, "buildCatalog", (int)catalogMilliseconds);
            AppendMetric(sb, "createGameLists", (int)createGameListsMilliseconds);
            sb.AppendLine($"startupBlocking={Count((int)(catalogMilliseconds + createGameListsMilliseconds))}");

            // built off the startup path, so this overlaps the work above rather than adding to it
            AppendMetric(sb, "buildVoiceIndexBackground", (int)VoiceSearchIndex.Instance.BuildMilliseconds);

            return sb.ToString();
        }

        #endregion

        #region formatting

        private static List<string> GetGrammarPhrases()
        {
            if (!EclipseSettingsDataProvider.Instance.EclipseSettings.EnableVoiceSearch)
            {
                return null;
            }

            return VoiceSearchIndex.Instance.Phrases
                .OrderBy(phrase => phrase, StringComparer.Ordinal)
                .ToList();
        }

        private static string Describe(GameMatch gameMatch)
        {
            return $"{gameMatch.Game.Id} {gameMatch.Game.Title}";
        }

        private static void AppendSetting(StringBuilder sb, string name, bool value)
        {
            sb.AppendLine($"{name}={(value ? "true" : "false")}");
        }

        private static void AppendMetric(StringBuilder sb, string name, int value)
        {
            sb.AppendLine($"{name}={Count(value)}");
        }

        private static string Count(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Ratio(int numerator, int denominator)
        {
            if (denominator == 0)
            {
                return "0.0";
            }

            return ((double)numerator / denominator).ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string Percent(int numerator, int denominator)
        {
            if (denominator == 0)
            {
                return "0%";
            }

            return ((double)numerator * 100 / denominator).ToString("0", CultureInfo.InvariantCulture) + "%";
        }

        #endregion
    }
}
