using Eclipse.Event;
using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using Newtonsoft.Json;
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

            customListDefinitionDataProvider = new CustomListDefinitionDataProvider();

            EditCommand = new RelayCommand(OnEditExecute, OnEditCanExecute);
            AddCommand = new RelayCommand(OnAddExecute, OnAddCanExecute);
            DeleteCommand = new RelayCommand(OnDeleteExecuteAsync, OnDeleteCanExecute);
            CloseCommand = new RelayCommand(OnCloseExecute);
            MoveUpCustomListCommand = new RelayCommand(OnMoveUpCustomListExecute);
            MoveDownCustomListCommand = new RelayCommand(OnMoveDownCustomListExecute);
            SaveCommand = new RelayCommand(OnSaveExecute);
            CancelCommand = new RelayCommand(OnCancelExecute);

            SettingsEvents.CustomListDefinitionSaved += OnCustomListDefinitionSavedSavedAsync;
            SettingsEvents.CustomListDefinitionEditClosing += OnCustomListDefinitionEditClosed;
        }

        /// <summary>
        /// Lets go of the two subscriptions taken in the constructor. Called by the window on
        /// Closed - this object outlives nothing, but the aggregator it subscribed to lives for
        /// the whole process, and the window is opened and closed repeatedly.
        /// </summary>
        public void Detach()
        {
            SettingsEvents.CustomListDefinitionSaved -= OnCustomListDefinitionSavedSavedAsync;
            SettingsEvents.CustomListDefinitionEditClosing -= OnCustomListDefinitionEditClosed;
        }

        private void OnCancelExecute()
        {
            SettingsEvents.RaiseEclipseSettingsClose();
        }

        // A command handler has to be void, so this is the one place the exception has to be
        // caught rather than propagated. It used to be async void with no handler at all, which
        // meant a save that failed - a read-only file, a full disk - closed the window and told
        // the user nothing.
        private async void OnSaveExecute()
        {
            try
            {
                // Awaited. This used to be a bare call to an async void method, so the window
                // could close with the custom-list write still in flight.
                await SaveCustomListsIfChangedAsync();

                await EclipseSettingsDataProvider.Instance.SaveEclipseSettingsAsync(eclipseSettings);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "save Eclipse settings");

                // Leave the window open. The user's edits are still in it, so they can retry or
                // fix whatever is wrong with the file; closing would throw the edits away.
                MessageDialogHelper.ShowOKDialog(
                    $"Your settings could not be saved.\n\n{ex.Message}\n\nThe window has been left open so you can try again.",
                    "Save failed");
                return;
            }

            OnCloseExecute();
        }

        private async Task SaveCustomListsIfChangedAsync()
        {
            if (listOrderChanged == true)
            {
                listOrderChanged = false;
                await customListDefinitionDataProvider.SaveCustomListDefinitionsAsync(CustomListDefinitions.ToList());
            }
        }

        /// <summary>
        /// The same write, for callers whose own failure reporting would be misleading - opening
        /// the add or edit window is not a save, and a modal there would be noise. Logged only.
        /// </summary>
        private async Task TrySaveCustomListsIfChangedAsync()
        {
            try
            {
                await SaveCustomListsIfChangedAsync();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "save the custom list order");
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
            TabPages.Add(EclipseSettingsTabs.ScreenSaver);
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

        /// <summary>
        /// The settings being edited, bound to directly by the XAML.
        ///
        /// There used to be forty-five delegating properties here - a getter that read this
        /// object and a setter that wrote it - so every setting was declared twice, and adding
        /// one meant remembering to add both. The four box-front margins below are the only ones
        /// that survive, because they are the only ones whose setter does something beyond
        /// notifying: they refresh the margin preview.
        ///
        /// This is a fresh instance read from disk, not the provider's cached copy, so Cancel
        /// still discards edits by simply not saving them.
        /// </summary>
        public Models.EclipseSettings Settings => eclipseSettings;

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
            ScreenSaverTabVisibility = Visibility.Collapsed;

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

                case EclipseSettingsTabs.ScreenSaver:
                    ScreenSaverTabVisibility = Visibility.Visible;
                    break;
            }
        }

        private Visibility screenSaverTabVisibility;
        public Visibility ScreenSaverTabVisibility
        {
            get => screenSaverTabVisibility;
            set
            {
                screenSaverTabVisibility = value;
                OnPropertyChanged("ScreenSaverTabVisibility");
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

        // An event handler, so void is forced. Reloading the list after a child window saved is
        // not something the user asked for directly, so a failure is logged rather than shown -
        // but it must not escape onto the dispatcher.
        private async void OnCustomListDefinitionSavedSavedAsync(string id)
        {
            try
            {
                await InitializeCustomListsAsync();

                CustomListDefinition customListDefinition = CustomListDefinitions.SingleOrDefault(l => l.Id == id);
                if (customListDefinition != null)
                {
                    SelectedCustomListDefinition = customListDefinition;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, "reload custom lists after a save");
            }
        }

        private void OnCloseExecute()
        {
            SettingsEvents.RaiseEclipseSettingsClose();
        }

        private async void OnDeleteExecuteAsync()
        {
            string customListName = string.IsNullOrWhiteSpace(SelectedCustomListDefinition?.Description) ? "list" : SelectedCustomListDefinition.Description;

            try
            {
                // Unchanged in order: a pending reorder is written whether or not the delete goes
                // ahead. What has changed is that it is awaited, so the write completes before
                // the dialog rather than racing it.
                await SaveCustomListsIfChangedAsync();

                MessageDialogResult messageDialogResult = MessageDialogHelper.ShowOKCancelDialog($"Delete {customListName}?", "Delete custom list");
                if (messageDialogResult == MessageDialogResult.OK)
                {
                    await customListDefinitionDataProvider.DeleteCustomListDefinition(SelectedCustomListDefinition.Id);

                    await InitializeCustomListsAsync();
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex, $"delete custom list {customListName}");

                MessageDialogHelper.ShowOKDialog(
                    $"“{customListName}” could not be deleted.\n\n{ex.Message}",
                    "Delete failed");
            }

            SelectedCustomListDefinition = null;
        }

        private bool OnDeleteCanExecute()
        {
            // if the window is closed (null) and a patcher is selected (not null)
            return (customListDefinitionEditView == null) && (SelectedCustomListDefinition != null);
        }

        // async void because a command handler has to be. The pending reorder is awaited rather
        // than fired and forgotten, so the write finishes before the child window opens on top
        // of it. A failure here is logged rather than shown - the user asked to open a window,
        // not to save, and a modal in that flow would be noise.
        private async void OnAddExecute()
        {
            await TrySaveCustomListsIfChangedAsync();

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

        private async void OnEditExecute()
        {
            await TrySaveCustomListsIfChangedAsync();

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

            // One notification for the whole object, instead of forty-four lines that read each
            // value off the model and wrote it straight back through a property whose getter
            // read the same model. Their only effect was this - plus the margin preview, which
            // is refreshed explicitly below.
            OnPropertyChanged(nameof(Settings));

            updateMarginSample();
        }

        private void InvalidateCommands()
        {
            ((RelayCommand)EditCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
            ((RelayCommand)AddCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CloseCommand).RaiseCanExecuteChanged();
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
        public const string ScreenSaver = "Screen saver";
    }
}