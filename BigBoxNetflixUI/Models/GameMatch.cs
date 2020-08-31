using BigBoxNetflixUI.View;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Unbroken.LaunchBox.Plugins.Data;

namespace BigBoxNetflixUI.Models
{
    public class GameMatch : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public static Uri GameFrontDummy = new Uri($"{Helpers.ResourceFolder}/NES_BoxFront_Template.png");

        public IGame Game { get; set; }
        public TitleMatchType TitleMatchType { get; set; }
     
        public GameMatch()
        {
        }

        public GameMatch(IGame game, TitleMatchType titleMatchType)
        {
            Game = game;
            TitleMatchType = titleMatchType;
            frontImage = GameFrontDummy;
        }

        public void SetupFrontImage()
        {
            string frontImagePath = Game?.FrontImagePath;
            if(!string.IsNullOrWhiteSpace(frontImagePath))
            {
                FrontImage = new Uri(frontImagePath);
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
                if(clearLogo == null)
                {
                    if(!string.IsNullOrWhiteSpace(Game.ClearLogoImagePath))
                    {
                        // todo: set fallback image to local resource if not found
                        clearLogo = new Uri(Game.ClearLogoImagePath);
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
                    double rating = Math.Round(Game.CommunityStarRating, 1);
                    string path = $"{Helpers.MediaFolder}\\StarRating\\{rating}.png";
                    // todo: set fallback image to local resource if not found
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
                    // todo: set fallback image to local resource if not found
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
                    if (!string.IsNullOrWhiteSpace(Game.BackgroundImagePath))
                    {
                        // todo: set fallback image to local resource if not found
                        backgroundImage = new Uri(Game.BackgroundImagePath);
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

        public string MatchDescription
        {
            get 
            {
                // todo: define match as percentage (based on match type) of confidence
                // todo: define visibility and only display if matching
                int tempMatch = 98;
                return $"{tempMatch}% match";
            }
        }

        public string ReleaseDate
        {
            get
            {
                if (Game.ReleaseDate?.Year == null)
                {
                    return ("");
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
