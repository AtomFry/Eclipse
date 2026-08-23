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
        public ObservableCollection<CustomListDefinition> CustomListDefinitions { get; }

        // Whether the list above differs from what is on disk - set by an edit, an add, a delete
        // or a reorder, and cleared when the window saves. All four used to commit at different
        // moments; this is what makes them agree.
        private bool customListsChanged;
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

            EditCommand = new RelayCommand(OnEditExecute, OnEditCanExecute);
            AddCommand = new RelayCommand(OnAddExecute, OnAddCanExecute);
            DeleteCommand = new RelayCommand(OnDeleteExecute, OnDeleteCanExecute);
            CloseCommand = new RelayCommand(OnCloseExecute);
            MoveUpCustomListCommand = new RelayCommand(OnMoveUpCustomListExecute);
            MoveDownCustomListCommand = new RelayCommand(OnMoveDownCustomListExecute);
            SaveCommand = new RelayCommand(OnSaveExecute);
            CancelCommand = new RelayCommand(OnCancelExecute);

            SettingsEvents.CustomListDefinitionSaved += OnCustomListDefinitionSaved;
            SettingsEvents.CustomListDefinitionEditClosing += OnCustomListDefinitionEditClosed;
        }

        /// <summary>
        /// Lets go of the two subscriptions taken in the constructor. Called by the window on
        /// Closed - SettingsEvents are static and hold their handlers strongly, so this is what
        /// lets a closed window be collected.
        /// </summary>
        public void Detach()
        {
            SettingsEvents.CustomListDefinitionSaved -= OnCustomListDefinitionSaved;
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

                await EclipseSettingsDataService.Instance.SaveEclipseSettingsAsync(eclipseSettings);
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
            if (customListsChanged)
            {
                customListsChanged = false;
                await CustomListDefinitionDataService.Instance.SaveCustomListDefinitionsAsync(CustomListDefinitions.ToList());
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

            customListsChanged = true;
        }

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

            customListsChanged = true;
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

                // The tab panels are DataTemplates now, chosen by a trigger on this value, so
                // this notification is the whole of switching tabs. It used to be followed by a
                // call that set seven Visibility properties to Collapsed and then switched on
                // this string to set one of them back.
                OnPropertyChanged("SelectedTabPage");
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


        private void OnCustomListDefinitionEditClosed()
        {
            customListDefinitionEditView = null;
            customListDefinitionEditViewModel = null;
        }

        /// <summary>
        /// The editor accepted an edit. Take its copy into the list, in place if it is one we
        /// already have and at the end if it is new.
        ///
        /// This used to reload the whole list from disk, because the editor had already written
        /// there. Nothing has been written yet - the user has to save this window - so the edit
        /// lives here until they do, and Cancel discards it by simply not writing.
        /// </summary>
        private void OnCustomListDefinitionSaved(CustomListDefinition edited)
        {
            if (string.IsNullOrWhiteSpace(edited.Id))
            {
                // new list - the id is assigned here rather than on the way to disk, because
                // there is no longer a trip to disk to assign it on
                edited.Id = Guid.NewGuid().ToString();
                CustomListDefinitions.Add(edited);
            }
            else
            {
                int index = IndexOfCustomList(edited.Id);
                if (index < 0)
                {
                    CustomListDefinitions.Add(edited);
                }
                else
                {
                    CustomListDefinitions[index] = edited;
                }
            }

            customListsChanged = true;
            SelectedCustomListDefinition = edited;
        }

        private int IndexOfCustomList(string id)
        {
            for (int index = 0; index < CustomListDefinitions.Count; index++)
            {
                if (CustomListDefinitions[index].Id == id)
                {
                    return index;
                }
            }

            return -1;
        }

        private void OnCloseExecute()
        {
            SettingsEvents.RaiseEclipseSettingsClose();
        }

        /// <summary>
        /// Removes the selected list from the window's own collection. Nothing is written until
        /// the user saves.
        ///
        /// Delete used to go straight to disk, independent of Save and Cancel - which is what
        /// `OQ-017` recorded, and which meant the window's Cancel button undid a reorder but not
        /// a deletion. All three - edit, reorder, delete - now commit at the same moment.
        /// </summary>
        private void OnDeleteExecute()
        {
            CustomListDefinition selected = SelectedCustomListDefinition;
            if (selected == null)
            {
                return;
            }

            string customListName = string.IsNullOrWhiteSpace(selected.Description) ? "list" : selected.Description;

            MessageDialogResult messageDialogResult = MessageDialogHelper.ShowOKCancelDialog($"Delete {customListName}?", "Delete custom list");
            if (messageDialogResult != MessageDialogResult.OK)
            {
                return;
            }

            CustomListDefinitions.Remove(selected);
            customListsChanged = true;

            SelectedCustomListDefinition = null;
        }

        private bool OnDeleteCanExecute()
        {
            // enabled only when the editor is closed and a list is selected
            return (customListDefinitionEditView == null) && (SelectedCustomListDefinition != null);
        }

        private void OnAddExecute()
        {

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
            // enabled only when the editor is closed and a list is selected
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

            IEnumerable<CustomListDefinition> customListDefinitions = await CustomListDefinitionDataService.Instance.GetAllCustomListDefinitionsAsync();

            foreach (CustomListDefinition customListDefinition in customListDefinitions)
            {
                CustomListDefinitions.Add(customListDefinition);
            }

            InvalidateCommands();
        }

        public async Task InitializeEclipseSettingsAsync()
        {
            eclipseSettings = await EclipseSettingsDataService.Instance.GetEclipseSettingsAsync();

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