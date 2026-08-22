using Eclipse.Helpers;
using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Eclipse.Service
{
    // Exercises every custom list field and operator against the real library and records what
    // each one does, so that replacing the reflection-based query engine (B-15b) can be shown
    // to have changed nothing.
    //
    // The startup dump only proves that *this* user's custom lists still work, and a
    // CustomLists.json rarely uses more than a handful of the 27 fields and 9 operators. Other
    // people's lists use the rest, and those files are not in this repository. This walks the
    // whole matrix instead: one synthetic list per combination, built through the real
    // GameListBuilder, with the result - or the exception - written down.
    //
    // Three outcomes are all equally valid to record. A combination that throws today must
    // still throw tomorrow; making it stop throwing is a decision, not a refactor.
    //
    // Runs with DumpGameLists, and comes out of the tree with the rest of the dump machinery.
    public static class CustomListQueryProbe
    {
        // How far into the library to look for a usable sample value before giving up on a
        // field. Games are in a deterministic order, so the same value is picked every run.
        private const int SampleGamesToScan = 500;

        // Enough games per probe to show that ordering and the size cap did something, without
        // writing the library out 300 times.
        private const int GamesPerProbe = 25;

        public static void WriteIfEnabled(IGameCatalogSource catalog)
        {
            if (!EclipseSettingsDataProvider.Instance.EclipseSettings.DumpGameLists)
            {
                return;
            }

            try
            {
                string path = Write(catalog);
                LogHelper.Log($"Custom list query probe written to {path}");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "write the custom list query probe");
            }
        }

        private static string Write(IGameCatalogSource catalog)
        {
            string folder = DirectoryInfoHelper.Instance.EclipseFolder;
            Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, $"CustomListProbe-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

            GameFieldEnum[] fields = (GameFieldEnum[])Enum.GetValues(typeof(GameFieldEnum));
            FilterFieldOperator[] operators = (FilterFieldOperator[])Enum.GetValues(typeof(FilterFieldOperator));

            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                WriteHeader(writer, catalog, fields.Length, operators.Length);

                // Sample values first and write them out, because every filter result below
                // depends on them. If the library changes, this section says so directly
                // instead of leaving a hundred probes to be puzzled over.
                Dictionary<GameFieldEnum, object> sampleValues = new Dictionary<GameFieldEnum, object>();
                writer.WriteLine();
                foreach (GameFieldEnum field in fields)
                {
                    object sample = SampleValue(catalog, field);
                    sampleValues[field] = sample;
                    writer.WriteLine($"SAMPLE\t{field}\t{field.ToFieldName()}\t{Describe(sample)}");
                }

                writer.WriteLine();
                foreach (GameFieldEnum field in fields)
                {
                    foreach (FilterFieldOperator filterFieldOperator in operators)
                    {
                        RunProbe(writer, catalog,
                                 $"filter\t{field}\t{filterFieldOperator}",
                                 FilterDefinition(field, filterFieldOperator, sampleValues[field]));
                    }
                }

                writer.WriteLine();
                foreach (GameFieldEnum field in fields)
                {
                    foreach (SortDirection direction in new[] { SortDirection.Ascending, SortDirection.Descending })
                    {
                        RunProbe(writer, catalog,
                                 $"sort\t{field}\t{direction}",
                                 SortDefinition(field, direction));
                    }
                }

                // Two keys, to show that the second only breaks ties of the first and that the
                // size cap is applied after both.
                writer.WriteLine();
                RunProbe(writer, catalog, "sort2\tPlatform-Asc\tTitle-Desc",
                         SortDefinition(GameFieldEnum.Platform, SortDirection.Ascending, GameFieldEnum.Title, SortDirection.Descending));
                RunProbe(writer, catalog, "sort2\tStarRating-Desc\tTitle-Asc",
                         SortDefinition(GameFieldEnum.StarRating, SortDirection.Descending, GameFieldEnum.Title, SortDirection.Ascending));
                RunProbe(writer, catalog, "sort2\tReleaseYear-Asc\tSortTitleOrTitle-Asc",
                         SortDefinition(GameFieldEnum.ReleaseYear, SortDirection.Ascending, GameFieldEnum.SortTitleOrTitle, SortDirection.Ascending));
            }

            return path;
        }

        private static void WriteHeader(StreamWriter writer, IGameCatalogSource catalog, int fieldCount, int operatorCount)
        {
            EclipseSettings settings = EclipseSettingsDataProvider.Instance.EclipseSettings;

            writer.WriteLine("# Eclipse custom list query probe");
            writer.WriteLine($"# taken {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"# games in library={catalog.Games.Count}");
            writer.WriteLine($"# fields={fieldCount} operators={operatorCount} gamesPerProbe={GamesPerProbe}");
            writer.WriteLine($"# IncludeHiddenGames={settings.IncludeHiddenGames}");
            writer.WriteLine($"# IncludeBrokenGames={settings.IncludeBrokenGames}");
            writer.WriteLine("#");
            writer.WriteLine("# SAMPLE <field> <property path> <value the filter probes use>");
            writer.WriteLine("# PROBE  <kind> <field> <operator/direction> <OK count=n | EMPTY | THREW ...>");
            writer.WriteLine("# GAME   <position> <game id> <title>");
            writer.WriteLine("#");
        }

        // Each probe gets its own builder and its own try/catch. A combination that throws must
        // not stop the ones after it, and the builder has no per-definition error handling -
        // which is itself part of what is being recorded.
        private static void RunProbe(StreamWriter writer, IGameCatalogSource catalog, string label, CustomListDefinition definition)
        {
            try
            {
                // MoreLikeThis is not in the catalog's category index, so ByCategory returns an
                // empty lookup and the set comes back holding only the probe's own list.
                GameListSet gameListSet = new GameListBuilder(catalog, new[] { definition }, null)
                    .Build(ListCategoryType.MoreLikeThis);

                GameList gameList = gameListSet.GameLists.FirstOrDefault();
                if (gameList == null)
                {
                    // The builder drops a custom list that matched nothing, so no list here
                    // means the filter excluded every game.
                    writer.WriteLine($"PROBE\t{label}\tEMPTY");
                    return;
                }

                writer.WriteLine($"PROBE\t{label}\tOK\tcount={gameList.MatchCount}");

                List<GameMatch> matchingGames = gameList.MatchingGames ?? new List<GameMatch>();
                for (int position = 0; position < matchingGames.Count; position++)
                {
                    writer.WriteLine($"GAME\t{position}\t{matchingGames[position]?.Game?.Id}\t{matchingGames[position]?.Game?.Title}");
                }
            }
            catch (Exception ex)
            {
                // Type and first line only. Some messages carry expression text that would
                // change with an unrelated edit and make the diff noisy.
                string message = (ex.Message ?? string.Empty).Split('\n')[0].Trim();
                writer.WriteLine($"PROBE\t{label}\tTHREW\t{ex.GetType().Name}: {message}");
            }
        }

        private static CustomListDefinition FilterDefinition(GameFieldEnum field, FilterFieldOperator filterFieldOperator, object value)
        {
            CustomListDefinition definition = NewDefinition($"filter-{field}-{filterFieldOperator}");
            definition.FilterExpressions.Add(new FilterExpression
            {
                GameFieldEnum = field,
                FilterFieldOperator = filterFieldOperator,
                FilterFieldValue = value
            });
            return definition;
        }

        private static CustomListDefinition SortDefinition(GameFieldEnum field, SortDirection direction)
        {
            CustomListDefinition definition = NewDefinition($"sort-{field}-{direction}");
            definition.SortExpressions.Add(new SortExpression { GameFieldEnum = field, SortDirection = direction });
            return definition;
        }

        private static CustomListDefinition SortDefinition(GameFieldEnum first, SortDirection firstDirection,
                                                           GameFieldEnum second, SortDirection secondDirection)
        {
            CustomListDefinition definition = SortDefinition(first, firstDirection);
            definition.SortExpressions.Add(new SortExpression { GameFieldEnum = second, SortDirection = secondDirection });
            return definition;
        }

        private static CustomListDefinition NewDefinition(string description)
        {
            return new CustomListDefinition
            {
                Id = description,
                Description = description,
                MaxGamesInList = GamesPerProbe,
                ListCategoryTypes = new List<ListCategoryType> { ListCategoryType.MoreLikeThis }
            };
        }

        // A real filter value comes out of CustomLists.json, so it arrives boxed as whatever
        // Newtonsoft produced - long for whole numbers, double for fractional ones - and the
        // engine converts it to the property's type. Sampling a live value and re-boxing it the
        // way JSON would is what makes these probes represent real custom lists rather than a
        // friendlier version of them.
        private static object SampleValue(IGameCatalogSource catalog, GameFieldEnum field)
        {
            string path = field.ToFieldName();
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            foreach (GameMatch gameMatch in catalog.Games.Take(SampleGamesToScan))
            {
                object value = ReadPath(gameMatch, path);

                if (value == null)
                {
                    continue;
                }

                if ((value is string text) && string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                return AsJsonValue(value);
            }

            return null;
        }

        // Walks the property path the same way the query engine does - off the static types,
        // not the runtime ones - so a field the engine cannot resolve is not silently resolved
        // here instead.
        private static object ReadPath(GameMatch gameMatch, string path)
        {
            Type type = typeof(GameMatch);
            object current = gameMatch;

            foreach (string part in path.Split('.'))
            {
                PropertyInfo propertyInfo = type.GetProperty(part);
                if (propertyInfo == null)
                {
                    return null;
                }

                current = current == null ? null : propertyInfo.GetValue(current);
                type = propertyInfo.PropertyType;
            }

            return current;
        }

        private static object AsJsonValue(object value)
        {
            if (value is int intValue)
            {
                return (long)intValue;
            }

            if (value is float floatValue)
            {
                return (double)floatValue;
            }

            return value;
        }

        private static string Describe(object value)
        {
            if (value == null)
            {
                return "value=(none)";
            }

            return $"value={value} ({value.GetType().Name})";
        }
    }
}
