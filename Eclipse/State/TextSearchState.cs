using Eclipse.Models;
using Eclipse.Service;
using Eclipse.Service.Search;
using System.Collections.Generic;

namespace Eclipse.State
{
    /// <summary>
    /// The search screen's eight inputs, and the two transitions the session cannot own:
    /// getting here from browsing, and putting the results in front of the user.
    ///
    /// THE SHAPE OF THIS SCREEN. Search is an overlay on the browsing surface, not a screen of
    /// its own. The results are installed as an ordinary GameListSet and shown with
    /// ShowCategory - exactly what VoiceRecognitionState does with a finished voice search, only
    /// driven live on each keystroke rather than once at the end. Everything below the row
    /// therefore behaves as it does anywhere else in Eclipse: the selection box, the previous
    /// game half off screen, the artwork hydration priority, the decode-ahead, the background
    /// image, the video preview and the selected game's details.
    ///
    /// What that leaves here is routing. While the cursor is on the keyboard, directions drive
    /// the key grid; while it is on the results, they drive the navigator. Nothing in this class
    /// computes anything about the search.
    /// </summary>
    public class TextSearchState : EclipseState
    {
        private readonly AttractModeService attractModeService;

        // Whether the user's place in the library was captured on the way in, so Escape only
        // tries to restore something that was actually remembered.
        private bool positionRemembered;

        public TextSearchState()
        {
            attractModeService = AttractModeService.Instance;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            View.MainWindowViewModel viewModel = eclipseStateContext.MainWindowViewModel;

            // Where the user was, so Escape can put them back. The same mechanism favouriting a
            // game already uses; guarded because a set with no selection would throw and this is
            // reachable before the first list is built.
            positionRemembered = false;
            if (viewModel.Navigator.CurrentSet != null && viewModel.Navigator.CurrentList?.SelectedGame != null)
            {
                viewModel.Navigator.RememberPosition();
                positionRemembered = true;
            }

            // The screen the search was opened from has to get out of the way. Voice search
            // learned this the hard way: starting from the detail overlay used to leave Play /
            // Favourite / More like this sitting behind the listening indicator.
            viewModel.IsDisplayingFeature = false;
            viewModel.IsDisplayingMoreInfo = false;
            viewModel.IsPickingCategory = false;
            viewModel.IsDisplayingSearch = true;

            viewModel.Search.Session.Enter();
            PublishResults(eclipseStateContext, restoreRememberedPlace: true);

            // Restarted rather than stopped. Unlike a voice search, which lasts one utterance,
            // this screen can sit open indefinitely while someone works out what to type. A user
            // who walks away should still get the screensaver; attract mode returns to whatever
            // state it interrupted, so it comes back here with everything intact.
            attractModeService.RestartAttractMode();
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            Session(eclipseStateContext).MoveUp(held);
            return true;
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            Session(eclipseStateContext).MoveDown(held);
            return true;
        }

        /// <summary>
        /// Left drives whichever zone has the cursor.
        ///
        /// RULE-SEARCH-036 - it never opens the options pane here, whatever
        /// OpenSettingsPaneOnLeft says. Falling into the settings pane off the leftmost key of
        /// the keyboard would be indefensible.
        /// </summary>
        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            if (Session(eclipseStateContext).IsOnResults)
            {
                eclipseStateContext.MainWindowViewModel.Navigator.MoveToPreviousGame();
                return true;
            }

            Session(eclipseStateContext).MoveKeyboardLeft();
            return true;
        }

        public bool OnRight(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            if (Session(eclipseStateContext).IsOnResults)
            {
                eclipseStateContext.MainWindowViewModel.Navigator.MoveToNextGame();
                return true;
            }

            Session(eclipseStateContext).MoveKeyboardRight();
            return true;
        }

        /// <summary>
        /// RULE-SEARCH-033 - Enter acts on whatever is highlighted. On a key that means typing
        /// it; on a result it means doing what Enter does while browsing.
        /// </summary>
        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            SearchSession session = Session(eclipseStateContext);

            if (session.IsOnResults)
            {
                CloseSearch(eclipseStateContext, keepResults: true);
                OpenSelectedGame(eclipseStateContext);
                return true;
            }

