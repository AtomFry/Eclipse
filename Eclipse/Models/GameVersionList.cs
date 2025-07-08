using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Eclipse.Models
{
    public class GameVersionList : INotifyPropertyChanged
    {
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<GameVersion> DisplayedGameVersions { get; set; }

        public int SelectedIndex { get; set; }

        private GameVersion selectedGameVersion;
        public GameVersion SelectedGameVersion
        {
            get => selectedGameVersion;
            set
            {
                selectedGameVersion = value;
                OnPropertyChanged();
            }
        }

        private List<GameVersion> gameVersions;
        public List<GameVersion> GameVersions
        {
            get => gameVersions;
            set
            {
                gameVersions = value;
                OnPropertyChanged();
            }
        }

        public GameVersionList(List<GameVersion> _gameVersions)
        {
            DisplayedGameVersions = new ObservableCollection<GameVersion>();

            GameVersions = _gameVersions;

            SelectedIndex = 0;

            if (GameVersions.Count > 0)
            {
                GameVersions[SelectedIndex].Selected = true;
            }

            RefreshOptions();
        }

        public void CycleForward()
        {
            GameVersions[SelectedIndex].Selected = false;
            if (SelectedIndex + 1 >= GameVersions.Count)
            {
                SelectedIndex = 0;
            }
            else
            {
                SelectedIndex++;
            }
            GameVersions[SelectedIndex].Selected = true;

            SelectedGameVersion = GameVersions[SelectedIndex];
        }

        public void CycleBackward()
        {
            GameVersions[SelectedIndex].Selected = false;
            if (SelectedIndex - 1 < 0)
            {
                SelectedIndex = GameVersions.Count - 1;
            }
            else
            {
                SelectedIndex--;
            }
            GameVersions[SelectedIndex].Selected = true;

            SelectedGameVersion = GameVersions[SelectedIndex];
        }

        private void RefreshOptions()
        {
            DisplayedGameVersions.Clear();

            if (GameVersions != null)
            {
                foreach (GameVersion option in GameVersions)
                {
                    DisplayedGameVersions.Add(option);
                }
            }

            SelectedGameVersion = GameVersions[SelectedIndex];

            HasAdditionalVersions = DisplayedGameVersions?.Count() > 1;
        }

        private bool hasAdditionalVersions;
        public bool HasAdditionalVersions
        {
            get => hasAdditionalVersions;
            set
            {
                hasAdditionalVersions = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}
