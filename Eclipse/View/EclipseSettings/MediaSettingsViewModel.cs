using Eclipse.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Eclipse.View.EclipseSettings
{
    public class MediaSettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Models.EclipseSettings eclipseSettings;

        public ObservableCollection<Option> PageFunctions { get; }

        public MediaSettingsViewModel(Models.EclipseSettings eclipseSettings)
        {
            this.eclipseSettings = eclipseSettings ?? throw new System.ArgumentNullException(nameof(eclipseSettings));
            PageFunctions = new ObservableCollection<Option>();
            InitializePageFunctions();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Video Settings
        public bool DisableVideos
        {
            get { return eclipseSettings.DisableVideos; }
            set 
            { 
                eclipseSettings.DisableVideos = value;
                OnPropertyChanged();
            }
        }

        public double DefaultVideoVolume
        {
            get { return eclipseSettings.DefaultVideoVolume; }
            set 
            { 
                eclipseSettings.DefaultVideoVolume = value;
                OnPropertyChanged();
            }
        }

        public int VideoDelayInMilliseconds
        {
            get { return eclipseSettings.VideoDelayInMilliseconds; }
            set 
            { 
                eclipseSettings.VideoDelayInMilliseconds = value;
                OnPropertyChanged();
            }
        }

        // Voice Search Settings
        public bool EnableVoiceSearch
        {
            get { return eclipseSettings.EnableVoiceSearch; }
            set 
            { 
                eclipseSettings.EnableVoiceSearch = value;
                OnPropertyChanged();
            }
        }

        // Screensaver Settings
        public bool EnableScreenSaver
        {
            get { return eclipseSettings.EnableScreenSaver; }
            set 
            { 
                eclipseSettings.EnableScreenSaver = value;
                OnPropertyChanged();
            }
        }

        public int ScreensaverDelayInSeconds
        {
            get { return eclipseSettings.ScreensaverDelayInSeconds; }
            set 
            { 
                eclipseSettings.ScreensaverDelayInSeconds = value;
                OnPropertyChanged();
            }
        }

        // Input Function Mappings
        public PageFunction PageUpFunction
        {
            get { return eclipseSettings.PageUpFunction; }
            set 
            { 
                eclipseSettings.PageUpFunction = value;
                OnPropertyChanged();
            }
        }

        public PageFunction PageDownFunction
        {
            get { return eclipseSettings.PageDownFunction; }
            set 
            { 
                eclipseSettings.PageDownFunction = value;
                OnPropertyChanged();
            }
        }

        // Navigation Behavior
        public bool BypassDetails
        {
            get { return eclipseSettings.BypassDetails; }
            set 
            { 
                eclipseSettings.BypassDetails = value;
                OnPropertyChanged();
            }
        }

        private void InitializePageFunctions()
        {
            PageFunctions.Add(new Option { Name = "Page Up", });
            PageFunctions.Add(new Option { Name = "Page Down", });
            PageFunctions.Add(new Option { Name = "Random Game", });
            PageFunctions.Add(new Option { Name = "Voice Search", });
            PageFunctions.Add(new Option { Name = "Zoom Box", });
            PageFunctions.Add(new Option { Name = "Volume Up", });
            PageFunctions.Add(new Option { Name = "Volume Down", });
            PageFunctions.Add(new Option { Name = "Display Details", });
            PageFunctions.Add(new Option { Name = "Flip Box", });
            PageFunctions.Add(new Option { Name = "Play Game", });
        }
    }
}