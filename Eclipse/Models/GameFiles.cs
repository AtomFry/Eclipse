using Eclipse.Helpers;
using Eclipse.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Models
{
    public class GameVersion : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public IGame Game { get; set; }

        public IAdditionalApplication AdditionalApplication { get; set; }

        public GameVersion(IGame _game, IAdditionalApplication _additionalApplication)
        {
            Game = _game;
            AdditionalApplication = _additionalApplication;

            if (AdditionalApplication != null)
            {
                switch (EclipseSettingsDataProvider.Instance.EclipseSettings.AdditionalApplicationDisplayField)
                {
                    case AdditionalApplicationDisplayField.Name:
                        Description = AdditionalApplication.Name;

                        if (EclipseSettingsDataProvider.Instance.EclipseSettings.AdditionalVersionsRemovePlayPrefix)
                        {
                            if (AdditionalApplication?.Name?.StartsWith("Play ") == true)
                            {
                                Description = Description.Substring(5);
                            }
                        }

                        if (EclipseSettingsDataProvider.Instance.EclipseSettings.AdditionalVersionsRemoveVersionPostfix)
                        {
                            if (AdditionalApplication?.Name?.EndsWith("Version...") == true)
                            {
                                Description = Description.Substring(0, Description.IndexOf("Version..."));
                            }
                        }

                        break;

                    case AdditionalApplicationDisplayField.Region:
                        Description = AdditionalApplication.Region;
                        break;

                    case AdditionalApplicationDisplayField.Version:
                        Description = AdditionalApplication.Version;
                        break;

                    default:
                        break;
                }
            }
            else
            {
                /*
                Description = string.IsNullOrWhiteSpace(Game.Version) ?
                    string.IsNullOrWhiteSpace(Game.Region) ?
                    Game.Title :
                    Game.Region :
                    Game.Version;
                */
                Description = Game.Title;
            }
        }

        private string description;
        public string Description
        {
            get => description;
            set
            {
                description = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Description"));
            }
        }

        private bool selected;
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Selected"));
            }
        }
    }

    public class GameVersionList : INotifyPropertyChanged
    {
        public ObservableCollection<GameVersion> DisplayedGameVersions { get; set; }

        public int SelectedIndex { get; set; }

        private GameVersion selectedGameVersion;
        public GameVersion SelectedGameVersion
        {
            get => selectedGameVersion;
            set
            {
                selectedGameVersion = value;
                PropertyChanged(this, new PropertyChangedEventArgs("SelectedGameVersion"));
            }
        }

        private List<GameVersion> gameVersions;
        public List<GameVersion> GameVersions
        {
            get => gameVersions;
            set
            {
                gameVersions = value;
                PropertyChanged(this, new PropertyChangedEventArgs("GameVersions"));
            }
        }

        public GameVersionList(List<GameVersion> _gameVersions)
        {
            DisplayedGameVersions = new ObservableCollection<GameVersion>();

            GameVersions = _gameVersions;

            SelectedIndex = 0;

            if (GameVersions.Count > 0)
            {
                GameVersions[SelectedIndex].Selected = true;
            }

            RefreshOptions();
        }

        public void CycleForward()
        {
            GameVersions[SelectedIndex].Selected = false;
            if (SelectedIndex + 1 >= GameVersions.Count)
            {
                SelectedIndex = 0;
            }
            else
            {
                SelectedIndex++;
            }
            GameVersions[SelectedIndex].Selected = true;

            SelectedGameVersion = GameVersions[SelectedIndex];
        }

        public void CycleBackward()
        {
            GameVersions[SelectedIndex].Selected = false;
            if (SelectedIndex - 1 < 0)
            {
                SelectedIndex = GameVersions.Count - 1;
            }
            else
            {
                SelectedIndex--;
            }
            GameVersions[SelectedIndex].Selected = true;

            SelectedGameVersion = GameVersions[SelectedIndex];
        }

        private void RefreshOptions()
        {
            DisplayedGameVersions.Clear();

            if (GameVersions != null)
            {
                foreach (GameVersion option in GameVersions)
                {
                    DisplayedGameVersions.Add(option);
                }
            }

            SelectedGameVersion = GameVersions[SelectedIndex];

            HasAdditionalVersions = DisplayedGameVersions?.Count() > 1;
        }

        private bool hasAdditionalVersions;
        public bool HasAdditionalVersions
        {
            get => hasAdditionalVersions;
            set
            {
                hasAdditionalVersions = value;
                PropertyChanged(this, new PropertyChangedEventArgs("HasAdditionalVersions"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }

    public class GameFiles : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private GameVersionList gameVersionList;
        public GameVersionList GameVersionList
        {
            get => gameVersionList;
            set
            {
                gameVersionList = value;
                PropertyChanged(this, new PropertyChangedEventArgs("GameVersionList"));
            }
        }

        public GameFiles(IGame game)
        {
            IsSetup = false;
            Game = game;
            FrontImage = ResourceImages.GameFrontDummy;
            BackImage = ResourceImages.GameFrontDummy;
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

        private Uri backImage;
        public Uri BackImage
        {
            get { return backImage; }
            set
            {
                if (backImage != value)
                {
                    backImage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("BackImage"));
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

        private Uri bigFrontImage;
        public Uri BigFrontImage
        {
            get { return bigFrontImage; }
            set
            {
                if (bigFrontImage != value)
                {
                    bigFrontImage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("BigFrontImage"));
                }
            }
        }

        private Uri bigBackImage;
        public Uri BigBackImage
        {
            get { return bigBackImage; }
            set
            {
                if (bigBackImage != value)
                {
                    bigBackImage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("BigBackImage"));
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

                    lbFrontImagePath = game?.FrontImagePath;
                    lbBackImagePath = game?.BackImagePath;

                    BigFrontImage = ResolveBigFrontImage();
                    BigBackImage = ResolveBigBackImage();
                    FrontImage = ResolveGameFrontImage();
                    BackImage = ResolveGameBackImage();

                    ClearLogo = ResolveClearLogoPath(game);
                    CommunityStarRatingImage = ResolveStarRatingPath(game);
                    UserStarRatingImage = ResolveUserStarRatingPath(game);
                    PlayModeImage = ResolvePlayModePath(game);
                    BackgroundImage = ResolveBackgroundImagePath(game);
                    PlatformClearLogoImage = ResolvePlatformLogoPath(game);
                    VideoPath = ResolveVideoPath(game);
                    TitleToFileName = ResolveGameTitleFileName(game);
                    GameBezelImage = ResolveBezelPath(game, TitleToFileName);

                    GameVersionList = ResolveAdditionalGameVersionList(game);
                }
            });
        }

        private GameVersionList ResolveAdditionalGameVersionList(IGame game)
        {
            IAdditionalApplication[] allAdditionalAppsArray = game.GetAllAdditionalApplications();

            List<GameVersion> additionalGameVersions = new List<GameVersion>();

            if (EclipseSettingsDataProvider.Instance.EclipseSettings.AdditionalVersionsEnable)
            {
                bool includeMainGame = true;

                // add any additional apps
                if (allAdditionalAppsArray != null)
                {
                    foreach (IAdditionalApplication additionalApplication in allAdditionalAppsArray)
                    {
                        // if an additional app is the same path as the game, flag it so we don't add it
                        if (additionalApplication.ApplicationPath == game.ApplicationPath)
                        {
                            includeMainGame = false;
                        }

                        // exclude if additional apps that are set to run before launching are set to be excluded
                        if (additionalApplication.AutoRunBefore
                            && EclipseSettingsDataProvider.Instance.EclipseSettings.AdditionalVersionsExcludeRunBefore)
                        {
                            continue;
                        }

                        // exclude if additional apps that are set to run after launching are set to be excluded
                        if (additionalApplication.AutoRunAfter
                            && EclipseSettingsDataProvider.Instance.EclipseSettings.AdditionalVersionsExcludeRunAfter)
                        {
                            continue;
                        }

                        // exclude if not using emulator/dosbox and set to only include emulator/dosbox 
                        if (!additionalApplication.UseEmulator
                            && !additionalApplication.UseDosBox
                            && EclipseSettingsDataProvider.Instance.EclipseSettings.AdditionalVersionsOnlyEmulatorOrDosBox)
                        {
                            continue;
                        }

                        additionalGameVersions.Add(new GameVersion(game, additionalApplication));
                    }
                }

                // only include the main game if it wasn't already included as an additional app
                if (includeMainGame)
                {
                    // add the game 
                    additionalGameVersions.Add(new GameVersion(game, null));
                }
            }



            GameVersionList gameVersionList = new GameVersionList(additionalGameVersions);
            return gameVersionList;
        }

        private Uri ResolveBigBackImage()
        {
            if (!string.IsNullOrWhiteSpace(lbBackImagePath))
            {
                return new Uri(lbBackImagePath);
            }

            return ResourceImages.DefaultFrontImage;
        }

        private Uri ResolveBigFrontImage()
        {
            if (!string.IsNullOrWhiteSpace(lbFrontImagePath))
            {
                return new Uri(lbFrontImagePath);
            }

            return ResourceImages.DefaultFrontImage;
        }

        private string lbFrontImagePath;
        private string lbBackImagePath;

        public void ResetStarRatingImage()
        {
            CommunityStarRatingImage = ResolveStarRatingPath(game);
            UserStarRatingImage = ResolveUserStarRatingPath(game);
        }

        public static char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

       

        public Uri ResolveGameFrontImage()
        {
            string frontImagePath = lbFrontImagePath;
            if (!string.IsNullOrWhiteSpace(frontImagePath))
            {
                string customPath = frontImagePath.Replace(DirectoryInfoHelper.Instance.ApplicationPath, DirectoryInfoHelper.Instance.MediaResolutionSpecificFolder);
                if (!string.IsNullOrWhiteSpace(customPath))
                {
                    if (!File.Exists(customPath))
                    {
                        // scale the image and cache it
                        ImageScaler.ScaleImage(frontImagePath, customPath);
                    }
                    return new Uri(customPath);
                }
            }

            return ResourceImages.DefaultFrontImage;
        }

        public Uri ResolveGameBackImage()
        {
            string backImagePath = lbBackImagePath;
            if (!string.IsNullOrWhiteSpace(backImagePath))
            {
                string customPath = backImagePath.Replace(DirectoryInfoHelper.Instance.ApplicationPath, DirectoryInfoHelper.Instance.MediaResolutionSpecificFolder);
                if (!string.IsNullOrWhiteSpace(customPath))
                {
                    if (!File.Exists(customPath))
                    {
                        // scale the image and cache it
                        ImageScaler.ScaleImage(backImagePath, customPath);
                    }
                    return new Uri(customPath);
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
                    if (!File.Exists(customPath))
                    {
                        // crop the image and cache it
                        ImageScaler.CropImage(clearLogoPath, customPath);
                    }
                    return new Uri(customPath);
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
