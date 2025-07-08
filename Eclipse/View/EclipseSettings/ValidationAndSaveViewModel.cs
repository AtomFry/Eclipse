using Eclipse.Event;
using Eclipse.Models;
using Eclipse.Service;
using Prism.Commands;
using Prism.Events;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Eclipse.View.EclipseSettings
{
    public class ValidationAndSaveViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly IEventAggregator eventAggregator;
        private readonly CustomListDefinitionViewModel customListDefinition;
        private Models.EclipseSettings eclipseSettings;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CloseCommand { get; }

        public ValidationAndSaveViewModel(IEventAggregator eventAggregator, CustomListDefinitionViewModel customListDefinition)
        {
            this.eventAggregator = eventAggregator ?? throw new System.ArgumentNullException(nameof(eventAggregator));
            this.customListDefinition = customListDefinition ?? throw new System.ArgumentNullException(nameof(customListDefinition));

            SaveCommand = new DelegateCommand(OnSaveExecute);
            CancelCommand = new DelegateCommand(OnCancelExecute);
            CloseCommand = new DelegateCommand(OnCloseExecute);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task LoadAsync()
        {
            await InitializeEclipseSettingsAsync();
            await customListDefinition.InitializeCustomListsAsync();
        }

        private async Task InitializeEclipseSettingsAsync()
        {
            eclipseSettings = await EclipseSettingsDataProvider.Instance.GetEclipseSettingsAsync();
        }

        public Models.EclipseSettings GetEclipseSettings()
        {
            return eclipseSettings;
        }

        private async void OnSaveExecute()
        {
            customListDefinition.SaveCustomListsIfChanged();
            await EclipseSettingsDataProvider.Instance.SaveEclipseSettingsAsync(eclipseSettings);
            OnCloseExecute();
        }

        private void OnCancelExecute()
        {
            eventAggregator.GetEvent<EclipseSettingsClose>().Publish();
        }

        private void OnCloseExecute()
        {
            eventAggregator.GetEvent<EclipseSettingsClose>().Publish();
        }
    }
}