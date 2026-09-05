using System;
using System.Collections.Generic;
using System.Text;
using Eclipse.Models;

namespace Eclipse.Service.Search
{
    /// <summary>Which part of the search screen has the cursor.</summary>
    public enum SearchZone
    {
        /// <summary>The key grid. Always present.</summary>
        Keyboard,

        /// <summary>The box art row. Present only while there are results.</summary>
        Results

        // Chips and Suggestions join this in stage 4. RULE-SEARCH-032 says a zone with no
        // content is skipped, never focused and never shown - so a zone that does not exist yet
        // is simply the same case, and nothing here has to anticipate it.
    }

    /// <summary>
    /// What has been typed, where the cursor is, and what the engine said about it.
    ///
    /// The state machine between input and the engine. It owns the interaction; the engine owns
    /// the computation. Nothing here touches WPF, a dispatcher, a GameMatch, a catalog or a
    /// singleton - results are catalog indices, exactly as the engine produced them.
    ///
    /// WHAT IT DELIBERATELY DOES NOT OWN. Which result is selected. The search results are
    /// installed as an ordinary Eclipse GameList and browsed with the ordinary navigator, so
    /// the selection lives where every other selection in the product lives - in the list's own
    /// cycle. An earlier version of this class kept its own SelectedResultIndex and projected a
    /// bespoke row from it; that meant reimplementing the box art window, the previous-game
    /// affordance, the artwork hydration priority and the decode-ahead, all of them worse than
    /// the originals. The session knows the results; it does not know which one you are looking
    /// at.
    ///
    /// LIFETIME. One session per Big Box run, kept across leaving and re-entering the screen -
    /// RULE-SEARCH-035, which is what makes Escape safe to be a single unconditional exit
    /// (RULE-SEARCH-034). It deliberately does not survive a restart.
    /// </summary>
    public sealed class SearchSession
    {
        /// <summary>
        /// How many results the row holds. Generous next to the thirteen slots on screen, and
        /// far short of anything a person would scroll.
        /// </summary>
        public const int LiveResultLimit = SearchEngine.DefaultMaxResults;

        private static readonly SearchHit[] NoHits = new SearchHit[0];

        private readonly ISearchIndexSource indexSource;
        private readonly StringBuilder query = new StringBuilder();

        public SearchSession(ISearchIndexSource indexSource, SearchKeyboardLayout layout)
        {
            this.indexSource = indexSource;
            Keyboard = new SearchKeyboard(layout);
            Results = NoHits;
        }

        /// <summary>Raised whenever anything a screen would draw has changed.</summary>
        public event EventHandler Changed;

        /// <summary>The key grid and its cursor.</summary>
        public SearchKeyboard Keyboard { get; }

        /// <summary>What has been typed so far. Never null.</summary>
        public string Query => query.ToString();

        public bool HasQuery => query.Length > 0;

        /// <summary>The current results, best first. Empty, never null.</summary>
        public IReadOnlyList<SearchHit> Results { get; private set; }

        public int ResultCount => Results.Count;

        public bool HasResults => Results.Count > 0;

        /// <summary>Which zone the cursor is in.</summary>
        public SearchZone Zone { get; private set; } = SearchZone.Keyboard;

        public bool IsOnResults => Zone == SearchZone.Results;

        /// <summary>
        /// Where in the results the user was when they last left the screen, so returning puts
        /// them back (RULE-SEARCH-035).
        ///
        /// Opaque to this class - it is written and read by whatever owns the results row, and
        /// never interpreted here. It exists on the session rather than in that owner because
        /// the session is the thing that survives leaving the screen.
        /// </summary>
        public int RememberedResultIndex { get; set; }

        /// <summary>Whether a search can run at all right now.</summary>
        public SearchAvailability Availability => indexSource?.Availability ?? SearchAvailability.Disabled;

        public bool IsReady => Availability == SearchAvailability.Ready;

        #region entering and leaving

        /// <summary>
        /// Opens the screen on whatever the last visit left behind (RULE-SEARCH-035).
        ///
        /// The query is re-run rather than trusted, because the index may have finished building
        /// since - a user who searched during startup, got "still getting ready" and came back
        /// should find their query answered rather than still empty.
        /// </summary>
        public void Enter()
        {
            // Not a query change: the text is the same one the user left behind, so nothing
            // about their place in it is reset here.
            RunQuery();
        }

        /// <summary>
        /// Leaves the screen, keeping everything. The cursor goes back to the keyboard because
        /// the results row belongs to the browsing surface once search is closed - returning to
        /// find the keyboard unfocused would mean typing did nothing.
        /// </summary>
        public void Leave()
        {
            Zone = SearchZone.Keyboard;
        }

        #endregion

        #region typing

