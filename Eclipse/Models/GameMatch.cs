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
    public class GameMatch : INotifyPropertyChanged
    {


        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public static char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        public IGame Game { get; set; }
        public TitleMatchType TitleMatchType { get; set; }
        public string ConvertedTitle { get; set; }
        private string titleToFileName;
        public string TitleToFileName
        {
            get
            {
                if(titleToFileName == null)
                {
                    titleToFileName = Game.Title;
                    foreach(char invalidChar in InvalidFileNameChars)
                    {
                        titleToFileName = titleToFileName.Replace(invalidChar, '_');
                    }
                }
                return titleToFileName;
            }
        }

        

        public GameMatch()
        {
        }

        public GameMatch(IGame game, TitleMatchType titleMatchType, string convertedTitle = "")
        {
            Game = game;
            TitleMatchType = titleMatchType;
            frontImage = ResourceImages.GameFrontDummy;
            ConvertedTitle = convertedTitle;
        }

        public static ConcurrentDictionary<string, Uri> GameFrontImages = new ConcurrentDictionary<string, Uri>();
        // public static Dictionary<string, Uri> GameFrontImages = new Dictionary<string, Uri>();

        public void SetupFrontImage()
        {
            Uri frontImage;
            if (GameFrontImages.TryGetValue(Game.Id, out frontImage))
            {
                FrontImage = frontImage;
            }
            /*
            else
            {
                string frontImagePath = Game?.FrontImagePath;
                if (!string.IsNullOrWhiteSpace(frontImagePath))
                {
                    string customPath = frontImagePath.Replace(Helpers.ApplicationPath, Helpers.MediaFolder);
                    if (!string.IsNullOrWhiteSpace(customPath))
                    {
                        if (File.Exists(customPath))
                        {
                            FrontImage = new Uri(customPath);
                            GameFrontImages.TryAdd(Game.Id, FrontImage);
                        }
                    }
                }
            }
            */
        }

        public static void AddGameToFrontImageDictionary(IGame game)
        {
            string frontImagePath = game?.FrontImagePath;
            if (!string.IsNullOrWhiteSpace(frontImagePath))
            {
                string customPath = frontImagePath.Replace(Helpers.ApplicationPath, Helpers.MediaFolder);
                if (!string.IsNullOrWhiteSpace(customPath))
                {
                    if (File.Exists(customPath))
                    {
                        // regular dictionary version 
                        // GameFrontImages.Add(game.Id, new Uri(customPath));
                        
                        // concurrent dictionary version
                        GameFrontImages.TryAdd(game.Id, new Uri(customPath));
                    }
                }
            }
        }


        public Func<string, Uri, Uri> GameFrontDictionaryUpdate = (string x, Uri y) => y;

        private Uri frontImage;
        public Uri FrontImage
        {
            get
            {
                return (frontImage);
            }
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
            get
            {
                if (clearLogo == null)
                {
                    string clearLogoPath = Game.ClearLogoImagePath;
                    if (!string.IsNullOrWhiteSpace(clearLogoPath))
                    {
                        string customPath = clearLogoPath.Replace(Helpers.ApplicationPath, Helpers.MediaFolder);
                        if(!string.IsNullOrWhiteSpace(customPath))
                        {
                            if(File.Exists(customPath))
                            {
                                clearLogo = new Uri(customPath);
                            }
                        }
                    }
                }
                return (clearLogo);
            }
        }


        private Uri communityStarRatingImage;
        public Uri CommunityStarRatingImage
        {
            get
            {
                if(communityStarRatingImage == null)
                {
                    string ratingFormatted = String.Format("{0:0.0}", Math.Round(Game.CommunityOrLocalStarRating, 1));
                    string path = $"{Helpers.MediaFolder}\\StarRating\\{ratingFormatted}.png";
                    communityStarRatingImage = new Uri(path);
                }
                return communityStarRatingImage;
            }
        }

        private Uri playModeImage;
        public Uri PlayModeImage
        {
            get
            {
                if (playModeImage == null)
                {
                    string path = $"{Helpers.MediaFolder}\\PlayMode\\{Game.PlayMode}.png";
                    if(!File.Exists(path))
                    {
                        path = $"{Helpers.MediaFolder}\\PlayMode\\Fallback.png";
                    }
                    playModeImage = new Uri(path);
                }
                return playModeImage;
            }
        }

        private Uri backgroundImage;
        public Uri BackgroundImage
        {
            get
            {
                if (backgroundImage == null)
                {
                    string backgroundImagePath = Game.BackgroundImagePath ?? Game.ScreenshotImagePath;
                    if (!string.IsNullOrWhiteSpace(backgroundImagePath))
                    {
                        backgroundImage = new Uri(backgroundImagePath);
                    }
                }
                return backgroundImage;
            }
        }

        private Uri platformClearLogoImage;
        public Uri PlatformClearLogoImage
        {
            get
            {
                if(platformClearLogoImage == null)
                {
                    string platformClearLogoImagePath = Game.PlatformClearLogoImagePath;
                    if(!string.IsNullOrWhiteSpace(platformClearLogoImagePath))
                    {
                        string customPath = platformClearLogoImagePath.Replace(Helpers.ApplicationPath, Helpers.MediaFolder);
                        if(!string.IsNullOrWhiteSpace(customPath))
                        {
                            if(File.Exists(customPath))
                            {
                                platformClearLogoImage = new Uri(customPath);
                            }
                        }
                    }
                }
                return platformClearLogoImage;
            }
        }

        private string bezelPath;
        private void SetBezelPath()
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
                        bezelPath = gameBezelFiles[0];
                        if (!string.IsNullOrWhiteSpace(bezelPath) && File.Exists(bezelPath))
                        {
                            return;
                        }
                    }
                }

                // check for MAME or retroarch bezels
                // MAME: ..\LaunchBox\Emulators\MAME\artwork\{game.ApplicationFilePath}\"Bezel.png"
                // Retroarch: ..\LaunchBox\Emulators\Retroarch\overlays\GameBezels\{CONVERTTORETROARCHPLATFORM}\{game.ApplicationFilePath}.png
                IEmulator emulator = PluginHelper.DataManager.GetEmulatorById(Game.EmulatorId);
                if (!string.IsNullOrWhiteSpace(emulator?.ApplicationPath))
                {
                    string emulatorFolder = Path.GetDirectoryName(emulator.ApplicationPath);

                    if (emulator.ApplicationPath.ToLower().Contains("mame"))
                    {
                        bezelPath = Path.Combine(Helpers.ApplicationPath, emulatorFolder, "artwork", Path.GetFileNameWithoutExtension(Game.ApplicationPath), "Bezel.png");
                    }
                    else if (emulator.ApplicationPath.ToLower().Contains("retroarch"))
                    {
                        string retroarchPlatformFolder = "";
                        if (Helpers.RetroarchPlatformLookup.TryGetValue(Game.Platform, out retroarchPlatformFolder))
                        {
                            bezelPath = Path.Combine(Helpers.ApplicationPath, emulatorFolder, @"overlays\GameBezels", retroarchPlatformFolder, $"{Path.GetFileNameWithoutExtension(Game.ApplicationPath)}.png");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(bezelPath) && File.Exists(bezelPath))
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Helpers.LogException(ex, $"setting up bezel for {Game.Title}");
            }
        }

        private Uri gameBezelImage;
        public Uri GameBezelImage
        {
            get
            {
                if(bezelPath == null)
                {
                    SetBezelPath();
                }

                if (gameBezelImage == null)
                {
                    if(!string.IsNullOrWhiteSpace(bezelPath))
                    {
                        if (File.Exists(bezelPath))
                        {
                            gameBezelImage = new Uri(bezelPath);
                        }
                    }
                }
                return gameBezelImage;
            }
        }

        public string VideoPath
        {
            get
            {
                return Game.GetVideoPath();
            }
        }

        public int? matchPercentage;
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
            matchPercentage = (int)(matchTypePortion);
        }


        private string matchDescription;
        public string MatchDescription
        {
            get 
            {
                // default to match percentage based on star rating - it should be overridden for voice searches
                if(matchPercentage == null)
                {
                    // for non-voice match, match percentage = star rating (0-5) * 20
                    matchPercentage = (int)(Game.CommunityOrLocalStarRating * 20);
                }

                if(matchDescription == null)
                {
                    matchDescription = $"{string.Format("{0:00}", matchPercentage)}% match";
                }
                return matchDescription;
            }
        }

        public string ReleaseDate
        {
            get
            {
                if (Game.ReleaseDate?.Year == null)
                {
                    return ("    ");
                }
                return (Game.ReleaseDate?.Year.ToString());
            }
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
