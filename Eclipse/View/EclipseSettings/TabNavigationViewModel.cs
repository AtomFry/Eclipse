using Eclipse.Helpers;
using Eclipse.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Eclipse.View.EclipseSettings
{
    public class TabNavigationViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> TabPages { get; }

        private string selectedTabPage;
        public string SelectedTabPage
        {
            get { return selectedTabPage; }
            set
            {
                selectedTabPage = value;
                OnPropertyChanged();
                UpdateTabVisibility();
            }
        }

        private Visibility listsTabVisibility;
        public Visibility ListsTabVisibility
        {
            get { return listsTabVisibility; }
            set
            {
                listsTabVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility inputsTabVisibility;
        public Visibility InputsTabVisibility
        {
            get { return inputsTabVisibility; }
            set
            {
                inputsTabVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility versionsTabVisibility;
        public Visibility VersionsTabVisibility
        {
            get { return versionsTabVisibility; }
            set
            {
                versionsTabVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility customListsTabVisibility;
        public Visibility CustomListsTabVisibility
        {
            get { return customListsTabVisibility; }
            set
            {
                customListsTabVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility otherTabVisibility;
        public Visibility OtherTabVisibility
        {
            get { return otherTabVisibility; }
            set
            {
                otherTabVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility boxMarginTabVisibility;
        public Visibility BoxMarginTabVisibility
        {
            get { return boxMarginTabVisibility; }
            set
            {
                boxMarginTabVisibility = value;
                OnPropertyChanged();
            }
        }

        public TabNavigationViewModel()
        {
            TabPages = new ObservableCollection<string>();
            InitializeTabPages();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void InitializeTabPages()
        {
            TabPages.Add(EclipseSettingsTabs.Lists);
            TabPages.Add(EclipseSettingsTabs.Inputs);
            TabPages.Add(EclipseSettingsTabs.Versions);
            TabPages.Add(EclipseSettingsTabs.CustomLists);
            TabPages.Add(EclipseSettingsTabs.Other);
            TabPages.Add(EclipseSettingsTabs.BoxMargin);

            SelectedTabPage = EclipseSettingsTabs.Lists;
        }

        private void UpdateTabVisibility()
        {
            switch (SelectedTabPage)
            {
                case EclipseSettingsTabs.Lists:
                    ListsTabVisibility = Visibility.Visible;
                    InputsTabVisibility = Visibility.Hidden;
                    VersionsTabVisibility = Visibility.Hidden;
                    CustomListsTabVisibility = Visibility.Hidden;
                    OtherTabVisibility = Visibility.Hidden;
                    BoxMarginTabVisibility = Visibility.Hidden;
                    break;
                case EclipseSettingsTabs.Inputs:
                    ListsTabVisibility = Visibility.Hidden;
                    InputsTabVisibility = Visibility.Visible;
                    VersionsTabVisibility = Visibility.Hidden;
                    CustomListsTabVisibility = Visibility.Hidden;
                    OtherTabVisibility = Visibility.Hidden;
                    BoxMarginTabVisibility = Visibility.Hidden;
                    break;
                case EclipseSettingsTabs.Versions:
                    ListsTabVisibility = Visibility.Hidden;
                    InputsTabVisibility = Visibility.Hidden;
                    VersionsTabVisibility = Visibility.Visible;
                    CustomListsTabVisibility = Visibility.Hidden;
                    OtherTabVisibility = Visibility.Hidden;
                    BoxMarginTabVisibility = Visibility.Hidden;
                    break;
                case EclipseSettingsTabs.CustomLists:
                    ListsTabVisibility = Visibility.Hidden;
                    InputsTabVisibility = Visibility.Hidden;
                    VersionsTabVisibility = Visibility.Hidden;
                    CustomListsTabVisibility = Visibility.Visible;
                    OtherTabVisibility = Visibility.Hidden;
                    BoxMarginTabVisibility = Visibility.Hidden;
                    break;
                case EclipseSettingsTabs.Other:
                    ListsTabVisibility = Visibility.Hidden;
                    InputsTabVisibility = Visibility.Hidden;
                    VersionsTabVisibility = Visibility.Hidden;
                    CustomListsTabVisibility = Visibility.Hidden;
                    OtherTabVisibility = Visibility.Visible;
                    BoxMarginTabVisibility = Visibility.Hidden;
                    break;
                case EclipseSettingsTabs.BoxMargin:
                    ListsTabVisibility = Visibility.Hidden;
                    InputsTabVisibility = Visibility.Hidden;
                    VersionsTabVisibility = Visibility.Hidden;
                    CustomListsTabVisibility = Visibility.Hidden;
                    OtherTabVisibility = Visibility.Hidden;
                    BoxMarginTabVisibility = Visibility.Visible;
                    break;
            }
        }
    }
}