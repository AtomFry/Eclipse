using Eclipse.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Eclipse.View.EclipseSettings
{
    public class UIPreferencesViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Models.EclipseSettings eclipseSettings;

        public UIPreferencesViewModel(Models.EclipseSettings eclipseSettings)
        {
            this.eclipseSettings = eclipseSettings ?? throw new System.ArgumentNullException(nameof(eclipseSettings));
            UpdateMarginSample();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Game Details Display Settings
        public bool ShowMatchPercent
        {
            get { return eclipseSettings.ShowMatchPercent; }
            set 
            { 
                eclipseSettings.ShowMatchPercent = value;
                OnPropertyChanged();
            }
        }

        public bool ShowPlatformLogo
        {
            get { return eclipseSettings.ShowPlatformLogo; }
            set 
            { 
                eclipseSettings.ShowPlatformLogo = value;
                OnPropertyChanged();
            }
        }

        public bool ShowPlayMode
        {
            get { return eclipseSettings.ShowPlayMode; }
            set 
            { 
                eclipseSettings.ShowPlayMode = value;
                OnPropertyChanged();
            }
        }

        public bool ShowNotes
        {
            get { return eclipseSettings.ShowNotes; }
            set 
            { 
                eclipseSettings.ShowNotes = value;
                OnPropertyChanged();
            }
        }

        public bool ShowReleaseYear
        {
            get { return eclipseSettings.ShowReleaseYear; }
            set 
            { 
                eclipseSettings.ShowReleaseYear = value;
                OnPropertyChanged();
            }
        }

        public bool ShowStarRating
        {
            get { return eclipseSettings.ShowStarRating; }
            set 
            { 
                eclipseSettings.ShowStarRating = value;
                OnPropertyChanged();
            }
        }

        // Layout Settings
        public bool DisplayFeaturedGame
        {
            get { return eclipseSettings.DisplayFeaturedGame; }
            set 
            { 
                eclipseSettings.DisplayFeaturedGame = value;
                OnPropertyChanged();
            }
        }

        public bool ShowOptionsIcon
        {
            get { return eclipseSettings.ShowOptionsIcon; }
            set 
            { 
                eclipseSettings.ShowOptionsIcon = value;
                OnPropertyChanged();
            }
        }

        public bool DisplayOptionsOnEscape
        {
            get { return eclipseSettings.DisplayOptionsOnEscape; }
            set 
            { 
                eclipseSettings.DisplayOptionsOnEscape = value;
                OnPropertyChanged();
            }
        }

        // Box Margin Settings
        public double BoxFrontMarginLeft
        {
            get { return eclipseSettings.BoxFrontMarginLeft; }
            set 
            { 
                eclipseSettings.BoxFrontMarginLeft = value;
                OnPropertyChanged();
                UpdateMarginSample();
            }
        }

        public double BoxFrontMarginRight
        {
            get { return eclipseSettings.BoxFrontMarginRight; }
            set 
            { 
                eclipseSettings.BoxFrontMarginRight = value;
                OnPropertyChanged();
                UpdateMarginSample();
            }
        }

        public double BoxFrontMarginTop
        {
            get { return eclipseSettings.BoxFrontMarginTop; }
            set 
            { 
                eclipseSettings.BoxFrontMarginTop = value;
                OnPropertyChanged();
                UpdateMarginSample();
            }
        }

        public double BoxFrontMarginBottom
        {
            get { return eclipseSettings.BoxFrontMarginBottom; }
            set 
            { 
                eclipseSettings.BoxFrontMarginBottom = value;
                OnPropertyChanged();
                UpdateMarginSample();
            }
        }

        private Thickness marginSample;
        public Thickness MarginSample
        {
            get { return marginSample; }
            set 
            { 
                marginSample = value;
                OnPropertyChanged();
            }
        }

        // Game Details Padding
        public double SelectedGameDetailsPadding
        {
            get { return eclipseSettings.SelectedGameDetailsPadding; }
            set 
            { 
                eclipseSettings.SelectedGameDetailsPadding = value;
                OnPropertyChanged();
            }
        }

        private void UpdateMarginSample()
        {
            MarginSample = new Thickness(
                BoxFrontMarginLeft, 
                BoxFrontMarginTop, 
                BoxFrontMarginRight, 
                BoxFrontMarginBottom);
        }
    }
}