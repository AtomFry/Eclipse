using Eclipse.View;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Models
{
    public class PlaylistGame
    {
        public string GameId { get; set; }
        public string Playlist { get; set; }
    }

    public class GameMatch : INotifyPropertyChanged
    {
        #region Static Members
        public static ConcurrentDictionary<string, Uri> GameFrontImages = new ConcurrentDictionary<string, Uri>();

        public static char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        public static string IEnumerableToCommaSeparatedString(IEnumerable<string> list)
        {
            if (list != null)
            {
                StringBuilder sb = new StringBuilder();

                foreach (string item in list)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(item);
                }
                return sb.ToString();
            }
            return string.Empty;
        }

        public static Uri ResolveGameFrontImage(IGame Game)
        {
            string frontImagePath = Game?.FrontImagePath;
            if (!string.IsNullOrWhiteSpace(frontImagePath))
            {
                string customPath = frontImagePath.Replace(Helpers.ApplicationPath, Helpers.MediaFolder);
                if (!string.IsNullOrWhiteSpace(customPath))
                {
                    if (File.Exists(customPath))
                    {
                        return new Uri(customPath);
                    }
                }
            }
            return null;
        }

        public static string ResolveGameTitleFileName(IGame Game)
        {
            string gameTitleToFileName = Game.Title;

            if (!string.IsNullOrWhiteSpace(gameTitleToFileName))
            {
                foreach (char invalidChar in InvalidFileNameChars)
                {
                    gameTitleToFileName = gameTitleToFileName.Replace(invalidChar, '_');
                }
                return gameTitleToFileName;
            }
            return string.Empty;
        }

        // tries to find a bezel image in the following order
        // game specific bezel in the plugin media\images\{Platform}\Bezel\{CleanGameTitle}.png
        // MAME bezels ..\LaunchBox\Emulators\MAME\artwork\{game.ApplicationFilePath}\"Bezel.png"
        // Retroarch bezels ..\LaunchBox\Emulators\Retroarch\overlays\GameBezels\{RetroarchPlatform}\{game.ApplicationFilePath}.png
        // fallback bezels are setup in the view because they are dependent on the video size so we set them when the video is loaded and the video size is known
        // fallback default plaform bezel path
        // ..\LaunchBox\Plugins\Eclipse\Media\Images\Platforms\{PLATFORM}\Bezel\Horizontal.png
        // ..\LaunchBox\Plugins\Eclipse\Media\Images\Platforms\{PLATFORM}\Bezel\Vertical.png
        // fallback default bezel path
        // ..\LaunchBox\Plugins\Eclipse\Media\Images\Platforms\Default\Bezel\Horizontal.png
        // ..\LaunchBox\Plugins\Eclipse\Media\Images\Platforms\Default\Bezel\Vertical.png
        public static Uri ResolveBezelPath(IGame Game, string TitleToFileName)
        {
            try
            {
                // find game specific bezel
                // Game Specific: ..\LaunchBox\Plugins\Eclipse\Media\Images\{PLATFORM}\Bezel\{TitleToFileName}.png
                string gameBezelPath = Path.Combine(Helpers.PluginImagesPath, Game.Platform, "Bezel");
                if (Directory.Exists(gameBezelPath))
                {
                    string[] gameBezelFiles = Directory.GetFiles(gameBezelPath, $"{TitleToFileName}.*", SearchOption.AllDirectories);
                    if (gameBezelFiles != null && gameBezelFiles.Length > 0)
                    {
                        gameBezelPath = gameBezelFiles[0];
                        if (!string.IsNullOrWhiteSpace(gameBezelPath) && File.Exists(gameBezelPath))
                        {
                            return new Uri(gameBezelPath);
                        }
                    }
                }

                // game specific bezels in MAME or Retroarch
                // MAME: ..\LaunchBox\Emulators\MAME\artwork\{game.ApplicationFilePath}\"Bezel.png"
                // Retroarch: ..\LaunchBox\Emulators\Retroarch\overlays\GameBezels\{CONVERTTORETROARCHPLATFORM}\{game.ApplicationFilePath}.png
                IEmulator emulator = PluginHelper.DataManager.GetEmulatorById(Game.EmulatorId);
                if (!string.IsNullOrWhiteSpace(emulator?.ApplicationPath))
                {
                    string emulatorFolder = Path.GetDirectoryName(emulator.ApplicationPath);

                    // hardcoded strings are lame here but I'm too lazy to fix this
                    if (emulator.ApplicationPath.ToLower().Contains("mame"))
                    {
                        gameBezelPath = Path.Combine(Helpers.ApplicationPath, emulatorFolder, "artwork", Path.GetFileNameWithoutExtension(Game.ApplicationPath), "Bezel.png");

                        if (!string.IsNullOrWhiteSpace(gameBezelPath) && File.Exists(gameBezelPath))
                        {
                            return new Uri(gameBezelPath);
                        }
                    }

                    if (emulator.ApplicationPath.ToLower().Contains("retroarch"))
                    {
                        string retroarchPlatformFolder = "";
                        if (Helpers.RetroarchPlatformLookup.TryGetValue(Game.Platform, out retroarchPlatformFolder))
                        {
                            gameBezelPath = Path.Combine(Helpers.ApplicationPath, emulatorFolder, @"overlays\GameBezels", retroarchPlatformFolder, $"{Path.GetFileNameWithoutExtension(Game.ApplicationPath)}.png");

                            if (!string.IsNullOrWhiteSpace(gameBezelPath) && File.Exists(gameBezelPath))
                            {
                                return new Uri(gameBezelPath);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helpers.LogException(ex, $"setting up bezel for {Game.Title}");
            }
            return null;
        }

        public static Uri ResolveClearLogoPath(IGame Game)
        {
            string clearLogoPath = Game.ClearLogoImagePath;
            if (!string.IsNullOrWhiteSpace(clearLogoPath))
            {
                string customPath = clearLogoPath.Replace(Helpers.ApplicationPath, Helpers.MediaFolder);
                if (!string.IsNullOrWhiteSpace(customPath))
                {
                    if (File.Exists(customPath))
                    {
                        return new Uri(customPath);
                    }
                }
            }
            return null;
        }

        public static Uri ResolveStarRatingPath(IGame Game)
        {
            string ratingFormatted = String.Format("{0:0.0}", Math.Round(Game.CommunityOrLocalStarRating, 1));
            string path = $"{Helpers.MediaFolder}\\StarRating\\{ratingFormatted}.png";
            if (File.Exists(path))
            {
                return new Uri(path);
            }
            return null;
        }

        public static Uri ResolvePlayModePath(IGame Game)
        {
            // resolve play mode path based on game's play mode 
            string path = $"{Helpers.MediaFolder}\\PlayMode\\{Game.PlayMode}.png";
            if (File.Exists(path))
            {
                return new Uri(path);
            }

            // resolve fallback path 
            path = $"{Helpers.MediaFolder}\\PlayMode\\Fallback.png";
            if (File.Exists(path))
            {
                return new Uri(path);
            }

            return null;
        }

        public static Uri ResolveBackgroundImagePath(IGame Game)
        {
            // get game's background image - either background or screenshot
            string backgroundImagePath = Game.BackgroundImagePath ?? Game.ScreenshotImagePath;
            if (!string.IsNullOrWhiteSpace(backgroundImagePath))
            {
                return new Uri(backgroundImagePath);
            }

            // fallback to platform device 
            string path = $"{Helpers.MediaFolder}\\PlatformDevice\\{Game.Platform}.png";
            if (File.Exists(path))
            {
                return new Uri(path);
            }

            // default resource provided for final fallback in case platform is not found
            return ResourceImages.DefaultBackground;
        }

        public static Uri ResolvePlatformLogoPath(IGame Game)
        {
            string platformClearLogoImagePath = Game.PlatformClearLogoImagePath;
            if (!string.IsNullOrWhiteSpace(platformClearLogoImagePath))
            {
                string customPath = platformClearLogoImagePath.Replace(Helpers.ApplicationPath, Helpers.MediaFolder);
                if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
                {
                    return new Uri(customPath);
                }
            }
            return null;
        }

        public static string ResolveVideoPath(IGame Game)
        {
            string videoPath = Game?.GetVideoPath();
            if(!string.IsNullOrWhiteSpace(videoPath) && File.Exists(videoPath))
            {
                return videoPath;
            }
            return null;
        }

        public static string ResolveDeveloperString(IGame Game)
        {
            return IEnumerableToCommaSeparatedString(Game.Developers);
        }

        public static string ResolvePublisherString(IGame Game)
        {
            return IEnumerableToCommaSeparatedString(Game.Publishers);
        }

        public static string ResolveSeriesString(IGame Game)
        {
            return IEnumerableToCommaSeparatedString(Game.SeriesValues);
        }

        public static string ResolveGenreString(IGame Game)
        {
            return IEnumerableToCommaSeparatedString(Game.Genres);
        }

        public static string ResolveReleaseYear(IGame Game)
        {
            int? releaseYear = Game?.ReleaseDate?.Year;
            if(releaseYear != null)
            {
                return releaseYear.ToString();
            }
            return "    ";
        }

        public static int ResolveDefaultMatchPercentage(IGame Game)
        {
            float? communityOrLocalStarRating = Game?.CommunityOrLocalStarRating;
            if(communityOrLocalStarRating != null)
            {
                return (int)(communityOrLocalStarRating * 20);
            }
            return 0;
        }
        #endregion

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public IGame Game { get; set; }
        
        public TitleMatchType TitleMatchType { get; set; }

        public string ConvertedTitle { get; set; }

        public Uri GameBezelImage { get; set; }
        
        public Uri FrontImage { get; set; }
        public string TitleToFileName { get; set; }
        public Uri ClearLogo { get; set; }
        public Uri CommunityStarRatingImage { get; set; }
        public Uri PlayModeImage { get; set; }
        public Uri BackgroundImage { get; set; }
        public Uri PlatformClearLogoImage { get; set; }
        public string VideoPath { get; set; }

        // expose IGame favorite setting so we can save it back to the file
        public bool Favorite
        {
            get { return Game.Favorite; }
            set
            {
                if (Game.Favorite != value)
                {
                    Game.Favorite = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Favorite"));
                }
            }
        }

        // expose IGame user rating setting so we can save it back to the file
        public float UserRating
        {
            get { return Game.StarRatingFloat; }
            set
            {
                if (Game.StarRatingFloat != value)
                {
                    Game.StarRatingFloat = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("UserRating"));
                }
            }
        }

        public string Developer { get; set; }
        public string Publisher { get; set; }
        public string Series { get; set; }
        public string Genre { get; set; }
        public string ReleaseYear { get; set; }

        // set the match percentage based on star rating unless voice match then use voice recognition details
        public int MatchPercentage { get; set; }
        public string MatchDescription { get; set; }

        public ListCategoryType CategoryType { get; set; }
        public string CategoryValue { get; set; }

        #endregion

        public GameMatch()
        {

        }

        public static GameMatch CloneGameMatch(GameMatch otherGameMatch,
                                        ListCategoryType categoryType,
                                        string categoryValue,
                                        TitleMatchType titleMatchType = TitleMatchType.None,
                                        string convertedTitle = "")
        {
            GameMatch gameMatch = new GameMatch();
            gameMatch.Game = otherGameMatch.Game;
            gameMatch.TitleMatchType = titleMatchType;
            gameMatch.CategoryType = categoryType;
            gameMatch.CategoryValue = categoryValue;
            gameMatch.ConvertedTitle = convertedTitle;

            gameMatch.FrontImage = otherGameMatch.FrontImage;
            gameMatch.GameBezelImage = otherGameMatch.GameBezelImage;
            gameMatch.ClearLogo = otherGameMatch.ClearLogo;
            gameMatch.CommunityStarRatingImage = otherGameMatch.CommunityStarRatingImage;
            gameMatch.PlayModeImage = otherGameMatch.PlayModeImage;
            gameMatch.BackgroundImage = otherGameMatch.BackgroundImage;
            gameMatch.PlatformClearLogoImage = otherGameMatch.PlatformClearLogoImage;

            gameMatch.TitleToFileName = otherGameMatch.TitleToFileName;
            gameMatch.VideoPath = otherGameMatch.VideoPath;
            gameMatch.Developer = otherGameMatch.Developer;
            gameMatch.Publisher = otherGameMatch.Publisher;
            gameMatch.Series = otherGameMatch.Series;
            gameMatch.Genre = otherGameMatch.Genre;
            gameMatch.ReleaseYear = otherGameMatch.ReleaseYear;

            gameMatch.MatchPercentage = otherGameMatch.MatchPercentage;
            gameMatch.MatchDescription = otherGameMatch.MatchDescription;
            return (gameMatch);
        }

        public GameMatch(IGame game)
        {
            Game = game;

            FrontImage = ResolveGameFrontImage(game);
            TitleToFileName = ResolveGameTitleFileName(game);
            GameBezelImage = ResolveBezelPath(game, TitleToFileName);
            ClearLogo = ResolveClearLogoPath(game);
            CommunityStarRatingImage = ResolveStarRatingPath(game);
            PlayModeImage = ResolvePlayModePath(game);
            BackgroundImage = ResolveBackgroundImagePath(game);
            PlatformClearLogoImage = ResolvePlatformLogoPath(game);

            VideoPath = ResolveVideoPath(game);
            Developer = ResolveDeveloperString(game);
            Publisher = ResolvePublisherString(game);
            Series = ResolveSeriesString(game);
            Genre = ResolveGenreString(game);
            ReleaseYear = ResolveReleaseYear(game);

            MatchPercentage = ResolveDefaultMatchPercentage(game);
            SetMatchDescription();
        }

        private void SetMatchDescription()
        {
            MatchDescription = $"{string.Format("{0:00}", MatchPercentage)}% match";
        }

        public void SetupVoiceMatchPercentage(float confidence, string phrase)
        {
            /*
             * For voice matches...
             * Confidence of spoken phrase (0-1) - this will be multiplied by the final result 
             * 
             * Match level: full title (100), main title (95), subtitle (90), phrase (60)
             * Bonus portion allows the percentage of the title that was matched (length of phrase / length of title) to be applied to available score
             * 
             * Examples 
             * 1.  Full title match is 100 so there is no room for bonus
             * 2.  Main title match is 95 so there are 5 bonus points available.  
             * 3.  Title contains is 60 so there is 40 bonus points available.  For the phrase "super mario" against the game "super mario bros." you get bonus 40 * (11/17)

             * Multiply the confidence by the final score 
             * 
             * Feels like this shit could be tweaked endlessly and never settle on an ideal solution?
             */
            var lengthMatchPercent = (float)((float)phrase.Length / (float)ConvertedTitle.Length);

            int availableBonus = 100 - (int)TitleMatchType;
            float bonusMatchTypePortion = availableBonus * lengthMatchPercent;

            // subtracting 0.001 to make sure it's never 100% - probably impossible since the confidence is never 1 but just in case
            var matchTypePortion = confidence * ((int)TitleMatchType + bonusMatchTypePortion - 0.001);
            MatchPercentage = (int)(matchTypePortion);
            SetMatchDescription();
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                GameMatch other = obj as GameMatch;
                if (other == null)
                {
                    return (false);
                }

                return (Game.Id == other.Game.Id);
            }
        }

        public override int GetHashCode()
        {
            return (Game.Id.GetHashCode());
        }
    }
}
