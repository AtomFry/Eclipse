using Eclipse.Service;
using System.ComponentModel;
using Unbroken.LaunchBox.Plugins.Data;
using System.Runtime.CompilerServices;

namespace Eclipse.Models
{
    public class GameVersion : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public IGame Game { get; set; }

        public IAdditionalApplication AdditionalApplication { get; set; }

        public GameVersion(IGame _game, IAdditionalApplication _additionalApplication)
        {
            Game = _game;
            AdditionalApplication = _additionalApplication;

            if (AdditionalApplication != null)
            {
                switch (EclipseSettingsDataProvider.Instance.EclipseSettings.AdditionalApplicationDisplayField)
                {
                    case AdditionalApplicationDisplayField.Name:
                        Description = AdditionalApplication.Name;

                        if (EclipseSettingsDataProvider.Instance.EclipseSettings.AdditionalVersionsRemovePlayPrefix)
                        {
                            if (AdditionalApplication?.Name?.StartsWith("Play ") == true)
                            {
                                Description = Description.Substring(5);
                            }
                        }

                        if (EclipseSettingsDataProvider.Instance.EclipseSettings.AdditionalVersionsRemoveVersionPostfix)
                        {
                            if (AdditionalApplication?.Name?.EndsWith("Version...") == true)
                            {
                                Description = Description.Substring(0, Description.IndexOf("Version..."));
                            }
                        }

                        break;

                    case AdditionalApplicationDisplayField.Region:
                        Description = AdditionalApplication.Region;
                        break;

                    case AdditionalApplicationDisplayField.Version:
                        Description = AdditionalApplication.Version;
                        break;

                    default:
                        break;
                }
            }
            else
            {
                /*
                Description = string.IsNullOrWhiteSpace(Game.Version) ?
                    string.IsNullOrWhiteSpace(Game.Region) ?
                    Game.Title :
                    Game.Region :
                    Game.Version;
                */
                Description = Game.Title;
            }
        }

        private string description;
        public string Description
        {
            get => description;
            set
            {
                description = value;
                OnPropertyChanged();
            }
        }

        private bool selected;
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                OnPropertyChanged();
            }
        }
    }
}
