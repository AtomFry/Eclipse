using Eclipse.Event;
using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private void InitializeTabPages()
        {
            SelectedTabPage = "Settings";

            TabPages.Add("Settings");
            TabPages.Add("Custom lists");
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

        public bool ShowGameCountInList
        {
            get { return eclipseSettings.ShowGameCountInList; }
            set
            {
                eclipseSettings.ShowGameCountInList = value;
                OnPropertyChanged("ShowGameCountInList");
            }
        }

        public PageFunction PageUpFunction
        {
            get { return eclipseSettings.PageUpFunction; }
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

        public int ScreensaverDelayInSeconds
        {
            get { return eclipseSettings.ScreensaverDelayInSeconds; }
            set
            {
                eclipseSettings.ScreensaverDelayInSeconds = value;
                OnPropertyChanged("ScreensaverDelayInSeconds");
            }
        }

        private void UpdateTabVisibility()
        {
            SettingsVisibility = Visibility.Collapsed;
            CustomListsVisibility = Visibility.Collapsed;

            switch (SelectedTabPage)
            {
                case "Settings":
                    SettingsVisibility = Visibility.Visible;
                    break;

                case "Custom lists":
                    CustomListsVisibility = Visibility.Visible;
                    break;
            }
        }

        private Visibility settingsVisibility;
        public Visibility SettingsVisibility
        {
            get { return settingsVisibility; }
            set
            {
                settingsVisibility = value;
                OnPropertyChanged("SettingsVisibility");
            }
        }

        private Visibility customListsVisibility;
        public Visibility CustomListsVisibility
        {
            get { return customListsVisibility; }
            set
            {
                customListsVisibility = value;
                OnPropertyChanged("CustomListsVisibility");
            }
        }

        private void OnCustomListDefinitionEditClosed()
        {
            customListDefinitionEditView = null;
            customListDefinitionEditViewModel = null;
        }

        private async void OnCustomListDefinitionSavedSavedAsync(string id)
        {
            await LoadAsync();

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

            MessageDialogResult messageDialogResult = MessageDialogHelper.ShowOKCancelDialog($"Delete {customListName}?", "Delete patcher");
            if (messageDialogResult == MessageDialogResult.OK)
            {
                await customListDefinitionDataProvider.DeleteCustomListDefinition(SelectedCustomListDefinition.Id);
                await LoadAsync();
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
            ShowGameCountInList = eclipseSettings.ShowGameCountInList;
            PageUpFunction = eclipseSettings.PageUpFunction;
            PageDownFunction = eclipseSettings.PageDownFunction;
            ScreensaverDelayInSeconds = eclipseSettings.ScreensaverDelayInSeconds;
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
}