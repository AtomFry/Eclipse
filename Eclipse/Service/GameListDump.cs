using Eclipse.Helpers;
using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Eclipse.Service
{
    // Records exactly what the game lists contain, so that moving list construction out of the
    // view model can be proved to have changed nothing.
    //
    // Most of that work (B-15) is guarded by unit tests, but the first step cannot be: the
    // builder has to come out of a view model that will not construct outside BigBox before any
    // test can reach it. This is what guards that step - dump before the move, dump after, diff
    // the two files. An empty diff is the acceptance criterion.
    //
    // Off unless DumpGameLists is set in EclipseSettings.json. It is not in the settings UI;
    // it is a development tool, and it writes a file the size of the library.
    public static class GameListDump
    {
        public static void WriteIfEnabled(IEnumerable<GameListSet> gameListSets, string label)
        {
            if (!EclipseSettingsDataProvider.Instance.EclipseSettings.DumpGameLists)
            {
                return;
            }

            try
            {
                string path = Write(gameListSets, label);
                LogHelper.Log($"Game list dump written to {path}");
            }
            catch (Exception ex)
            {
                // A diagnostic that breaks startup is worse than no diagnostic
                LogHelper.LogException(ex, "write the game list dump");
            }
        }

        private static string Write(IEnumerable<GameListSet> gameListSets, string label)
        {
            string folder = DirectoryInfoHelper.Instance.EclipseFolder;
            Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, $"GameListDump-{label}-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                WriteHeader(writer, label);

                // Sets are written in category order rather than the order they were built.
                // Nothing reads GameListSets positionally - every consumer looks a set up by
                // its category - so a build that produces the same sets in a different order
                // is not a behaviour change and should not show up as one in the diff.
                IEnumerable<GameListSet> orderedSets = (gameListSets ?? Enumerable.Empty<GameListSet>())
                    .OrderBy(set => set.ListCategoryType.ToString(), StringComparer.Ordinal);

                foreach (GameListSet gameListSet in orderedSets)
                {
                    WriteSet(writer, gameListSet);
                }
            }

            return path;
        }

        private static void WriteHeader(StreamWriter writer, string label)
        {
            EclipseSettings settings = EclipseSettingsDataProvider.Instance.EclipseSettings;

            // ShowGameCountInList changes what a list is called, and IncludeHiddenGames and
            // IncludeBrokenGames change what the library contains, so a dump is only
            // comparable against another taken with the same three values.
            writer.WriteLine($"# Eclipse game list dump - {label}");
            writer.WriteLine($"# taken {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"# ShowGameCountInList={settings.ShowGameCountInList}");
            writer.WriteLine($"# IncludeHiddenGames={settings.IncludeHiddenGames}");
            writer.WriteLine($"# IncludeBrokenGames={settings.IncludeBrokenGames}");
            writer.WriteLine("#");
            writer.WriteLine("# SET   <category> lists=<n> games=<n>");
            writer.WriteLine("# LIST  sortOrder=<n> start=<n> count=<n> value=<list value>");
            writer.WriteLine("# GAME  <position> <game id> <title>");
            writer.WriteLine("#");
        }

        private static void WriteSet(StreamWriter writer, GameListSet gameListSet)
        {
            List<GameList> gameLists = gameListSet?.GameLists ?? new List<GameList>();

            writer.WriteLine();
            writer.WriteLine($"SET\t{gameListSet?.ListCategoryType}\tlists={gameLists.Count}\tgames={gameLists.Sum(list => list.MatchCount)}");

            // Lists are written in the order the set holds them, because that order is the
            // order the user scrolls through and is exactly what this is here to protect.
            foreach (GameList gameList in gameLists)
            {
                WriteList(writer, gameList);
            }
        }

        private static void WriteList(StreamWriter writer, GameList gameList)
        {
            writer.WriteLine($"LIST\tsortOrder={gameList.SortOrder}\tstart={gameList.ListSetStartIndex}\tcount={gameList.MatchCount}\tvalue={gameList.ListTypeValue}");

            List<GameMatch> matchingGames = gameList.MatchingGames;
            if (matchingGames == null)
            {
                return;
            }

            // Position is written alongside the id so that a game moving within a list shows
            // up as a change on both lines rather than as a silent renumbering below it.
            for (int position = 0; position < matchingGames.Count; position++)
            {
                GameMatch gameMatch = matchingGames[position];
                writer.WriteLine($"GAME\t{position}\t{gameMatch?.Game?.Id}\t{gameMatch?.Game?.Title}");
            }
        }
    }
}
