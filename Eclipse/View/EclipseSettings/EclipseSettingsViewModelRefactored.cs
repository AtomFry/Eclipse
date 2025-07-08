using Eclipse.Event;
using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using Eclipse.View;
using Prism.Events;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Eclipse.View.EclipseSettings
{
    public class EclipseSettingsViewModelRefactored : ViewModelBase
    {
        private readonly IEventAggregator eventAggregator;
        private Models.EclipseSettings eclipseSettings;

        public CustomListDefinitionViewModel CustomListDefinition { get; private set; }
        public UIPreferencesViewModel UIPreferences { get; private set; }
        public TabNavigationViewModel TabNavigation { get; private set; }
        public GameSelectionCriteriaViewModel GameSelectionCriteria { get; private set; }
        public MediaSettingsViewModel MediaSettings { get; private set; }
        public ValidationAndSaveViewModel ValidationAndSave { get; private set; }

        public EclipseSettingsViewModelRefactored()
        {
            eventAggregator = EventAggregatorHelper.Instance.EventAggregator;
            
            TabNavigation = new TabNavigationViewModel();
            CustomListDefinition = new CustomListDefinitionViewModel();
            
            // ValidationAndSave depends on CustomListDefinition, so create it after
            ValidationAndSave = new ValidationAndSaveViewModel(eventAggregator, CustomListDefinition);
        }

        public async Task LoadAsync()
        {
            await ValidationAndSave.LoadAsync();
            eclipseSettings = ValidationAndSave.GetEclipseSettings();
            
            // Now create ViewModels that depend on eclipseSettings
            UIPreferences = new UIPreferencesViewModel(eclipseSettings);
            GameSelectionCriteria = new GameSelectionCriteriaViewModel(eclipseSettings);
            MediaSettings = new MediaSettingsViewModel(eclipseSettings);
        }

        // Expose commands through ValidationAndSave
        public ICommand SaveCommand => ValidationAndSave.SaveCommand;
        public ICommand CancelCommand => ValidationAndSave.CancelCommand;
        public ICommand CloseCommand => ValidationAndSave.CloseCommand;

        // Expose commands through CustomListDefinition
        public ICommand EditCommand => CustomListDefinition.EditCommand;
        public ICommand AddCommand => CustomListDefinition.AddCommand;
        public ICommand DeleteCommand => CustomListDefinition.DeleteCommand;
        public ICommand MoveUpCustomListCommand => CustomListDefinition.MoveUpCustomListCommand;
        public ICommand MoveDownCustomListCommand => CustomListDefinition.MoveDownCustomListCommand;
    }
}