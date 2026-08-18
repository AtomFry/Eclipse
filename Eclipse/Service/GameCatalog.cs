using Eclipse.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Service
{
    // The game library: one GameMatch per game, plus an index from each category value to
    // the games carrying it.
    //
    // This replaces the per-category cloning GameBagService used to do. A game in three
    // genres is now one object referenced from three buckets rather than three objects, so
    // a category lookup is a dictionary hit instead of a scan over every clone in the
    // library.
    public sealed class GameCatalog
    {
        private static readonly ILookup<string, GameMatch> EmptyLookup =
            Enumerable.Empty<GameMatch>().ToLookup(gameMatch => string.Empty);

        private bool isSetup;
        private List<GameMatch> games;
        private List<GameFiles> mediaEntries;
        private Dictionary<ListCategoryType, ILookup<string, GameMatch>> categoryIndex;

        // Every game that passes the broken/hidden filters, once each.
        public IReadOnlyList<GameMatch> Games
        {
            get
            {
                EnsureSetup();
                return games;
            }
        }

        // Media records in the same order as Games, for the background hydration pump.
        public IReadOnlyList<GameFiles> MediaEntries
        {
            get
            {
                EnsureSetup();
                return mediaEntries;
            }
        }

        // Games grouped by the values they carry for a category - genre name, publisher,
        // release year and so on. Category types that are not browsable this way
        // (VoiceSearch, RandomGame, MoreLikeThis) return an empty lookup.
        public ILookup<string, GameMatch> ByCategory(ListCategoryType listCategoryType)
        {
            EnsureSetup();

            ILookup<string, GameMatch> lookup;
            if (categoryIndex.TryGetValue(listCategoryType, out lookup))
            {
                return lookup;
            }
            return EmptyLookup;
        }

        private void EnsureSetup()
        {
            if (!isSetup)
            {
                Setup();
            }
        }

        private void Setup()
        {
            List<IGame> allGames = DataService.GetGames();

            // One pass over the playlist records instead of scanning them once per game.
            ILookup<string, PlaylistGame> playlistsByGameId =
                PlaylistGameService.Instance.PlaylistGames.ToLookup(playlistGame => playlistGame.GameId);

            PrescaleImages();

            bool includeBroken = EclipseSettingsDataProvider.Instance.EclipseSettings.IncludeBrokenGames;
            bool includeHidden = EclipseSettingsDataProvider.Instance.EclipseSettings.IncludeHiddenGames;

            ConcurrentBag<GameMatch> builtGames = new ConcurrentBag<GameMatch>();

            Parallel.ForEach(allGames, (game) =>
            {
                if (game.Broken && !includeBroken)
                {
                    return;
                }

                if (game.Hide && !includeHidden)
                {
                    return;
                }

                builtGames.Add(new GameMatch(game, new GameFiles(game)));
            });

            // Parallel.ForEach fills the bag in whatever order the threads finish, and every
            // sort downstream is a stable LINQ sort - so without a deterministic order here,
            // games that tie on a sort key come out in a different sequence on every run.
            // Ordering by the dominant sort key, then by id, makes the whole pipeline
            // reproducible.
            games = builtGames
                .OrderBy(gameMatch => gameMatch.Game.SortTitleOrTitle)
                .ThenBy(gameMatch => gameMatch.Game.Id, StringComparer.Ordinal)
                .ToList();

            mediaEntries = games.Select(gameMatch => gameMatch.GameFiles).ToList();
            categoryIndex = BuildCategoryIndex(games, playlistsByGameId);

            isSetup = true;
        }

        private static Dictionary<ListCategoryType, ILookup<string, GameMatch>> BuildCategoryIndex(
            List<GameMatch> games,
            ILookup<string, PlaylistGame> playlistsByGameId)
        {
            return new Dictionary<ListCategoryType, ILookup<string, GameMatch>>
            {
                [ListCategoryType.Platform] = games.ToLookup(gameMatch => gameMatch.Game.Platform),
                [ListCategoryType.ReleaseYear] = games.ToLookup(gameMatch => gameMatch.ReleaseYear),
                [ListCategoryType.PlayMode] = Expand(games, gameMatch => gameMatch.Game.PlayModes),
                [ListCategoryType.Genre] = Expand(games, gameMatch => gameMatch.Game.Genres),
                [ListCategoryType.Publisher] = Expand(games, gameMatch => gameMatch.Game.Publishers),
                [ListCategoryType.Developer] = Expand(games, gameMatch => gameMatch.Game.Developers),
                [ListCategoryType.Series] = Expand(games, gameMatch => gameMatch.Game.SeriesValues),
                [ListCategoryType.Playlist] = Expand(games, gameMatch =>
                    playlistsByGameId[gameMatch.Game.Id].Select(playlistGame => playlistGame.Playlist))
            };
        }

        // Index a category whose games carry several values at once - a game with three
        // genres lands in three buckets, referencing the same object each time.
        private static ILookup<string, GameMatch> Expand(List<GameMatch> games, Func<GameMatch, IEnumerable<string>> valuesOf)
        {
            return games
                .SelectMany(gameMatch => valuesOf(gameMatch).Select(value => new { Value = value, Game = gameMatch }))
                .ToLookup(entry => entry.Value, entry => entry.Game);
        }

        // Pre-scale artwork to the display resolution. This runs here rather than on demand
        // so the loading screen can show progress while it happens.
        private static void PrescaleImages()
        {
            List<FileInfo> gameFrontFilesToProcess = ImageScaler.GetMissingGameFrontImageFiles();
            List<FileInfo> platformLogosToProcess = ImageScaler.GetMissingPlatformClearLogoFiles();
            List<FileInfo> gameClearLogosToProcess = ImageScaler.GetMissingGameClearLogoFiles();
            bool scaleDefaultBoxFrontImage = !ImageScaler.DefaultBoxFrontExists();

            // get the desired height of pre-scaled box images based on the monitor's resolution
            // only need this if we have anything to process
            int desiredHeight = 0;
            if ((gameFrontFilesToProcess.Count > 0)
                || (platformLogosToProcess.Count > 0)
                || (gameClearLogosToProcess.Count > 0)
                || scaleDefaultBoxFrontImage)
            {
                desiredHeight = ImageScaler.GetDesiredHeight();
            }

            // scale the default box front image
            if (scaleDefaultBoxFrontImage)
            {
                ImageScaler.ScaleDefaultBoxFront(desiredHeight);
            }

            // crop platform clear logos
            foreach (FileInfo fileInfo in platformLogosToProcess)
            {
                ImageScaler.CropImage(fileInfo);
            }
        }

        #region singleton implementation
        public static GameCatalog Instance => instance;

        private static readonly GameCatalog instance = new GameCatalog();

        static GameCatalog()
        {
        }

        private GameCatalog()
        {
        }
        #endregion
    }
}
