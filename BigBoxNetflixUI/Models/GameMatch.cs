using BigBoxNetflixUI.View;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Unbroken.LaunchBox.Plugins.Data;

namespace BigBoxNetflixUI.Models
{
    public class GameMatch : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public IGame Game { get; set; }
        public TitleMatchType TitleMatchType { get; set; }
     
        public GameMatch()
        {
        }

        public GameMatch(IGame game, TitleMatchType titleMatchType)
        {
            Game = game;
            TitleMatchType = titleMatchType;
            frontImage = ResourceImages.GameFrontDummy;
        }

        public void SetupFrontImage()
        {
            string frontImagePath = Game?.FrontImagePath;
            if(!string.IsNullOrWhiteSpace(frontImagePath))
            {
                string customPath = frontImagePath.Replace(Helpers.ApplicationPath, Helpers.MediaFolder);
                if(!string.IsNullOrWhiteSpace(customPath))
                {
                    FrontImage = new Uri(customPath);
                }
            }
            // todo: implement fall back image
        }

        private Uri frontImage;
        public Uri FrontImage
        {
            get
            {
                return (frontImage);
            }
            set
            {
                try
                {
                    if (frontImage != value)
                    {
                        frontImage = value;
                        PropertyChanged(this, new PropertyChangedEventArgs("FrontImage"));
                    }
                }
                catch(Exception ex)
                {
                    Helpers.LogException(ex, $"Setting front image for {Game.Title}");
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
                        clearLogo = new Uri(clearLogoPath);
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
                    string backgroundImagePath = Game.BackgroundImagePath;
                    if (!string.IsNullOrWhiteSpace(backgroundImagePath))
                    {
                        // todo: set fallback image to local resource if not found
                        backgroundImage = new Uri(backgroundImagePath);
                    }
                }
                return backgroundImage;
            }
        }


        public string VideoPath
        {
            get
            {
                return Game.GetVideoPath();
            }
        }

        private string matchDescription;
        public string MatchDescription
        {
            get 
            {
                if(matchDescription == null)
                {
                    // todo: probably create subclass for voice match and override the calculation
                    // for non-voice match, match percentage = start rating (0-5) * 20
                    // for voice match, lots to consider...

                    /*
                     * Confidence of spoken phrase (0-1)
                     * Match level (full title (100), main title (95), subtitle (90), phrase (70+?))
                     * For partial phrase match - would like to increase it - maybe max out at 95 so 70 + (29 * (length of phrase / length of title))
                     * Length of phrase vs length of title?  (0-1)
                     */
                    // todo: define match as percentage (based on match type) of confidence
                    int tempMatch = (int)(Game.CommunityOrLocalStarRating * 20);

                    // prefix with space if less than 10 
                    matchDescription = $"{string.Format("{0:00}", tempMatch)}% match";
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
