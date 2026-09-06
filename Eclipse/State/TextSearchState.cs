using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using Eclipse.Service.Search;
using System.Collections.Generic;
using System.Linq;

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

        // The row count last installed, so the log speaks when the shape of the results changes
        // rather than on every keystroke.
        private int lastInstalledRowCount = -1;

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
            //
            // ONLY WHEN ARRIVING FROM THE LIBRARY. Coming back from the game detail overlay the
            // navigator is still showing this search's own rows, and capturing that would
            // overwrite the very thing this exists to protect - Escape would then return the user
            // to the results they were already looking at instead of the library they came from.
            // Asked of the navigator rather than tracked with a flag, because a flag set on the
            // way out has to be cleared on every way back, including the ones that never return
            // here at all.
            if (viewModel.Navigator.CurrentSet?.ListCategoryType != ListCategoryType.TextSearch)
            {
                positionRemembered = false;

                if (viewModel.Navigator.CurrentSet != null && viewModel.Navigator.CurrentList?.SelectedGame != null)
                {
                    viewModel.Navigator.RememberPosition();
                    positionRemembered = true;
                }
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

        /// <summary>
        /// RULE-SEARCH-077 / 079 - inside the results, Up and Down walk the rows, and the rows
        /// are entered and left as one block: from the top going down, from the bottom going up.
        ///
        /// THE BLOCK IS THE POINT. The zone and the row are two separate cursors, and the first
        /// version of this left the row where it was when the zone moved on. That produced a
        /// dead end: Down out of the last row went to the keyboard with the rows still parked at
        /// the bottom, so walking back down the keyboard re-entered the rows at the last one and
        /// the next Down left again immediately. The middle rows were unreachable that way round,
        /// and it read as having lost your place. Rewinding the rows as the cursor leaves them is
        /// what makes going round the loop a second time behave like the first.
        ///
        /// Delegated to the navigator rather than handled in the session, for the reason Left and
        /// Right already are: which row is showing is the navigator's business, and the session
        /// owns only which <em>zone</em> has the cursor.
        /// </summary>
        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            GameListNavigator navigator = eclipseStateContext.MainWindowViewModel.Navigator;
            SearchSession session = Session(eclipseStateContext);

            if (session.IsOnResults && !navigator.IsOnFirstList)
            {
                navigator.MoveToPreviousList();
                return true;
            }

            bool wasOnResults = session.IsOnResults;
            session.MoveUp(held);

            // Wrapping up into the rows enters them from below, so it lands on the last of them
            // and carries on upward. Without this the cursor arrives at the first row and the
            // next Up leaves again, which is the same dead end from the other side.
            if (!wasOnResults && session.IsOnResults)
            {
                navigator.MoveToLastList();
            }

            return true;
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            GameListNavigator navigator = eclipseStateContext.MainWindowViewModel.Navigator;
            SearchSession session = Session(eclipseStateContext);

            if (session.IsOnResults && !navigator.IsOnLastList)
            {
                navigator.MoveToNextList();
                return true;
            }

            // Leaving the rows downward rewinds them, so the screen goes back to showing the
            // search itself while the user types - and so coming back down the keyboard enters
            // them at the top rather than at the end they were left at.
            if (session.IsOnResults)
            {
                navigator.MoveToFirstList();
            }

            session.MoveDown(held);
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

            Session(eclipseStateContext).MoveLeft(held);
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

            Session(eclipseStateContext).MoveRight(held);
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
                // OpenSelectedGame decides whether this ends the search - see the note there.
                OpenSelectedGame(eclipseStateContext);
                return true;
            }

            if (session.IsOnSuggestions)
            {
                session.ApplySelectedSuggestion();
                PublishResults(eclipseStateContext, restoreRememberedPlace: false);
                return true;
            }

            if (session.IsOnChips)
            {
                session.RemoveSelectedChip();
                PublishResults(eclipseStateContext, restoreRememberedPlace: false);
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
        /// Page Down does nothing here.
        ///
        /// It used to commit the search - install the results and close the panel. That made
        /// sense while the results were a preview of a list that did not exist yet; under the
        /// overlay design they are already the live list, so there is nothing to commit. The
        /// input is still swallowed rather than declined, because handing Page Down to Big Box
        /// from inside the search screen would drop the user somewhere unrelated.
        /// </summary>
        public bool OnPageDown(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
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

            // One list per row (RULE-SEARCH-073): the search itself, then the near misses. A row
            // that projects to nothing is dropped rather than installed empty - it would be a
            // heading over a blank strip, and ShowCategory would have to cope with it.
            List<GameList> lists = new List<GameList>(session.Rows.Count);

            foreach (SearchRowResult row in session.Rows)
            {
                List<GameMatch> games = SearchableGameProjection.ToGameMatches(viewModel.gameCatalog, row.Hits);

                if (games.Count > 0)
                {
                    lists.Add(new GameList(row.Name, games));
                }
            }

            if (lists.Count == 0)
            {
                viewModel.IsDisplayingResults = false;
                return;
            }

            // How many rows a search installed, logged only when the number changes. Cheap
            // enough to leave in - it is silent through a whole query and speaks once when the
            // shape of the results changes, which is the only thing worth knowing after the fact
            // about a screen nobody can attach a debugger to.
            if (lists.Count != lastInstalledRowCount)
            {
                lastInstalledRowCount = lists.Count;
                LogHelper.Log($"Text search installed {lists.Count} row(s): "
                              + string.Join(" | ", lists.Select(list => $"{list.ListTypeValue} ({list.MatchingGames.Count})")));
            }

            viewModel.Navigator.InstallSet(new GameListSet
            {
                ListCategoryType = ListCategoryType.TextSearch,
                GameLists = lists
            });

            viewModel.Navigator.ShowCategory(ListCategoryType.TextSearch);

            // Coming back into the screen puts the user where they left off (RULE-SEARCH-035) -
            // which row as well as where in it, now that a search has more than one. An edit does
            // not: a new result set is a new set of rows, in which the old place means nothing,
            // so it starts at the best match of the primary.
            if (restoreRememberedPlace)
            {
                RestorePlace(viewModel, session, lists);
            }

            viewModel.IsDisplayingResults = true;
        }

        /// <summary>
        /// Puts the user back in the row and column they left, if both still exist.
        ///
        /// Bounds-checked against the rows that were actually installed rather than against the
        /// session's, because a row that projected to nothing was dropped a few lines above and
        /// the indices would not line up. Out of range means the search has changed shape since;
        /// the primary row's best match is the right place to start over.
        /// </summary>
        private static void RestorePlace(View.MainWindowViewModel viewModel,
                                         SearchSession session,
                                         List<GameList> lists)
        {
            int row = session.RememberedRowIndex;
            if (row < 0 || row >= lists.Count)
            {
                return;
            }

            int game = session.RememberedResultIndex;
            if (game <= 0 || game >= lists[row].MatchingGames.Count)
            {
                game = 0;
            }

            if (row == 0 && game == 0)
            {
                return;
            }

            viewModel.Navigator.MoveToPosition(row, game);
        }

        /// <summary>
        /// What the results list is called - the search, said in one line.
        ///
        /// This is not decoration. The search panel fades out while the cursor is down in the box
        /// art row, so that the selected game's clear logo, details, artwork and video have the
        /// screen to themselves; the list heading is then the only thing left saying what the
        /// user searched for. It has to carry the query <em>and</em> the filters, because the
        /// chips have faded with everything else.
        ///
        /// It is also the name position restoration matches a list by, and the name the list
        /// keeps once the user commits and browses on.
        ///
        /// The naming itself lives in SearchRow, below the boundary, because a search will
        /// shortly have more than one row and every one of them needs a name. This is the
        /// primary row of that set - the search the user actually asked for.
        /// </summary>
        private static string DescribeSearch(SearchSession session)
        {
            return new SearchRow(session.Query, session.Filters, true).Name;
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

            // Where they were in the results, so returning to search comes back to it - the row
            // as well as the place in it.
            session.RememberedRowIndex = viewModel.Navigator.CurrentListIndex;
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
        /// Takes the panel off the screen without ending the search.
        ///
        /// For the game detail overlay, which the user may abandon. CloseSearch would forget the
        /// browsing position the user came from, and there would be nothing left to return to
        /// when they backed out of the game - so the search would be over on the strength of a
        /// press the user then took back.
        /// </summary>
        private static void SuspendSearch(EclipseStateContext eclipseStateContext)
        {
            View.MainWindowViewModel viewModel = eclipseStateContext.MainWindowViewModel;
            SearchSession session = viewModel.Search.Session;

            session.RememberedRowIndex = viewModel.Navigator.CurrentListIndex;
            session.RememberedResultIndex = viewModel.Navigator.CurrentList?.CurrentGameIndex ?? 0;

            // Note what is NOT called here: session.Leave(). Leaving parks the cursor on the
            // keyboard, which is right when the search is over - coming back to find the keyboard
            // unfocused would mean typing did nothing. It is wrong for a suspend. The user was
            // standing on a game when they pressed Enter, and that is where backing out of the
            // game should put them; sending them to the keyboard would make Escape cost them
            // their place in the results as well as their look at the game.
            viewModel.IsDisplayingSearch = false;

            // The detail overlay lives inside Grid_DisplayingResults in the markup, so opening it
            // while this is false shows the user a blank screen.
            viewModel.IsDisplayingResults = true;
        }

        /// <summary>
        /// Does what Enter does while browsing, so picking a game out of the results behaves
        /// exactly as picking one out of a row does - including BypassDetails.
        ///
        /// The two branches part company over whether the search is finished. Playing a game
        /// finishes it: the user asked for that game and got it. Opening the details does not -
        /// the overlay can be abandoned with Escape, and abandoning it is the clearest possible
        /// statement that this was not the game they were looking for. So that path suspends the
        /// search and tells the overlay to come back here, and the user lands on the keyboard
        /// with their query, their filters, their rows and their place in them intact.
        /// </summary>
        private void OpenSelectedGame(EclipseStateContext eclipseStateContext)
        {
            View.MainWindowViewModel viewModel = eclipseStateContext.MainWindowViewModel;

            if (EclipseSettingsDataProvider.Instance.EclipseSettings.BypassDetails)
            {
                CloseSearch(eclipseStateContext, keepResults: true);
                eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
                viewModel.PlayCurrentGame();
                return;
            }

            SuspendSearch(eclipseStateContext);
            GameDetailOptionsState.OpenReturningTo(eclipseStateContext, typeof(TextSearchState));
        }

        private static SearchSession Session(EclipseStateContext eclipseStateContext)
        {
            return eclipseStateContext.MainWindowViewModel.Search.Session;
        }
    }
}
