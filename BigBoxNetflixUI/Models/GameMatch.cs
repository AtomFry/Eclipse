using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Unbroken.LaunchBox.Plugins.Data;
using Unbroken.LaunchBox.Plugins.RetroAchievements;

namespace BigBoxNetflixUI.Models
{
    public class GameMatch
    {
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
                        frontImage = new BitmapImage(new Uri(Game.FrontImagePath));
                    }
                }

                return frontImage;
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