        /// <summary>Applies whatever key the cursor is on.</summary>
        public void PressSelectedKey()
        {
            SearchKey key = Keyboard.SelectedKey;

            switch (key.Kind)
            {
                case SearchKeyKind.Character:
                    Append(key.Character);
                    break;

                case SearchKeyKind.Space:
                    Append(' ');
                    break;

                case SearchKeyKind.Backspace:
                    Backspace();
                    break;

                case SearchKeyKind.Clear:
                    Clear();
                    break;
            }
        }

        /// <summary>Adds one character and re-runs the search.</summary>
        public void Append(char character)
        {
            // A leading space would only produce an empty term, and holding the space key at
            // the start of an empty query is an easy accident on a d-pad.
            if (character == ' ' && query.Length == 0)
            {
                return;
            }

            query.Append(character);
            OnQueryEdited();
        }

        /// <summary>Removes the last character. Does nothing on an empty query.</summary>
        public void Backspace()
        {
            if (query.Length == 0)
            {
                return;
            }

            query.Length = query.Length - 1;
            OnQueryEdited();
        }

        /// <summary>
        /// Empties the query. The context-sensitive half of RULE-SEARCH-038 - clearing the
        /// filters - arrives with the filters themselves in stage 4; until then there is only
        /// ever the query to clear.
        /// </summary>
        public void Clear()
        {
            if (query.Length == 0)
            {
                return;
            }

            query.Length = 0;
            OnQueryEdited();
        }

        // An edit produces a different result set, in which the old place means nothing.
        private void OnQueryEdited()
        {
            RememberedResultIndex = 0;
            RunQuery();
        }

        #endregion

        #region moving

        /// <summary>Moves the keyboard cursor left, wrapping within the row.</summary>
        public void MoveKeyboardLeft()
        {
            Keyboard.MoveLeft();
            Raise();
        }

        /// <summary>Moves the keyboard cursor right, wrapping within the row.</summary>
        public void MoveKeyboardRight()
        {
            Keyboard.MoveRight();
            Raise();
        }

        /// <summary>
        /// Moves up: within the keyboard, or out of it at the top edge (RULE-SEARCH-031).
        /// </summary>
        /// <param name="held">
        /// Held movement never crosses a zone boundary (RULE-SEARCH-037), so a user travelling
        /// across the key grid cannot shoot out of it into the results.
        /// </param>
        public void MoveUp(bool held)
        {
            if (Zone == SearchZone.Results)
            {
                // out of the results, back to the nearest edge of the keyboard
                Zone = SearchZone.Keyboard;
                Keyboard.MoveToLastRow();
                Raise();
                return;
            }

            if (Keyboard.MoveUp())
            {
                Raise();
                return;
            }

            if (held || !HasResults)
            {
                // nowhere to go, so wrap inside the keyboard rather than sticking
                Keyboard.MoveToLastRow();
                Raise();
                return;
            }

            Zone = SearchZone.Results;
            Raise();
        }

        /// <summary>Moves down: within the keyboard, or out of it at the bottom edge. See MoveUp.</summary>
        public void MoveDown(bool held)
        {
            if (Zone == SearchZone.Results)
            {
                Zone = SearchZone.Keyboard;
                Keyboard.MoveToFirstRow();
                Raise();
                return;
            }

            if (Keyboard.MoveDown())
            {
                Raise();
                return;
            }

            if (held || !HasResults)
            {
                Keyboard.MoveToFirstRow();
                Raise();
                return;
            }

            Zone = SearchZone.Results;
            Raise();
        }

        #endregion

        #region results

        /// <summary>
        /// Re-runs the query against the index and republishes the results.
        ///
        /// Called on every keystroke. The engine is a pure function, so this is the whole of
        /// "the results changed": there is no incremental state to keep in step, and nothing to
        /// invalidate.
        /// </summary>
        private void RunQuery()
        {
            ISearchIndex index = indexSource?.Index;
            SearchQuery parsed = SearchQuery.Parse(Query);

            Results = index == null || parsed.IsEmpty
                ? NoHits
                : SearchEngine.Search(parsed, index, LiveResultLimit);

            // The results zone cannot hold the cursor when there are no results
            // (RULE-SEARCH-032), and being stranded in an empty zone is how a user ends up
            // pressing buttons that do nothing.
            if (!HasResults && Zone == SearchZone.Results)
            {
                Zone = SearchZone.Keyboard;
            }

            Raise();
        }

        /// <summary>
        /// The whole ranked result set, past the cap the row holds.
        ///
        /// Recomputed rather than cached: it is wanted once, at the moment the user commits, and
        /// keeping a second copy in step through every keystroke would be work done on every
        /// keystroke for a moment that happens once.
        /// </summary>
        public IReadOnlyList<SearchHit> AllResults()
        {
            ISearchIndex index = indexSource?.Index;
            SearchQuery parsed = SearchQuery.Parse(Query);

            if (index == null || parsed.IsEmpty)
            {
                return NoHits;
            }

            return SearchEngine.Search(parsed, index, 0);
        }

        #endregion

        private void Raise()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
