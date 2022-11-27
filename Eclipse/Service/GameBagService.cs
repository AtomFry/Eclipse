using Eclipse.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Service
{
    public sealed class GameBagService
    {
        private bool isSetup = false;
        private ConcurrentBag<GameMatch> gameBag;
        private ConcurrentBag<GameFiles> gameFilesBag;

        public ConcurrentBag<GameMatch> GameBag
        {
            get
            {
                if (!isSetup)
                {
                    Setup();
                }
                return gameBag;
            }
        }

        public ConcurrentBag<GameFiles> GameFilesBag
        {
            get
            {
                if (!isSetup)
                {
                    Setup();
                }
                return gameFilesBag;
            }
        }

        private void Setup()
        {
            List<IGame> allGames = DataService.GetGames();
            List<PlaylistGame> playlistGames = PlaylistGameService.Instance.PlaylistGames;

            gameBag = new ConcurrentBag<GameMatch>();
            gameFilesBag = new ConcurrentBag<GameFiles>();

            // prescale box front images - doing this here so it's easier to update the progress bar
            // get list of image files in launchbox folders that are missing from the plug-in folders
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

            bool includeBroken = EclipseSettingsDataProvider.Instance.EclipseSettings.IncludeBrokenGames;
            bool includeHidden = EclipseSettingsDataProvider.Instance.EclipseSettings.IncludeHiddenGames;

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

                GameFiles gameFiles = new GameFiles(game);
                gameFilesBag.Add(gameFiles);

                GameMatch gameMatch = new GameMatch(game, gameFiles);

                // create a dictionary of platform game matches
                gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Platform, game.Platform));

                // create a dictionary of release year game matches
                gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.ReleaseYear, gameMatch.ReleaseYear));

                // create a dictionary of play modes and game matches
                foreach (string playMode in game.PlayModes)
                {
                    gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.PlayMode, playMode));
                }

                // create a dictionary of Genres and game matches
                foreach (string genre in game.Genres)
                {
                    gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Genre, genre));
                }

                // create a dictionary of publishers and game matches
                foreach (string publisher in game.Publishers)
                {
                    gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Publisher, publisher));
                }

                // create a dictionary of developers and game matches
                foreach (string developer in game.Developers)
                {
                    gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Developer, developer));
                }

                // create a dictionary of series and game matches
                foreach (string series in game.SeriesValues)
                {
                    gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Series, series));
                }

                // create a dictionary of playlist and game matches 
                IEnumerable<PlaylistGame> playlistGameQuery = from playlistGame in playlistGames
                                                              where playlistGame.GameId == game.Id
                                                              select playlistGame;

                foreach (PlaylistGame playlistGame in playlistGameQuery)
                {
                    gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.Playlist, playlistGame.Playlist));
                }

                // create voice recognition grammar library for the game
                if (EclipseSettingsDataProvider.Instance.EclipseSettings.EnableVoiceSearch)
                {
                    GameTitleGrammarBuilder gameTitleGrammarBuilder = new GameTitleGrammarBuilder(game);
                    foreach (GameTitleGrammar gameTitleGrammar in gameTitleGrammarBuilder.gameTitleGrammars)
                    {
                        if (!string.IsNullOrWhiteSpace(gameTitleGrammar.Title))
                        {
                            gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, gameTitleGrammar.Title, TitleMatchType.FullTitleMatch, gameTitleGrammar.Title));
                        }

                        if (!string.IsNullOrWhiteSpace(gameTitleGrammar.MainTitle))
                        {
                            gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, gameTitleGrammar.MainTitle, TitleMatchType.MainTitleMatch, gameTitleGrammar.Title));
                        }

                        if (!string.IsNullOrWhiteSpace(gameTitleGrammar.Subtitle))
                        {
                            gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, gameTitleGrammar.Subtitle, TitleMatchType.SubtitleMatch, gameTitleGrammar.Title));
                        }

                        for (int i = 0; i < gameTitleGrammar.TitleWords.Count; i++)
                        {
                            StringBuilder sb = new StringBuilder();
                            for (int j = i; j < gameTitleGrammar.TitleWords.Count; j++)
                            {
                                sb.Append($"{gameTitleGrammar.TitleWords[j]} ");
                                if (!GameTitleGrammar.IsNoiseWord(sb.ToString().Trim()))
                                {
                                    gameBag.Add(GameMatch.CloneGameMatch(gameMatch, ListCategoryType.VoiceSearch, sb.ToString().Trim(), TitleMatchType.FullTitleContains, gameTitleGrammar.Title));
                                }
                            }
                        }
                    }
                }
            });

            isSetup = true;
        }


        #region singleton implementation 
        public static GameBagService Instance => instance;

        private static readonly GameBagService instance = new GameBagService();

        static GameBagService()
        {
        }

        private GameBagService()
        {
        }
        #endregion
    }
}