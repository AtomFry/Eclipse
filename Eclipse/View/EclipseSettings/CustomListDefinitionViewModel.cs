using Eclipse.Event;
using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using Eclipse.View;
using static Eclipse.Helpers.MessageDialogHelper;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Eclipse.View.EclipseSettings
{
    public class CustomListDefinitionViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly CustomListDefinitionDataProvider customListDefinitionDataProvider;
        private readonly IEventAggregator eventAggregator;

        public ObservableCollection<CustomListDefinition> CustomListDefinitions { get; }
        public ICommand EditCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand MoveUpCustomListCommand { get; }
        public ICommand MoveDownCustomListCommand { get; }

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

        private bool listOrderChanged;
        private CustomListDefinitionEditViewModel customListDefinitionEditViewModel;
        private CustomListDefinitionEditView customListDefinitionEditView;

        public CustomListDefinitionViewModel()
        {
            CustomListDefinitions = new ObservableCollection<CustomListDefinition>();
            
            eventAggregator = EventAggregatorHelper.Instance.EventAggregator;
            customListDefinitionDataProvider = new CustomListDefinitionDataProvider();

            EditCommand = new DelegateCommand(OnEditExecute, OnEditCanExecute);
            AddCommand = new DelegateCommand(OnAddExecute, OnAddCanExecute);
            DeleteCommand = new DelegateCommand(OnDeleteExecuteAsync, OnDeleteCanExecute);
            MoveUpCustomListCommand = new DelegateCommand(OnMoveUpCustomListExecute);
            MoveDownCustomListCommand = new DelegateCommand(OnMoveDownCustomListExecute);

            eventAggregator.GetEvent<CustomListDefinitionSaved>().Subscribe(OnCustomListDefinitionSavedSavedAsync);
            eventAggregator.GetEvent<CustomListDefinitionEditClosing>().Subscribe(OnCustomListDefinitionEditClosed);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task InitializeCustomListsAsync()
        {
            var customListDefinitions = await customListDefinitionDataProvider.GetAllCustomListDefinitionsAsync();
            CustomListDefinitions.Clear();
            foreach (var customListDefinition in customListDefinitions)
            {
                CustomListDefinitions.Add(customListDefinition);
            }
        }

        public async void SaveCustomListsIfChanged()
        {
            if (listOrderChanged == true)
            {
                listOrderChanged = false;
                await customListDefinitionDataProvider.SaveCustomListDefinitionsAsync(CustomListDefinitions.ToList());
            }
        }

        public void InvalidateCommands()
        {
            ((DelegateCommand)EditCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)AddCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)DeleteCommand).RaiseCanExecuteChanged();
        }

        private void OnMoveDownCustomListExecute()
        {
            if (SelectedCustomListDefinition == null)
            {
                return;
            }

            var index = CustomListDefinitions.IndexOf(SelectedCustomListDefinition);
            if (index < CustomListDefinitions.Count - 1)
            {
                CustomListDefinitions.Move(index, index + 1);
                listOrderChanged = true;
            }
        }

        private void OnMoveUpCustomListExecute()
        {
            if (SelectedCustomListDefinition == null)
            {
                return;
            }

            var index = CustomListDefinitions.IndexOf(SelectedCustomListDefinition);
            if (index > 0)
            {
                CustomListDefinitions.Move(index, index - 1);
                listOrderChanged = true;
            }
        }

        private async void OnCustomListDefinitionSavedSavedAsync(string customListDefinitionName)
        {
            await InitializeCustomListsAsync();
        }

        private void OnCustomListDefinitionEditClosed()
        {
            customListDefinitionEditView.Close();
        }

        private bool OnDeleteCanExecute()
        {
            return SelectedCustomListDefinition != null;
        }

        private async void OnDeleteExecuteAsync()
        {
            if (SelectedCustomListDefinition == null)
            {
                return;
            }

            var customListDefinition = SelectedCustomListDefinition;
            var messageBoxResult = MessageDialogHelper.ShowOKCancelDialog(
                "Are you sure you want to delete the custom list definition \"" + customListDefinition.Description + "\"?",
                "Confirm Delete");

            if (messageBoxResult == MessageDialogResult.OK)
            {
                await customListDefinitionDataProvider.DeleteCustomListDefinition(customListDefinition.Id);
                CustomListDefinitions.Remove(customListDefinition);
                SelectedCustomListDefinition = null;
            }
        }

        private bool OnAddCanExecute()
        {
            return true;
        }

        private void OnAddExecute()
        {
            customListDefinitionEditViewModel = new CustomListDefinitionEditViewModel(new CustomListDefinition());
            customListDefinitionEditView = new CustomListDefinitionEditView(customListDefinitionEditViewModel);
            customListDefinitionEditView.ShowDialog();
        }

        private bool OnEditCanExecute()
        {
            return SelectedCustomListDefinition != null;
        }

        private void OnEditExecute()
        {
            customListDefinitionEditViewModel = new CustomListDefinitionEditViewModel(SelectedCustomListDefinition);
            customListDefinitionEditView = new CustomListDefinitionEditView(customListDefinitionEditViewModel);
            customListDefinitionEditView.ShowDialog();
        }
    }
}