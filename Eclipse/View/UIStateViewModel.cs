using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Eclipse.View
{
    public class UIStateViewModel : INotifyPropertyChanged
    {
        private bool isInitializing;
        private bool isPickingCategory;
        private bool isDisplayingFeature;
        private bool isDisplayingAttractMode;
        private bool isRecognizing;
        private bool isDisplayingResults;
        private bool isDisplayingMoreInfo;
        private bool isDisplayingError;
        private bool isDisplayingSearch;
        private bool isRatingGame;
        private bool isZoomingBox;
        private bool isPlayingGame;

        public bool IsInitializing
        {
            get => isInitializing;
            set
            {
                if (isInitializing != value)
                {
                    isInitializing = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPickingCategory
        {
            get => isPickingCategory;
            set
            {
                if (isPickingCategory != value)
                {
                    isPickingCategory = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDisplayingFeature
        {
            get => isDisplayingFeature;
            set
            {
                if (isDisplayingFeature != value)
                {
                    isDisplayingFeature = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDisplayingAttractMode
        {
            get => isDisplayingAttractMode;
            set
            {
                if (isDisplayingAttractMode != value)
                {
                    isDisplayingAttractMode = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRecognizing
        {
            get => isRecognizing;
            set
            {
                if (isRecognizing != value)
                {
                    isRecognizing = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDisplayingResults
        {
            get => isDisplayingResults;
            set
            {
                if (isDisplayingResults != value)
                {
                    isDisplayingResults = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDisplayingMoreInfo
        {
            get => isDisplayingMoreInfo;
            set
            {
                if (isDisplayingMoreInfo != value)
                {
                    isDisplayingMoreInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDisplayingError
        {
            get => isDisplayingError;
            set
            {
                if (isDisplayingError != value)
                {
                    isDisplayingError = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDisplayingSearch
        {
            get => isDisplayingSearch;
            set
            {
                if (isDisplayingSearch != value)
                {
                    isDisplayingSearch = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRatingGame
        {
            get => isRatingGame;
            set
            {
                if (isRatingGame != value)
                {
                    isRatingGame = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsZoomingBox
        {
            get => isZoomingBox;
            set
            {
                if (isZoomingBox != value)
                {
                    isZoomingBox = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPlayingGame
        {
            get => isPlayingGame;
            set
            {
                if (isPlayingGame != value)
                {
                    isPlayingGame = value;
                    OnPropertyChanged();
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}