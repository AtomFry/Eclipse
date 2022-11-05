using System.Collections.Generic;
using System.ComponentModel;

namespace Eclipse.Models
{
    public static class ListCategoryTypeExtensionMethods
    {
        public static bool IsValidForCustomList(this ListCategoryType listCategoryType)
        {
            bool isValid;

            switch(listCategoryType)
            {
                case ListCategoryType.MoreLikeThis:
                case ListCategoryType.RandomGame:
                case ListCategoryType.VoiceSearch:
                    isValid = false;
                    break;

                default:
                    isValid = true;
                    break;
            }

            return isValid;
        }
    }

    public enum ListCategoryType
    {
        VoiceSearch,
        RandomGame,
        ReleaseYear,
        Platform,
        Genre,
        Series,
        Playlist,
        PlayMode,
        Developer,
        Publisher,
        MoreLikeThis
    }

    public class OptionList : INotifyPropertyChanged
    {
        private ListCycle<Option<ListCategoryType>> optionCycle;

        private List<Option<ListCategoryType>> options;
        public List<Option<ListCategoryType>> Options
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

        public OptionList()
        {

        }

        public OptionList(List<Option<ListCategoryType>> _optionList)
        {
            Options = _optionList;
            optionCycle = new ListCycle<Option<ListCategoryType>>(Options, _optionList.Count);
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
            Option4 = optionCycle.GetItem(4);
            Option5 = optionCycle.GetItem(5);
            Option6 = optionCycle.GetItem(6);
            Option7 = optionCycle.GetItem(7);
            Option8 = optionCycle.GetItem(8);
            Option9 = optionCycle.GetItem(9);
        }

        private Option<ListCategoryType> option0;
        public Option<ListCategoryType> Option0
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

        private Option<ListCategoryType> option1;
        public Option<ListCategoryType> Option1
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

        private Option<ListCategoryType> option2;
        public Option<ListCategoryType> Option2
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

        private Option<ListCategoryType> option3;
        public Option<ListCategoryType> Option3
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

        private Option<ListCategoryType> option4;
        public Option<ListCategoryType> Option4
        {
            get { return option4; }
            set
            {
                if (option4 != value)
                {
                    option4 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Option4"));
                }
            }
        }

        private Option<ListCategoryType> option5;
        public Option<ListCategoryType> Option5
        {
            get { return option5; }
            set
            {
                if (option5 != value)
                {
                    option5 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Option5"));
                }
            }
        }

        private Option<ListCategoryType> option6;
        public Option<ListCategoryType> Option6
        {
            get { return option6; }
            set
            {
                if (option6 != value)
                {
                    option6 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Option6"));
                }
            }
        }

        private Option<ListCategoryType> option7;
        public Option<ListCategoryType> Option7
        {
            get { return option7; }
            set
            {
                if (option7 != value)
                {
                    option7 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Option7"));
                }
            }
        }

        private Option<ListCategoryType> option8;
        public Option<ListCategoryType> Option8
        {
            get { return option8; }
            set
            {
                if (option8 != value)
                {
                    option8 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Option8"));
                }
            }
        }

        private Option<ListCategoryType> option9;
        public Option<ListCategoryType> Option9
        {
            get { return option9; }
            set
            {
                if (option9 != value)
                {
                    option9 = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Option9"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }

    public class Option
    {
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public string ShortDescription { get; set; }
        public string LongDescription { get; set; }
        public ListCategoryType ListCategoryType{get; set;}
    }
}
