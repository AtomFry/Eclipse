using Eclipse.Event;
using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Eclipse.View.EclipseSettings
{
    public class EclipseSettingsViewModel : ViewModelBase
    {
        private CustomListDefinitionDataProvider customListDefinitionDataProvider;
        private IEventAggregator eventAggregator;
        public ObservableCollection<CustomListDefinition> CustomListDefinitions { get; }
        public ICommand EditCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand MoveUpCustomListCommand { get; }
        public ICommand MoveDownCustomListCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private CustomListDefinition selectedCustomListDefinition;

        public CustomListDefinition SelectedCustomListDefinition
        {
            get { return selectedCustomListDefinition; }
            set
            {
                selectedCustomListDefinition = value;
                OnPropertyChanged();
                InvalidateCommands();
            }
        }

        private CustomListDefinitionEditViewModel customListDefinitionEditViewModel;
        private CustomListDefinitionEditView customListDefinitionEditView;

        public EclipseSettingsViewModel()
        {
            TabPages = new ObservableCollection<string>();
            InitializeTabPages();
            InitializeListTypes();

            CustomListDefinitions = new ObservableCollection<CustomListDefinition>();

            eventAggregator = EventAggregatorHelper.Instance.EventAggregator;
            customListDefinitionDataProvider = new CustomListDefinitionDataProvider();

            EditCommand = new DelegateCommand(OnEditExecute, OnEditCanExecute);
            AddCommand = new DelegateCommand(OnAddExecute, OnAddCanExecute);
            DeleteCommand = new DelegateCommand(OnDeleteExecuteAsync, OnDeleteCanExecute);
            CloseCommand = new DelegateCommand(OnCloseExecute);
            MoveUpCustomListCommand = new DelegateCommand(OnMoveUpCustomListExecute);
            MoveDownCustomListCommand = new DelegateCommand(OnMoveDownCustomListExecute);
            SaveCommand = new DelegateCommand(OnSaveExecute);
            CancelCommand = new DelegateCommand(OnCancelExecute);

            eventAggregator.GetEvent<CustomListDefinitionSaved>().Subscribe(OnCustomListDefinitionSavedSavedAsync);
            eventAggregator.GetEvent<CustomListDefinitionEditClosing>().Subscribe(OnCustomListDefinitionEditClosed);
        }

        private void OnCancelExecute()
        {
            eventAggregator.GetEvent<EclipseSettingsClose>().Publish();
        }

        private async void OnSaveExecute()
        {
            SaveCustomListsIfChanged();

            await EclipseSettingsDataProvider.Instance.SaveEclipseSettingsAsync(eclipseSettings);

            OnCloseExecute();
        }

        private async void SaveCustomListsIfChanged()
        {
            if (listOrderChanged == true)
            {
                listOrderChanged = false;
                await customListDefinitionDataProvider.SaveCustomListDefinitionsAsync(CustomListDefinitions.ToList());
            }
        }

        private void OnMoveDownCustomListExecute()
        {
            if (SelectedCustomListDefinition == null)
            {
                return;
            }

            CustomListDefinition currentCustomListDefinition = SelectedCustomListDefinition;

            int selectedIndex = CustomListDefinitions.IndexOf(currentCustomListDefinition);
            if (selectedIndex + 1 < CustomListDefinitions.Count)
            {
                CustomListDefinitions.RemoveAt(selectedIndex);
                CustomListDefinitions.Insert(selectedIndex + 1, currentCustomListDefinition);
            }

            SelectedCustomListDefinition = currentCustomListDefinition;

            listOrderChanged = true;
        }

        private bool? listOrderChanged;

        private void OnMoveUpCustomListExecute()
        {
            if (SelectedCustomListDefinition == null)
            {
                return;
            }

            CustomListDefinition currentCustomListDefinition = SelectedCustomListDefinition;

            int selectedIndex = CustomListDefinitions.IndexOf(currentCustomListDefinition);
            if (selectedIndex - 1 >= 0)
            {
                CustomListDefinitions.RemoveAt(selectedIndex);
                CustomListDefinitions.Insert(selectedIndex - 1, currentCustomListDefinition);
            }

            SelectedCustomListDefinition = currentCustomListDefinition;

            listOrderChanged = true;
        }

        private void InitializeListTypes()
        {
            string[] listTypes = Enum.GetNames(typeof(ListCategoryType));

            IEnumerable<object> listOfTypes = from listType in listTypes
                                              where listType != ListCategoryType.VoiceSearch.ToString()
                                              && listType != ListCategoryType.RandomGame.ToString()
                                              && listType != ListCategoryType.MoreLikeThis.ToString()
                                              select Enum.Parse(typeof(ListCategoryType), listType);

            DefaultListTypes = listOfTypes;
        }

        private void InitializeTabPages()
        {
            SelectedTabPage = EclipseSettingsTabs.Lists;

            TabPages.Add(EclipseSettingsTabs.Lists);
            TabPages.Add(EclipseSettingsTabs.Inputs);
            TabPages.Add(EclipseSettingsTabs.Versions);
            TabPages.Add(EclipseSettingsTabs.Other);
            TabPages.Add(EclipseSettingsTabs.CustomLists);
            TabPages.Add(EclipseSettingsTabs.BoxMargin);
        }

        private string selectedTabPage;
        public string SelectedTabPage
        {
            get { return selectedTabPage; }
            set
            {
                selectedTabPage = value;
                OnPropertyChanged("SelectedTabPage");

                UpdateTabVisibility();
            }
        }

        private Models.EclipseSettings eclipseSettings;

        public ListCategoryType DefaultListCategoryType
        {
            get { return eclipseSettings.DefaultListCategoryType; }
            set
            {
                eclipseSettings.DefaultListCategoryType = value;
                OnPropertyChanged("DefaultListCategoryType");
            }
        }

        public bool EnableScreenSaver
        {
            get => eclipseSettings.EnableScreenSaver;
            set
            {
                eclipseSettings.EnableScreenSaver = value;
                OnPropertyChanged("EnableScreenSaver");
            }
        }

        public bool OpenSettingsPaneOnLeft
        {
            get => eclipseSettings.OpenSettingsPaneOnLeft;
            set
            {
                eclipseSettings.OpenSettingsPaneOnLeft = value;
                OnPropertyChanged("OpenSettingsPaneOnLeft");
            }
        }

        public bool IncludeHiddenGames
        {
            get => eclipseSettings.IncludeHiddenGames;
            set
            {
                eclipseSettings.IncludeHiddenGames = value;
                OnPropertyChanged("IncludeHiddenGames");
            }
        }

        public bool IncludeBrokenGames
        {
            get => eclipseSettings.IncludeBrokenGames;
            set
            {
                eclipseSettings.IncludeBrokenGames = value;
                OnPropertyChanged("IncludeBrokenGames");
            }
        }

        public bool DisableVideos
        {
            get => eclipseSettings.DisableVideos;
            set
            {
                eclipseSettings.DisableVideos = value;
                OnPropertyChanged("DisableVideos");
            }
        }

        public double DefaultVideoVolume
        {
            get => eclipseSettings.DefaultVideoVolume;
            set
            {
                eclipseSettings.DefaultVideoVolume = value;
                OnPropertyChanged("DefaultVideoVolume");
            }
        }

        public bool ShowGameCountInList
        {
            get => eclipseSettings.ShowGameCountInList;
            set
            {
                eclipseSettings.ShowGameCountInList = value;
                OnPropertyChanged("ShowGameCountInList");
            }
        }

        public bool AdditionalVersionsEnable
        {
            get => eclipseSettings.AdditionalVersionsEnable;
            set
            {
                eclipseSettings.AdditionalVersionsEnable = value;
                OnPropertyChanged("AdditionalVersionsEnable");
            }
        }

        public bool AdditionalVersionsExcludeRunBefore
        {
            get => eclipseSettings.AdditionalVersionsExcludeRunBefore;
            set
            {
                eclipseSettings.AdditionalVersionsExcludeRunBefore = value;
                OnPropertyChanged("AdditionalVersionsExcludeRunBefore");
            }
        }

        public bool AdditionalVersionsExcludeRunAfter
        {
            get => eclipseSettings.AdditionalVersionsExcludeRunAfter;
            set
            {
                eclipseSettings.AdditionalVersionsExcludeRunAfter = value;
                OnPropertyChanged("AdditionalVersionsExcludeRunAfter");
            }
        }

        public bool AdditionalVersionsOnlyEmulatorOrDosBox
        {
            get => eclipseSettings.AdditionalVersionsOnlyEmulatorOrDosBox;
            set
            {
                eclipseSettings.AdditionalVersionsOnlyEmulatorOrDosBox = value;
                OnPropertyChanged("AdditionalVersionsOnlyEmulatorOrDosBox");
            }
        }

        public AdditionalApplicationDisplayField AdditionalApplicationDisplayField
        {
            get => eclipseSettings.AdditionalApplicationDisplayField;
            set
            {
                eclipseSettings.AdditionalApplicationDisplayField = value;
                OnPropertyChanged("AdditionalApplicationDisplayField");
            }
        }

        public bool AdditionalVersionsRemovePlayPrefix
        {
            get => eclipseSettings.AdditionalVersionsRemovePlayPrefix;
            set
            {
                eclipseSettings.AdditionalVersionsRemovePlayPrefix = value;
                OnPropertyChanged("AdditionalVersionsRemovePlayPrefix");
            }
        }

        public bool AdditionalVersionsRemoveVersionPostfix
        {
            get => eclipseSettings.AdditionalVersionsRemoveVersionPostfix;
            set
            {
                eclipseSettings.AdditionalVersionsRemoveVersionPostfix = value;
                OnPropertyChanged("AdditionalVersionsRemoveVersionPostfix");
            }
        }

        public PageFunction PageUpFunction
        {
            get => eclipseSettings.PageUpFunction;
            set
            {
                eclipseSettings.PageUpFunction = value;
                OnPropertyChanged("PageUpFunction");
            }
        }

        public PageFunction PageDownFunction
        {
            get { return eclipseSettings.PageDownFunction; }
            set
            {
                eclipseSettings.PageDownFunction = value;
                OnPropertyChanged("PageDownFunction");
            }
        }

        public int VideoDelayInMilliseconds
        {
            get { return eclipseSettings.VideoDelayInMilliseconds; }
            set
            {
                eclipseSettings.VideoDelayInMilliseconds = value;
                OnPropertyChanged("VideoDelayInMilliseconds");
            }
        }

        public bool BypassDetails
        {
            get { return eclipseSettings.BypassDetails; }
            set
            {
                eclipseSettings.BypassDetails = value;
                OnPropertyChanged("BypassDetails");
            }
        }

        public bool RepeatGamesToFillScreen
        {
            get => eclipseSettings.RepeatGamesToFillScreen;
            set
            {
                eclipseSettings.RepeatGamesToFillScreen = value;
                OnPropertyChanged("RepeatGamesToFillScreen");
            }
        }

        public bool ShowMatchPercent
        {
            get => eclipseSettings.ShowMatchPercent;
            set
            {
                eclipseSettings.ShowMatchPercent = value;
                OnPropertyChanged("ShowMatchPercent");
            }
        }

        public bool ShowPlatformLogo
        {
            get => eclipseSettings.ShowPlatformLogo;
            set
            {
                eclipseSettings.ShowPlatformLogo = value;
                OnPropertyChanged("ShowPlatformLogo");
            }
        }

        public bool ShowPlayMode
        {
            get => eclipseSettings.ShowPlayMode;
            set
            {
                eclipseSettings.ShowPlayMode = value;
                OnPropertyChanged("ShowPlayMode");
            }
        }

        public bool ShowReleaseYear
        {
            get => eclipseSettings.ShowReleaseYear;
            set
            {
                eclipseSettings.ShowReleaseYear = value;
                OnPropertyChanged("ShowReleaseYear");
            }
        }

        public bool ShowStarRating
        {
            get => eclipseSettings.ShowStarRating;
            set
            {
                eclipseSettings.ShowStarRating = value;
                OnPropertyChanged("ShowStarRating");
            }
        }


        public int ScreensaverDelayInSeconds
        {
            get { return eclipseSettings.ScreensaverDelayInSeconds; }
            set
            {
                eclipseSettings.ScreensaverDelayInSeconds = value;
                OnPropertyChanged("ScreensaverDelayInSeconds");
            }
        }

        public bool EnableVoiceSearch
        {
            get { return eclipseSettings.EnableVoiceSearch; }
            set
            {
                eclipseSettings.EnableVoiceSearch = value;
                OnPropertyChanged("EnableVoiceSearch");
            }
        }

        public double BoxFrontMarginLeft
        {
            get { return eclipseSettings.BoxFrontMarginLeft; }
            set
            {
                eclipseSettings.BoxFrontMarginLeft = value;
                OnPropertyChanged("BoxFrontMarginLeft");
                updateMarginSample();
            }
        }

        public double BoxFrontMarginRight
        {
            get { return eclipseSettings.BoxFrontMarginRight; }
            set
            {
                eclipseSettings.BoxFrontMarginRight = value;
                OnPropertyChanged("BoxFrontMarginRight");
                updateMarginSample();
            }
        }

        public double BoxFrontMarginTop
        {
            get { return eclipseSettings.BoxFrontMarginTop; }
            set
            {
                eclipseSettings.BoxFrontMarginTop = value;
                OnPropertyChanged("BoxFrontMarginTop");
                updateMarginSample();
            }
        }

        public double BoxFrontMarginBottom
        {
            get { return eclipseSettings.BoxFrontMarginBottom; }
            set
            {
                eclipseSettings.BoxFrontMarginBottom = value;
                OnPropertyChanged("BoxFrontMarginBottom");
                updateMarginSample();
            }
        }

        private Thickness marginSample;
        public Thickness MarginSample
        {
            get => marginSample;
            set
            {
                marginSample = value;
                OnPropertyChanged("MarginSample");
            }
        }

        public bool DisplayFeaturedGame 
        {
            get => eclipseSettings.DisplayFeaturedGame;
            set
            {
                eclipseSettings.DisplayFeaturedGame = value;
                OnPropertyChanged("DisplayFeaturedGame");
            }
        }

        public bool ShowOptionsIcon
        {
            get => eclipseSettings.ShowOptionsIcon;
            set
            {
                eclipseSettings.ShowOptionsIcon = value;
                OnPropertyChanged("ShowOptionsIcon");
            }
        }

        public bool DisplayOptionsOnEscape 
        {
            get => eclipseSettings.DisplayOptionsOnEscape;
            set
            {
                eclipseSettings.DisplayOptionsOnEscape = value;
                OnPropertyChanged("DisplayOptionsOnEscape");
            }
        }


        private void updateMarginSample()
        {
            MarginSample = new Thickness(BoxFrontMarginLeft, BoxFrontMarginTop, BoxFrontMarginRight, BoxFrontMarginBottom);
        }

        private IEnumerable<object> defaultListTypes;
        public IEnumerable<object> DefaultListTypes
        {
            get { return defaultListTypes; }
            set
            {
                defaultListTypes = value;
                OnPropertyChanged("DefaultListTypes");
            }
        }

        private void UpdateTabVisibility()
        {
            ListSettingsTabVisibility = Visibility.Collapsed;
            InputTabVisibility = Visibility.Collapsed;
            VersionsTabVisibility = Visibility.Collapsed;
            OtherTabVisibility = Visibility.Collapsed;
            CustomListsTabVisibility = Visibility.Collapsed;
            BoxMarginTabVisibility = Visibility.Collapsed;

            switch (SelectedTabPage)
            {
                case EclipseSettingsTabs.Lists:
                    ListSettingsTabVisibility = Visibility.Visible;
                    break;

                case EclipseSettingsTabs.Inputs:
                    InputTabVisibility = Visibility.Visible;
                    break;

                case EclipseSettingsTabs.Other:
                    OtherTabVisibility = Visibility.Visible;
                    break;

                case EclipseSettingsTabs.Versions:
                    VersionsTabVisibility = Visibility.Visible;
                    break;

                case EclipseSettingsTabs.CustomLists:
                    CustomListsTabVisibility = Visibility.Visible;
                    break;

                case EclipseSettingsTabs.BoxMargin:
                    BoxMarginTabVisibility = Visibility.Visible;
                    break;
            }
        }

        private Visibility inputTabVisibility;
        public Visibility InputTabVisibility
        {
            get => inputTabVisibility;
            set
            {
                inputTabVisibility = value;
                OnPropertyChanged("InputTabVisibility");
            }
        }

        private Visibility versionsTabVisibility;
        public Visibility VersionsTabVisibility
        {
            get => versionsTabVisibility;
            set
            {
                versionsTabVisibility = value;
                OnPropertyChanged("VersionsTabVisibility");
            }
        }

        private Visibility otherTabVisibility;
        public Visibility OtherTabVisibility
        {
            get => otherTabVisibility;
            set
            {
                otherTabVisibility = value;
                OnPropertyChanged("OtherTabVisibility");
            }
        }

        private Visibility listSettingsTabVisibility;
        public Visibility ListSettingsTabVisibility
        {
            get => listSettingsTabVisibility;
            set
            {
                listSettingsTabVisibility = value;
                OnPropertyChanged("ListSettingsTabVisibility");
            }
        }

        private Visibility customListsTabVisibility;
        public Visibility CustomListsTabVisibility
        {
            get => customListsTabVisibility;
            set
            {
                customListsTabVisibility = value;
                OnPropertyChanged("CustomListsTabVisibility");
            }
        }


        private Visibility boxMarginTabVisibility;
        public Visibility BoxMarginTabVisibility
        {
            get => boxMarginTabVisibility;
            set
            {
                boxMarginTabVisibility = value;
                OnPropertyChanged("BoxMarginTabVisibility");
            }
        }


        private void OnCustomListDefinitionEditClosed()
        {
            customListDefinitionEditView = null;
            customListDefinitionEditViewModel = null;
        }

        private async void OnCustomListDefinitionSavedSavedAsync(string id)
        {
            await InitializeCustomListsAsync();

            CustomListDefinition customListDefinition = CustomListDefinitions.SingleOrDefault(l => l.Id == id);
            if (customListDefinition != null)
            {
                SelectedCustomListDefinition = customListDefinition;
            }
        }

        private void OnCloseExecute()
        {
            eventAggregator.GetEvent<EclipseSettingsClose>().Publish();
        }

        private async void OnDeleteExecuteAsync()
        {
            SaveCustomListsIfChanged();

            string customListName = string.IsNullOrWhiteSpace(SelectedCustomListDefinition?.Description) ? "list" : SelectedCustomListDefinition.Description;

            MessageDialogResult messageDialogResult = MessageDialogHelper.ShowOKCancelDialog($"Delete {customListName}?", "Delete custom list");
            if (messageDialogResult == MessageDialogResult.OK)
            {
                await customListDefinitionDataProvider.DeleteCustomListDefinition(SelectedCustomListDefinition.Id);

                await InitializeCustomListsAsync();
            }

            SelectedCustomListDefinition = null;
        }

        private bool OnDeleteCanExecute()
        {
            // if the window is closed (null) and a patcher is selected (not null)
            return (customListDefinitionEditView == null) && (SelectedCustomListDefinition != null);
        }

        private void OnAddExecute()
        {
            SaveCustomListsIfChanged();

            if (customListDefinitionEditView != null)
            {
                customListDefinitionEditView.Activate();
            }
            else
            {
                SelectedCustomListDefinition = null;
                customListDefinitionEditViewModel = new CustomListDefinitionEditViewModel(new CustomListDefinition());
                customListDefinitionEditView = new CustomListDefinitionEditView(customListDefinitionEditViewModel);
                customListDefinitionEditView.Show();
            }
        }

        private bool OnAddCanExecute()
        {
            // add is ok if the edit window is closed 
            return customListDefinitionEditView == null;
        }

        private void OnEditExecute()
        {
            SaveCustomListsIfChanged();

            if (customListDefinitionEditView != null)
            {
                customListDefinitionEditView.Activate();
            }
            else
            {
                customListDefinitionEditViewModel = new CustomListDefinitionEditViewModel(SelectedCustomListDefinition);
                customListDefinitionEditView = new CustomListDefinitionEditView(customListDefinitionEditViewModel);
                customListDefinitionEditView.Show();
            }
        }

        private bool OnEditCanExecute()
        {
            // if the window is closed (null) and a patcher is selected (not null)
            return (customListDefinitionEditView == null) && (SelectedCustomListDefinition != null);
        }

        public async Task LoadAsync()
        {
            await InitializeEclipseSettingsAsync();

            await InitializeCustomListsAsync();
        }

        public async Task InitializeCustomListsAsync()
        {
            CustomListDefinitions.Clear();

            IEnumerable<CustomListDefinition> customListDefinitions = await customListDefinitionDataProvider.GetAllCustomListDefinitionsAsync();

            foreach (CustomListDefinition customListDefinition in customListDefinitions)
            {
                CustomListDefinitions.Add(customListDefinition);
            }

            InvalidateCommands();
        }

        public async Task InitializeEclipseSettingsAsync()
        {
            eclipseSettings = await EclipseSettingsDataProvider.Instance.GetEclipseSettingsAsync();

            DefaultListCategoryType = eclipseSettings.DefaultListCategoryType;

            DisableVideos = eclipseSettings.DisableVideos;
            DefaultVideoVolume = eclipseSettings.DefaultVideoVolume;

            EnableVoiceSearch = eclipseSettings.EnableVoiceSearch;
            EnableScreenSaver = eclipseSettings.EnableScreenSaver;
            ShowGameCountInList = eclipseSettings.ShowGameCountInList;
            IncludeBrokenGames = eclipseSettings.IncludeBrokenGames;
            IncludeHiddenGames = eclipseSettings.IncludeHiddenGames;
            OpenSettingsPaneOnLeft = eclipseSettings.OpenSettingsPaneOnLeft;

            AdditionalVersionsEnable = eclipseSettings.AdditionalVersionsEnable;
            AdditionalVersionsExcludeRunBefore = eclipseSettings.AdditionalVersionsExcludeRunBefore;
            AdditionalVersionsExcludeRunAfter = eclipseSettings.AdditionalVersionsExcludeRunAfter;
            AdditionalVersionsOnlyEmulatorOrDosBox = eclipseSettings.AdditionalVersionsOnlyEmulatorOrDosBox;
            AdditionalApplicationDisplayField = eclipseSettings.AdditionalApplicationDisplayField;
            AdditionalVersionsRemovePlayPrefix = eclipseSettings.AdditionalVersionsRemovePlayPrefix;
            AdditionalVersionsRemoveVersionPostfix = eclipseSettings.AdditionalVersionsRemoveVersionPostfix;

            PageUpFunction = eclipseSettings.PageUpFunction;
            PageDownFunction = eclipseSettings.PageDownFunction;

            ScreensaverDelayInSeconds = eclipseSettings.ScreensaverDelayInSeconds;
            VideoDelayInMilliseconds = eclipseSettings.VideoDelayInMilliseconds;
            BypassDetails = eclipseSettings.BypassDetails;
            RepeatGamesToFillScreen = eclipseSettings.RepeatGamesToFillScreen;
            ShowMatchPercent = eclipseSettings.ShowMatchPercent;
            ShowReleaseYear = eclipseSettings.ShowReleaseYear;
            ShowStarRating = eclipseSettings.ShowStarRating;
            ShowPlayMode = eclipseSettings.ShowPlayMode;
            ShowPlatformLogo = eclipseSettings.ShowPlatformLogo;
            ShowOptionsIcon = eclipseSettings.ShowOptionsIcon;

            BoxFrontMarginLeft = eclipseSettings.BoxFrontMarginLeft;
            BoxFrontMarginRight = eclipseSettings.BoxFrontMarginRight;
            BoxFrontMarginTop = eclipseSettings.BoxFrontMarginTop;
            BoxFrontMarginBottom = eclipseSettings.BoxFrontMarginBottom;

            DisplayFeaturedGame = eclipseSettings.DisplayFeaturedGame;
            DisplayOptionsOnEscape = eclipseSettings.DisplayOptionsOnEscape;
        }

        private void InvalidateCommands()
        {
            ((DelegateCommand)EditCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)DeleteCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)AddCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)CloseCommand).RaiseCanExecuteChanged();
        }

        public Uri IconUri { get; } = ResourceImages.EclipseSettingsIcon1;

        public ObservableCollection<string> TabPages { get; }
    }

    public static class EclipseSettingsTabs
    {
        public const string Lists = "Lists";
        public const string Inputs = "Inputs";
        public const string Versions = "Versions";
        public const string CustomLists = "Custom lists";
        public const string Other = "Other";
        public const string BoxMargin = "Margin";
    }
}