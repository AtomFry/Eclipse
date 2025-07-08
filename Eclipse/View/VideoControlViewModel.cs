using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Eclipse.View
{
    public class VideoControlViewModel : INotifyPropertyChanged
    {
        private double videoVolume;

        public double VideoVolume
        {
            get => videoVolume;
            set
            {
                if (videoVolume != value)
                {
                    videoVolume = value;
                    OnPropertyChanged();
                }
            }
        }

        public void AdjustVideoVolume(double increment)
        {
            if (VideoVolume + increment > 1)
            {
                VideoVolume = 1;
            }
            else if (VideoVolume + increment < 0)
            {
                VideoVolume = 0;
            }
            else
            {
                VideoVolume += increment;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}