            session.PressSelectedKey();
            PublishResults(eclipseStateContext, restoreRememberedPlace: false);
            return true;
        }

        /// <summary>
        /// RULE-SEARCH-034 - Escape always leaves, whatever zone the cursor is in, and the
        /// session is kept (RULE-SEARCH-035) so an accidental press costs nothing.
        ///
        /// The library goes back to where the user was before they opened search. A single
        /// unconditional exit rather than a ladder that unwinds zone by zone: with a ladder the
        /// user cannot tell how many presses gets them out.
        /// </summary>
        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            CloseSearch(eclipseStateContext, keepResults: false);
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        /// <summary>
        /// RULE-SEARCH-042 - Page Up is Backspace inside the search screen, regardless of how the
        /// user has mapped it. Its configured browse function has no meaning here, and text entry
        /// on a d-pad is slow enough that a free delete button is worth more.
        /// </summary>
        public bool OnPageUp(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            Session(eclipseStateContext).Backspace();
            PublishResults(eclipseStateContext, restoreRememberedPlace: false);
            return true;
        }

        /// <summary>
        /// RULE-SEARCH-042 - Page Down closes the search panel and leaves the user browsing the
        /// results, without having to walk down to the row first.
        /// </summary>
        public bool OnPageDown(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            if (!Session(eclipseStateContext).HasResults)
            {
                // Nothing to hand over. Stay put rather than dumping the user into an empty
                // library - they are mid-search and the query is still there to edit.
                return true;
            }

            CloseSearch(eclipseStateContext, keepResults: true);
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        /// <summary>
        /// Puts the current results in front of the user as an ordinary list.
        ///
        /// Called on entry and after every edit. Installing a set and showing it is what voice
        /// search does with its finished results; doing it live is the whole of what makes the
        /// search row a real Eclipse row rather than a second, worse one.
        ///
        /// A query that matches nothing - including the empty query the screen opens on - hides
        /// the browsing surface rather than installing an empty list. ShowCategory deliberately
        /// refuses to switch to a set with no lists, and a stale row sitting behind the keyboard
        /// would claim to be results that no longer exist.
        /// </summary>
        private void PublishResults(EclipseStateContext eclipseStateContext, bool restoreRememberedPlace)
        {
            View.MainWindowViewModel viewModel = eclipseStateContext.MainWindowViewModel;
            SearchSession session = viewModel.Search.Session;

            if (!session.HasResults)
            {
                viewModel.IsDisplayingResults = false;
                return;
            }

            List<GameMatch> games = SearchableGameProjection.ToGameMatches(viewModel.gameCatalog, session.Results);
            if (games.Count == 0)
            {
                viewModel.IsDisplayingResults = false;
                return;
            }

            string listName = string.IsNullOrWhiteSpace(session.Query) ? "Search" : session.Query;

            viewModel.Navigator.InstallSet(new GameListSet
            {
                ListCategoryType = ListCategoryType.TextSearch,
                GameLists = new List<GameList>
                {
                    new GameList(listName, games)
                }
            });

            viewModel.Navigator.ShowCategory(ListCategoryType.TextSearch);

            // Coming back into the screen puts the user where they left off (RULE-SEARCH-035).
            // An edit does not: a new result set is a new list, in which the old place means
            // nothing, so it starts at the best match.
            if (restoreRememberedPlace
                && session.RememberedResultIndex > 0
                && session.RememberedResultIndex < games.Count)
            {
                viewModel.Navigator.CurrentList?.SetGameIndex(session.RememberedResultIndex);
            }

            viewModel.IsDisplayingResults = true;
        }

        /// <summary>
        /// Takes the screen out of search. Every way out goes through here, so none of them can
        /// half-do it - the lesson voice search's nine hand-written exit paths taught.
        /// </summary>
        /// <param name="keepResults">
        /// Whether the user is leaving <em>into</em> the results or away from them. Escaping puts
        /// the library back exactly as it was; committing leaves the results installed and the
        /// selection where the user put it.
        /// </param>
        private void CloseSearch(EclipseStateContext eclipseStateContext, bool keepResults)
        {
            View.MainWindowViewModel viewModel = eclipseStateContext.MainWindowViewModel;
            SearchSession session = viewModel.Search.Session;

            // Where they were in the results, so returning to search comes back to it.
            session.RememberedResultIndex = viewModel.Navigator.CurrentList?.CurrentGameIndex ?? 0;

            session.Leave();
            viewModel.IsDisplayingSearch = false;

            // Put back what PublishResults may have taken away. This is not belt and braces: the
            // game detail overlay lives inside Grid_DisplayingResults in the markup, so opening
            // it while this is false shows the user a blank screen.
            viewModel.IsDisplayingResults = true;

            if (!keepResults && positionRemembered)
            {
                viewModel.Navigator.RestorePosition();
            }

            positionRemembered = false;
            attractModeService.RestartAttractMode();
        }

        /// <summary>
        /// Does what Enter does while browsing, so picking a game out of the results behaves
        /// exactly as picking one out of a row does - including BypassDetails.
        /// </summary>
        private static void OpenSelectedGame(EclipseStateContext eclipseStateContext)
        {
            View.MainWindowViewModel viewModel = eclipseStateContext.MainWindowViewModel;

            if (EclipseSettingsDataProvider.Instance.EclipseSettings.BypassDetails)
            {
                eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
                viewModel.PlayCurrentGame();
                return;
            }

            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(GameDetailOptionsState)));
        }

        private static SearchSession Session(EclipseStateContext eclipseStateContext)
        {
            return eclipseStateContext.MainWindowViewModel.Search.Session;
        }
    }
}
