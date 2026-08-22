using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Eclipse.State.GameDetailOptions
{
    // The options in the detail overlay, in display order, and which one the user is on.
    //
    // The order used to be spread across the option classes themselves - each named the
    // neighbour to move to on Up and on Down, so the ring existed only as eight hardcoded
    // edges and adding an option meant editing two unrelated files. It is now just the order
    // of this collection.
    //
    // Mirrors OptionList in Models/Option.cs, which does the same job for the category picker.
    public class GameDetailOptionList : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        /// <summary>The options, in the order they appear on screen.</summary>
        public ObservableCollection<GameDetailOptionItem> Options { get; }

        public int SelectedIndex { get; private set; }

        public GameDetailOptionItem SelectedOption => Options[SelectedIndex];

        public GameDetailOptionList()
            : this(new List<GameDetailOptionItem>
            {
                new PlayDetailOption(),
                new FavoriteDetailOption(),
                new MoreLikeThisDetailOption(),
                new RatingDetailOption()
            })
        {
        }

        public GameDetailOptionList(IEnumerable<GameDetailOptionItem> options)
        {
            Options = new ObservableCollection<GameDetailOptionItem>(options);
            SelectedIndex = 0;
        }

        /// <summary>
        /// Put the selection back to the first option without running any enter or exit
        /// behaviour. The overlay always opens on the first option however it was left.
        /// </summary>
        public void Reset()
        {
            foreach (GameDetailOptionItem option in Options)
            {
                option.Selected = false;
            }

            SelectedIndex = 0;
            OnSelectedIndexChanged();
        }

        public void CycleForward(EclipseStateContext eclipseStateContext)
        {
            MoveTo(SelectedIndex + 1 >= Options.Count ? 0 : SelectedIndex + 1, eclipseStateContext);
        }

        public void CycleBackward(EclipseStateContext eclipseStateContext)
        {
            MoveTo(SelectedIndex - 1 < 0 ? Options.Count - 1 : SelectedIndex - 1, eclipseStateContext);
        }

        /// <summary>Mark the current option selected and let it run its arrival behaviour.</summary>
        public void SelectCurrent(EclipseStateContext eclipseStateContext)
        {
            SelectedOption.Selected = true;
            SelectedOption.OnSelected(eclipseStateContext);
        }

        private void MoveTo(int newIndex, EclipseStateContext eclipseStateContext)
        {
            GameDetailOptionItem leaving = SelectedOption;

            leaving.Selected = false;
            leaving.OnDeselected(eclipseStateContext);

            SelectedIndex = newIndex;

            SelectedOption.Selected = true;
            SelectedOption.OnSelected(eclipseStateContext);

            OnSelectedIndexChanged();
        }

        private void OnSelectedIndexChanged()
        {
            PropertyChanged(this, new PropertyChangedEventArgs("SelectedIndex"));
            PropertyChanged(this, new PropertyChangedEventArgs("SelectedOption"));
        }
    }
}
