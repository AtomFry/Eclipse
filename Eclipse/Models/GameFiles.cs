using Eclipse.Helpers;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Models
{
    public class GameFiles : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public GameFiles(IGame game)
        {
            IsSetup = false;
            Game = game;
            FrontImage = ResourceImages.GameFrontDummy;
        }

        private IGame game;
        public IGame Game
        {
            get { return game; }
            set
            {
                if (game != value)
                {
                    game = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Game"));
                }
            }
        }

        private Uri frontImage;
        public Uri FrontImage
        {
            get { return frontImage; }
            set
            {
                if (frontImage != value)
                {
                    frontImage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("FrontImage"));
                }
            }
        }

        private Uri clearLogo;
        public Uri ClearLogo
        {
            get { return clearLogo; }
            set
            {
                if (clearLogo != value)
                {
                    clearLogo = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("ClearLogo"));
                }
            }
        }

        private Uri communityStarRatingImage;
        public Uri CommunityStarRatingImage
        {
            get { return communityStarRatingImage; }
            set
            {
                if (communityStarRatingImage != value)
                {
                    communityStarRatingImage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("CommunityStarRatingImage"));
                }
            }
        }

        private Uri userStarRatingImage;
        public Uri UserStarRatingImage
        {
            get { return userStarRatingImage; }
            set
            {
                if (userStarRatingImage != value)
                {
                    userStarRatingImage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("UserStarRatingImage"));
                }
            }
        }


        private Uri playModeImage;
        public Uri PlayModeImage
        {
            get { return playModeImage; }
            set
            {
                if (playModeImage != value)
                {
                    playModeImage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("PlayModeImage"));
                }
            }
        }

        private Uri backgroundImage;
        public Uri BackgroundImage
        {
            get { return backgroundImage; }
            set
            {
                if (backgroundImage != value)
                {
                    backgroundImage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("BackgroundImage"));
                }
            }
        }

        private Uri platformClearLogoImage;
        public Uri PlatformClearLogoImage
        {
            get { return platformClearLogoImage; }
            set
            {
                if (platformClearLogoImage != value)
                {
                    platformClearLogoImage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("PlatformClearLogoImage"));
                }
            }
        }

        private string videoPath;
        public string VideoPath
        {
            get { return videoPath; }
            set
            {
                if (videoPath != value)
                {
                    videoPath = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("VideoPath"));
                }
            }
        }

        private Uri gameBezelImage;
        public Uri GameBezelImage
        {
            get { return gameBezelImage; }
            set
            {
                if (gameBezelImage != value)
                {
                    gameBezelImage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("GameBezelImage"));
                }
            }
        }

        private string titleToFileName;
        public string TitleToFileName
        {
            get { return titleToFileName; }
            set
            {
                if (titleToFileName != value)
                {
                    titleToFileName = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("TitleToFileName"));
                }
            }
        }

        public bool IsSetup { get; set; }

        public async Task SetupFiles()
        {
            await Task.Run(() =>
            {
                if (IsSetup == false)
                {
                    IsSetup = true;
                    FrontImage = ResolveGameFrontImage(game);
                    ClearLogo = ResolveClearLogoPath(game);
                    CommunityStarRatingImage = ResolveStarRatingPath(game);
                    UserStarRatingImage = ResolveUserStarRatingPath(game);
                    PlayModeImage = ResolvePlayModePath(game);
                    BackgroundImage = ResolveBackgroundImagePath(game);
                    PlatformClearLogoImage = ResolvePlatformLogoPath(game);
                    VideoPath = ResolveVideoPath(game);
                    TitleToFileName = ResolveGameTitleFileName(game);
                    GameBezelImage = ResolveBezelPath(game, TitleToFileName);
                }
            });
        }

        public void ResetStarRatingImage()
        {
            CommunityStarRatingImage = ResolveStarRatingPath(game);
            UserStarRatingImage = ResolveUserStarRatingPath(game);
        }

        public static char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        public static Uri ResolveGameFrontImage(IGame Game)
        {
            string frontImagePath = Game?.FrontImagePath;
            if (!string.IsNullOrWhiteSpace(frontImagePath))
            {
                string customPath = frontImagePath.Replace(DirectoryInfoHelper.Instance.ApplicationPath, DirectoryInfoHelper.Instance.MediaResolutionSpecificFolder);
                if (!string.IsNullOrWhiteSpace(customPath))
                {
                    if (File.Exists(customPath))
                    {
                        return new Uri(customPath);
                    }
                }
            }

            return ResourceImages.DefaultFrontImage;
        }
        public static Uri ResolveClearLogoPath(IGame Game)
        {
            string clearLogoPath = Game.ClearLogoImagePath;
            if (!string.IsNullOrWhiteSpace(clearLogoPath))
            {
                string customPath = clearLogoPath.Replace(DirectoryInfoHelper.Instance.ApplicationPath, DirectoryInfoHelper.Instance.MediaResolutionSpecificFolder);
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
            string ratingFormatted = String.Format("{0:0.0}", Math.Round(Game.CommunityStarRating, 1));
            string path = $"{DirectoryInfoHelper.Instance.MediaFolder}\\StarRating\\{ratingFormatted}.png";
            if (File.Exists(path))
            {
                return new Uri(path);
            }
            return null;
        }

        public static Uri ResolveUserStarRatingPath(IGame Game)
        {
            string ratingFormatted = String.Format("{0:0.0}", Math.Round(Game.StarRatingFloat, 1));
            string path = $"{DirectoryInfoHelper.Instance.MediaFolder}\\UserStarRating\\{ratingFormatted}.png";
            if (File.Exists(path))
            {
                return new Uri(path);
            }
            return null;
        }

        public static Uri ResolvePlayModePath(IGame Game)
        {
            // resolve play mode path based on game's play mode 
            string path = $"{DirectoryInfoHelper.Instance.MediaFolder}\\PlayMode\\{Game.PlayMode}.png";
            if (File.Exists(path))
            {
                return new Uri(path);
            }

            // resolve fallback path 
            path = $"{DirectoryInfoHelper.Instance.MediaFolder}\\PlayMode\\Fallback.png";
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

            // default resource provided for final fallback in case platform is not found
            return ResourceImages.DefaultBackground;
        }

        public static Uri ResolvePlatformLogoPath(IGame Game)
        {
            string platformClearLogoImagePath = Game.PlatformClearLogoImagePath;
            if (!string.IsNullOrWhiteSpace(platformClearLogoImagePath))
            {
                string customPath = platformClearLogoImagePath.Replace(DirectoryInfoHelper.Instance.ApplicationPath, DirectoryInfoHelper.Instance.MediaResolutionSpecificFolder);
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
            if (!string.IsNullOrWhiteSpace(videoPath) && File.Exists(videoPath))
            {
                return videoPath;
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
        // Game Specific: ..\LaunchBox\Plugins\Eclipse\Media\Bezels\{PLATFORM}\{TitleToFileName}.png
        // MAME bezels ..\LaunchBox\Emulators\MAME\artwork\{game.ApplicationFilePath}\"Bezel.png"
        // Retroarch bezels ..\LaunchBox\Emulators\Retroarch\overlays\GameBezels\{RetroarchPlatform}\{game.ApplicationFilePath}.png
        // fallback bezels are setup in the view because they are dependent on the video aspect ratio so we set them when the video is loaded and the video size is known
        // fallback default plaform bezel path
        // ..\LaunchBox\Plugins\Eclipse\Media\Bezels\{PLATFORM}\Horizontal.png
        // ..\LaunchBox\Plugins\Eclipse\Media\Bezels\{PLATFORM}\Vertical.png
        // fallback default bezel path
        // ..\LaunchBox\Plugins\Eclipse\Media\Bezels\default\Horizontal.png
        // ..\LaunchBox\Plugins\Eclipse\Media\Bezels\default\Vertical.png
        public static Uri ResolveBezelPath(IGame Game, string TitleToFileName)
        {
            try
            {
                // find game specific bezel
                // Game Specific: ..\LaunchBox\Plugins\Eclipse\Media\Bezels\{PLATFORM}\{TitleToFileName}.png
                string gameBezelPath = Path.Combine(DirectoryInfoHelper.Instance.BezelFolder, Game.Platform);
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
                        gameBezelPath = Path.Combine(DirectoryInfoHelper.Instance.ApplicationPath, emulatorFolder, "artwork", Path.GetFileNameWithoutExtension(Game.ApplicationPath), "Bezel.png");

                        if (!string.IsNullOrWhiteSpace(gameBezelPath) && File.Exists(gameBezelPath))
                        {
                            return new Uri(gameBezelPath);
                        }
                    }

                    if (emulator.ApplicationPath.ToLower().Contains("retroarch"))
                    {
                        string retroarchPlatformFolder = "";
                        if (RetroarchHelper.RetroarchPlatformLookup.TryGetValue(Game.Platform, out retroarchPlatformFolder))
                        {
                            gameBezelPath = Path.Combine(DirectoryInfoHelper.Instance.ApplicationPath, emulatorFolder, @"overlays\GameBezels", retroarchPlatformFolder, $"{Path.GetFileNameWithoutExtension(Game.ApplicationPath)}.png");

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
                LogHelper.LogException(ex, $"setting up bezel for {Game.Title}");
            }
            return null;
        }
    }
}
