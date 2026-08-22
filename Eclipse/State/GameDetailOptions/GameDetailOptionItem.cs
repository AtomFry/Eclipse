using Eclipse.Models;
using System.ComponentModel;

namespace Eclipse.State.GameDetailOptions
{
    // One option in the game detail overlay - Play, Favorite, More like this, Rating.
    //
    // These used to be four EclipseState classes of about 85 lines each, of which only a
    // handful differed: the rest was an identical Escape handler, identical Page Up/Down
    // handlers, an attract mode call in every method, and each class naming its neighbours
    // to build the navigation ring. GameDetailOptionsState now owns all of that, and an
    // option carries only what makes it different from the other three.
    public abstract class GameDetailOptionItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        /// <summary>Which option this is. Used to pick the content template for the row.</summary>
        public abstract GameDetailOption Kind { get; }

        private bool selected;

        /// <summary>Whether this is the option the user is currently on. Drives the highlight.</summary>
        public bool Selected
        {
            get { return selected; }
            set
            {
                if (selected != value)
                {
                    selected = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Selected"));
                }
            }
        }

        /// <summary>The user has moved onto this option.</summary>
        public virtual void OnSelected(EclipseStateContext eclipseStateContext)
        {
        }

        /// <summary>
        /// The user has moved off this option to another one. This is a commit point - the
        /// rating option saves here, which is why leaving it with Up or Down keeps the change.
        /// </summary>
        public virtual void OnDeselected(EclipseStateContext eclipseStateContext)
        {
        }

        /// <summary>
        /// The user pressed Escape while on this option. Distinct from OnDeselected because
        /// Escape means abandon rather than commit.
        /// </summary>
        public virtual void OnCancelled(EclipseStateContext eclipseStateContext)
        {
        }

        /// <summary>
        /// Enter. Each option handles its own attract mode call: launching a game stops the
        /// screen saver outright, whereas the others are ordinary activity and restart the
        /// idle timer.
        /// </summary>
        public abstract void Activate(EclipseStateContext eclipseStateContext);

        /// <summary>Left. Only Play and Rating do anything; the rest deliberately ignore it.</summary>
        public virtual void MoveLeft(EclipseStateContext eclipseStateContext)
        {
        }

        /// <summary>Right. Only Play and Rating do anything; the rest deliberately ignore it.</summary>
        public virtual void MoveRight(EclipseStateContext eclipseStateContext)
        {
        }
    }
}
