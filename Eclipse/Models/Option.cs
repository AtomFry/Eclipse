using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

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
        public ObservableCollection<Option<ListCategoryType>> DisplayedListCategoryTypes { get; set; }

        public int SelectedIndex { get; set; }

        public Option<ListCategoryType> SelectedOption => Options[SelectedIndex];

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
            DisplayedListCategoryTypes = new ObservableCollection<Option<ListCategoryType>>();

            Options = _optionList;

            SelectedIndex = 0;
            Options[SelectedIndex].Selected = true;

            RefreshOptions();
        }

        public void CycleForward()
        {
            Options[SelectedIndex].Selected = false;
            if (SelectedIndex + 1 >= Options.Count)
            {
                SelectedIndex = 0;
            }
            else
            {
                SelectedIndex++;
            }
            Options[SelectedIndex].Selected = true;
        }

        public void CycleBackward()
        {
            Options[SelectedIndex].Selected = false;
            if (SelectedIndex - 1 < 0)
            {
                SelectedIndex = Options.Count - 1;
            }
            else
            {
                SelectedIndex--;
            }
            Options[SelectedIndex].Selected = true;
        }

        private void RefreshOptions()
        {
            DisplayedListCategoryTypes.Clear();
            foreach (Option<ListCategoryType> option in Options)
            {
                DisplayedListCategoryTypes.Add(option);
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
