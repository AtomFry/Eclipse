using Eclipse.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Eclipse.View.EclipseSettings
{
    public class GameSelectionCriteriaViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Models.EclipseSettings eclipseSettings;

        public ObservableCollection<Option> DefaultListTypes { get; }

        public GameSelectionCriteriaViewModel(Models.EclipseSettings eclipseSettings)
        {
            this.eclipseSettings = eclipseSettings ?? throw new System.ArgumentNullException(nameof(eclipseSettings));
            DefaultListTypes = new ObservableCollection<Option>();
            InitializeListTypes();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Core Game Selection Settings
        public ListCategoryType DefaultListCategoryType
        {
            get { return eclipseSettings.DefaultListCategoryType; }
            set 
            { 
                eclipseSettings.DefaultListCategoryType = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeHiddenGames
        {
            get { return eclipseSettings.IncludeHiddenGames; }
            set 
            { 
                eclipseSettings.IncludeHiddenGames = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeBrokenGames
        {
            get { return eclipseSettings.IncludeBrokenGames; }
            set 
            { 
                eclipseSettings.IncludeBrokenGames = value;
                OnPropertyChanged();
            }
        }

        // List Display Options
        public bool ShowGameCountInList
        {
            get { return eclipseSettings.ShowGameCountInList; }
            set 
            { 
                eclipseSettings.ShowGameCountInList = value;
                OnPropertyChanged();
            }
        }

        public bool RepeatGamesToFillScreen
        {
            get { return eclipseSettings.RepeatGamesToFillScreen; }
            set 
            { 
                eclipseSettings.RepeatGamesToFillScreen = value;
                OnPropertyChanged();
            }
        }

        // Additional Versions Settings (using the actual properties from EclipseSettings)
        public bool AdditionalVersionsEnable
        {
            get { return eclipseSettings.AdditionalVersionsEnable; }
            set 
            { 
                eclipseSettings.AdditionalVersionsEnable = value;
                OnPropertyChanged();
            }
        }

        public bool AdditionalVersionsExcludeRunBefore
        {
            get { return eclipseSettings.AdditionalVersionsExcludeRunBefore; }
            set 
            { 
                eclipseSettings.AdditionalVersionsExcludeRunBefore = value;
                OnPropertyChanged();
            }
        }

        public bool AdditionalVersionsExcludeRunAfter
        {
            get { return eclipseSettings.AdditionalVersionsExcludeRunAfter; }
            set 
            { 
                eclipseSettings.AdditionalVersionsExcludeRunAfter = value;
                OnPropertyChanged();
            }
        }

        public bool AdditionalVersionsOnlyEmulatorOrDosBox
        {
            get { return eclipseSettings.AdditionalVersionsOnlyEmulatorOrDosBox; }
            set 
            { 
                eclipseSettings.AdditionalVersionsOnlyEmulatorOrDosBox = value;
                OnPropertyChanged();
            }
        }

        public AdditionalApplicationDisplayField AdditionalApplicationDisplayField
        {
            get { return eclipseSettings.AdditionalApplicationDisplayField; }
            set 
            { 
                eclipseSettings.AdditionalApplicationDisplayField = value;
                OnPropertyChanged();
            }
        }

        public bool AdditionalVersionsRemovePlayPrefix
        {
            get { return eclipseSettings.AdditionalVersionsRemovePlayPrefix; }
            set 
            { 
                eclipseSettings.AdditionalVersionsRemovePlayPrefix = value;
                OnPropertyChanged();
            }
        }

        public bool AdditionalVersionsRemoveVersionPostfix
        {
            get { return eclipseSettings.AdditionalVersionsRemoveVersionPostfix; }
            set 
            { 
                eclipseSettings.AdditionalVersionsRemoveVersionPostfix = value;
                OnPropertyChanged();
            }
        }

        private void InitializeListTypes()
        {
            DefaultListTypes.Add(new Option { Name = "Platform", ListCategoryType = ListCategoryType.Platform });
            DefaultListTypes.Add(new Option { Name = "Genre", ListCategoryType = ListCategoryType.Genre });
            DefaultListTypes.Add(new Option { Name = "Release Year", ListCategoryType = ListCategoryType.ReleaseYear });
            DefaultListTypes.Add(new Option { Name = "Publisher", ListCategoryType = ListCategoryType.Publisher });
            DefaultListTypes.Add(new Option { Name = "Developer", ListCategoryType = ListCategoryType.Developer });
            DefaultListTypes.Add(new Option { Name = "Series", ListCategoryType = ListCategoryType.Series });
            DefaultListTypes.Add(new Option { Name = "Play Mode", ListCategoryType = ListCategoryType.PlayMode });
            DefaultListTypes.Add(new Option { Name = "Playlist", ListCategoryType = ListCategoryType.Playlist });
        }
    }
}