using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Unbroken.LaunchBox.Plugins.Data;
using Unbroken.LaunchBox.Plugins.RetroAchievements;

namespace BigBoxNetflixUI.Models
{
    public class GameMatch
    {
        private static string _appPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        public IGame Game { get; set; }
        public TitleMatchType TitleMatchType { get; set; }
     
        public GameMatch()
        {
        }

        public GameMatch(IGame game, TitleMatchType titleMatchType)
        {
            Game = game;
            TitleMatchType = titleMatchType;
        }

        private BitmapImage frontImage;
        public BitmapImage FrontImage
        {
            get
            {
                if (frontImage == null)
                {
                    if (!string.IsNullOrWhiteSpace(Game.FrontImagePath))
                    {
                        // todo: set fallback image to local resource if not found
                        frontImage = new BitmapImage(new Uri(Game.FrontImagePath));
                    }
                }
                return frontImage;
            }
        }

        private BitmapImage clearLogo;
        public BitmapImage ClearLogo
        {
            get
            {
                if(clearLogo == null)
                {
                    if(!string.IsNullOrWhiteSpace(Game.ClearLogoImagePath))
                    {
                        // todo: set fallback image to local resource if not found
                        clearLogo = new BitmapImage(new Uri(Game.ClearLogoImagePath));
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
                    string path = _appPath + $"\\Plugins\\BigBoxNetflixUI\\Media\\StarRating\\{rating}.png";
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
                    string path = _appPath + $"\\Plugins\\BigBoxNetflixUI\\Media\\PlayMode\\{Game.PlayMode}.png";
                    // todo: set fallback image to local resource if not found
                    playModeImage = new Uri(path);
                }
                return playModeImage;
            }
        }

        private BitmapImage backgroundImage;
        public BitmapImage BackgroundImage
        {
            get
            {
                if (backgroundImage == null)
                {
                    if (!string.IsNullOrWhiteSpace(Game.BackgroundImagePath))
                    {
                        // todo: set fallback image to local resource if not found
                        backgroundImage = new BitmapImage(new Uri(Game.BackgroundImagePath));
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
