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

    public class Option<T>
    {
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string ShortDescription { get; set; }
        public string LongDescription { get; set; }
        public T EnumOption{ get; set; }
    }

    public class GameDetailOptionList : INotifyPropertyChanged
    {
        private ListCycle<Option<GameDetailOption>> optionCycle;

        private List<Option<GameDetailOption>> options;
        public List<Option<GameDetailOption>> Options
        {
            get { return options; }
            set
            {
                if (options != value)
                {
                    options = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Options"));
                }
            }
        }

        public GameDetailOptionList()
        {

        }

        public GameDetailOptionList(List<Option<GameDetailOption>> _optionList)
        {
            Options = _optionList;
            optionCycle = new ListCycle<Option<GameDetailOption>>(Options, 4);
            RefreshOptions();
        }

        public void CycleForward()
        {
            optionCycle.CycleForward();
            RefreshOptions();
        }

        public void CycleBackward()
        {
            optionCycle.CycleBackward();
            RefreshOptions();
        }

        private void RefreshOptions()
        {
            Option0 = optionCycle.GetItem(0);
            Option1 = optionCycle.GetItem(1);
            Option2 = optionCycle.GetItem(2);
            Option3 = optionCycle.GetItem(3);
        }

        private Option<GameDetailOption> option0;
        public Option<GameDetailOption> Option0
        {
            get { return option0; }
            set
            {
                if (option0 != value)
                {
                    option0 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Option0"));
                }
            }
        }

        private Option<GameDetailOption> option1;
        public Option<GameDetailOption> Option1
        {
            get { return option1; }
            set
            {
                if (option1 != value)
                {
                    option1 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Option1"));
                }
            }
        }

        private Option<GameDetailOption> option2;
        public Option<GameDetailOption> Option2
        {
            get { return option2; }
            set
            {
                if (option2 != value)
                {
                    option2 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Option2"));
                }
            }
        }

        private Option<GameDetailOption> option3;
        public Option<GameDetailOption> Option3
        {
            get { return option3; }
            set
            {
                if (option3 != value)
                {
                    option3 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Option3"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}
