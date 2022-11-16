using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.Models
{
    public enum GameDetailOption
    {
        Play,
        Favorite,
        Rating,
        MoreLikeThis
    }

    public class Option<T> : INotifyPropertyChanged

    {
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string ShortDescription { get; set; }
        public string LongDescription { get; set; }
        public T EnumOption{ get; set; }
        public bool Default { get; set; }

        private bool selected;
        public bool Selected 
        {
            get { return selected; }
            set
            {
                selected = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Selected"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}
