using System.ComponentModel;
using Eclipse.Models;
using Eclipse.Service.Search;

namespace Eclipse.View
{
    /// <summary>
    /// What the search panel draws: the keyboard, the query line and the status.
    ///
    /// And nothing else. The results are not here - they are installed as an ordinary Eclipse
    /// GameList and drawn by the browsing surface that was already on screen, which is how the
    /// selection box, the previous-game affordance, the artwork hydration, the background image
    /// and the video preview all come for free rather than being reimplemented worse.
    ///
    /// This class was twice this size and owned a bespoke thirteen-slot box art window. That
    /// window was the wrong idea: docs/plans/text-search.md 6.1 says the row "is nearly free -
    /// GameList already is a ranked row of games with a moving window", and building a second
    /// one produced a row with no selection chrome, no previous game, the wrong scroll
    /// behaviour and artwork that never got hydration priority because it was not the current
    /// list.
    ///
    /// One-way and derived: the session is the source of truth, this republishes it for XAML.
    /// </summary>
    public class SearchViewModel : INotifyPropertyChanged
    {
        public SearchViewModel(SearchSession session)
        {
            Session = session;

            if (Session != null)
            {
                Session.Changed += (sender, args) => Refresh();
            }
        }

        /// <summary>The state machine this is a view of.</summary>
        public SearchSession Session { get; }

        /// <summary>The key grid, for the keyboard to bind to.</summary>
        public SearchKeyboard Keyboard => Session?.Keyboard;

        /// <summary>What has been typed, for the query line.</summary>
        public string Query => Session?.Query ?? string.Empty;

        public bool HasResults => Session?.HasResults == true;

        /// <summary>
        /// True while the cursor is on the keyboard.
        ///
        /// Drives two things beyond the obvious. The selected key is drawn as an outline rather
        /// than a filled tile when this is false, so a cursor parked on the keyboard while the
        /// user is browsing results does not look like it is still taking input. And the video
        /// preview holds off while this is true, so typing does not start a video behind the
        /// keyboard on every keystroke.
        /// </summary>
        public bool IsOnKeyboard => Session == null || Session.Zone == SearchZone.Keyboard;

        /// <summary>
        /// The line under the query: how many games matched, or why none did.
        ///
        /// The count is the most useful thing on the screen - it tells the user whether the next
        /// character will help or whether they have already over-narrowed - so the states that
        /// are not a count say what they are instead of showing a bare zero.
        /// </summary>
        public string Status
        {
            get
            {
                if (Session == null)
                {
                    return string.Empty;
                }

                switch (Session.Availability)
                {
                    case SearchAvailability.Preparing:
                        return "Getting search ready...";

                    case SearchAvailability.Failed:
                        return "Search is not available";

                    case SearchAvailability.Disabled:
                        return "Search is turned off in the Eclipse settings";
                }

                if (!Session.HasQuery)
                {
                    return "Type to search";
                }

                if (!Session.HasResults)
                {
                    return "No games found";
                }

                return Session.ResultCount == 1 ? "1 game" : $"{Session.ResultCount} games";
            }
        }

        /// <summary>Pulls everything through from the session, on every change it raises.</summary>
        public void Refresh()
        {
            Notify("Query");
            Notify("HasResults");
            Notify("IsOnKeyboard");
            Notify("Status");
        }

        private void Notify(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}
