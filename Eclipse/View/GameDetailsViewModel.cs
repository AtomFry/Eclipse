using System.ComponentModel;
using System.Runtime.CompilerServices;
using Eclipse.Models;

namespace Eclipse.View
{
    public class GameDetailsViewModel : INotifyPropertyChanged
    {
        private readonly Models.EclipseSettings eclipseSettings;

        public GameDetailsViewModel(Models.EclipseSettings settings)
        {
            eclipseSettings = settings;
        }

        public bool ShowMatchPercent
        {
            get => eclipseSettings.ShowMatchPercent;
            set
            {
                eclipseSettings.ShowMatchPercent = value;
                OnPropertyChanged();
            }
        }

        public bool ShowPlatformLogo
        {
            get => eclipseSettings.ShowPlatformLogo;
            set
            {
                eclipseSettings.ShowPlatformLogo = value;
                OnPropertyChanged();
            }
        }

        public bool ShowPlayMode
        {
            get => eclipseSettings.ShowPlayMode;
            set
            {
                eclipseSettings.ShowPlayMode = value;
                OnPropertyChanged();
            }
        }

        public bool ShowReleaseYear
        {
            get => eclipseSettings.ShowReleaseYear;
            set
            {
                eclipseSettings.ShowReleaseYear = value;
                OnPropertyChanged();
            }
        }

        public bool ShowStarRating
        {
            get => eclipseSettings.ShowStarRating;
            set
            {
                eclipseSettings.ShowStarRating = value;
                OnPropertyChanged();
            }
        }

        public bool ShowOptionsIcon
        {
            get => eclipseSettings.ShowOptionsIcon;
            set
            {
                eclipseSettings.ShowOptionsIcon = value;
                OnPropertyChanged();
            }
        }

        public bool ShowNotes
        {
            get => eclipseSettings.ShowNotes;
            set
            {
                eclipseSettings.ShowNotes = value;
                OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}