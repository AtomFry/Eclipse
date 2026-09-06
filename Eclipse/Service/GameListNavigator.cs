using Eclipse.Helpers;
using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eclipse.Service
{
    // Where the user is: which set of lists they are browsing, which list within it, and which
    // game within that. The only thing that changes any of those.
    //
    // This used to be spread across the view model, the states and the key strategies, and the
    // part that told the rest of the screen to catch up was a separate call each caller had to
    // remember. Some operations made it and some left it to their caller, with nothing to say
    // which - moving left out of the options pane, for instance, moved the selection and never
    // said so, and the screen kept up only because the transition back to browsing happened to
    // say so on the way in.
    //
    // Now every public move ends by raising SelectionChanged. Moving and announcing the move are
    // the same call, so a new operation cannot forget.
    public sealed class GameListNavigator
    {
        private static readonly Random random = new Random();

        // Every set there is to browse - by platform, by genre, and so on - plus the ones built
        // from results rather than from the catalog: voice search and more like this.
        private readonly List<GameListSet> gameListSets = new List<GameListSet>();

        // Which list within the current set is showing, and the one after it. Two slots: the row
        // the user is on and the row below it.
        private ListCycle<GameList> listCycle;

        /// <summary>Raised once at the end of every move.</summary>
        public event EventHandler SelectionChanged;

        /// <summary>
        /// Raised when there is nothing left to navigate - the set that was switched to has no
        /// lists in it. The view decides what to show; this only reports it.
        /// </summary>
        public event EventHandler NavigationFailed;

        public GameListSet CurrentSet { get; private set; }
        public GameList CurrentList { get; private set; }
        public GameList NextList { get; private set; }

        /// <summary>
        /// Whether the row showing is the first list in its set. The featured game screen opens
        /// from there and nowhere else, and asking this used to mean a state class reaching into
        /// the list cycle and comparing an index.
        /// </summary>
        public bool IsOnFirstList => listCycle?.GetIndexValue(0) == 0;

        /// <summary>Which list of the set is showing.</summary>
        public int CurrentListIndex => listCycle?.GetIndexValue(0) ?? 0;

        /// <summary>
        /// Whether the row showing is the last in its set - so a caller that walks the rows knows
        /// where the walk ends rather than wrapping past it. Text search needs this: Down moves
        /// through the search's rows and only leaves the results at the bottom.
        /// </summary>
        public bool IsOnLastList =>
            CurrentSet?.GameLists == null || CurrentListIndex >= CurrentSet.GameLists.Count - 1;

        /// <summary>Every set, for the callers that build a new one out of the existing lists.</summary>
        public IReadOnlyList<GameListSet> Sets => gameListSets;

        #region what there is to browse

        /// <summary>
        /// Puts a set of lists in place, replacing whatever set of the same category was there.
        ///
        /// Three places used to write this out longhand - building the category sets, voice
        /// search, and more like this - each removing by category and adding, in three different
        /// files. Replacing rather than clearing matters: a rebuild triggered by favouriting a
        /// game must not throw away the voice search results or the more-like-this set.
        /// </summary>
        public void InstallSet(GameListSet gameListSet)
        {
            gameListSets.RemoveAll(set => set.ListCategoryType == gameListSet.ListCategoryType);
            gameListSets.Add(gameListSet);
        }

        /// <summary>Switches to browsing by a category - by genre instead of by platform.</summary>
        public void ShowCategory(ListCategoryType listCategoryType)
        {
            GameListSet requestedListSet = gameListSets
                .FirstOrDefault(gameListSet => gameListSet.ListCategoryType == listCategoryType);

            // Switching to a set with no lists leaves nothing to navigate: the list cycle
            // ends up empty while CurrentList still points at the old list, so moving
            // left and right appears to work but moving between lists reports a failure.
            // Keep the lists we already have instead.
            if (requestedListSet?.GameLists == null || requestedListSet.GameLists.Count == 0)
            {
                return;
            }

            CurrentSet = requestedListSet;
            listCycle = new ListCycle<GameList>(CurrentSet.GameLists, 2);
            RefreshSelection();
        }

        #endregion

        #region moving

        public void MoveToNextGame()
        {
            CurrentList?.CycleForward();
            RaiseSelectionChanged();
        }

        public void MoveToPreviousGame()
        {
            CurrentList?.CycleBackward();
            RaiseSelectionChanged();
        }

        public void MoveToNextList()
        {
            listCycle.CycleForward();
            RefreshSelection();
        }

        public void MoveToPreviousList()
        {
            listCycle.CycleBackward();
            RefreshSelection();
        }

        /// <summary>
        /// Jumps to a row by position, keeping that row's own selected game.
        ///
        /// Unlike MoveToPosition, which is for restoring a remembered place, this is for a caller
        /// walking the set as a block and needing to land at one end of it.
        /// </summary>
        public void MoveToList(int listIndex)
        {
            if (CurrentSet?.GameLists == null
                || listIndex < 0 || listIndex >= CurrentSet.GameLists.Count)
            {
                return;
            }

            listCycle.SetCurrentIndex(listIndex);
            RefreshSelection();
        }

        public void MoveToFirstList()
        {
            MoveToList(0);
        }

        public void MoveToLastList()
        {
            MoveToList((CurrentSet?.GameLists?.Count ?? 0) - 1);
        }

        public void PageForward()
        {
            PageSelection(1);
        }

        public void PageBackward()
        {
            PageSelection(-1);
        }

        /// <summary>
        /// Jumps to a game chosen at random from the whole set, so a game in a large list is
        /// more likely than one in a small list (RULE-BROWSE-011).
        ///
        /// The weighting used to be done by giving every list a precomputed start index across
        /// the set and picking a number in that space. Counting the lists off as we walk them
        /// gives the same distribution without any stored position to keep in step - and
        /// without writing derived state onto lists that other sets share.
        /// </summary>
        public void MoveToRandomGame()
        {
            int gamesToSkip = random.Next(0, CurrentSet.TotalGameCount);

            for (int listIndex = 0; listIndex < CurrentSet.GameLists.Count; listIndex++)
            {
                GameList gameList = CurrentSet.GameLists[listIndex];

                if (gamesToSkip < gameList.MatchCount)
                {
                    MoveToGame(listIndex, gamesToSkip);
                    break;
                }

                gamesToSkip -= gameList.MatchCount;
            }

            RaiseSelectionChanged();
        }

        private void PageSelection(int direction)
        {
            GameList gameList = CurrentList;
            if ((gameList == null) || (gameList.MatchCount == 0))
            {
                return;
            }

            // A page is a fixed number of games, except in a list too short for that, where it
            // is half the list - so the jump is never a no-op and never a full loop
            // (RULE-BROWSE-009).
            int pageAmount = gameList.MatchCount <= EclipseConstants.GamesToPage
                ? gameList.MatchCount / 2
                : EclipseConstants.GamesToPage;

            int gameIndex = (gameList.CurrentGameIndex + (direction * pageAmount) + gameList.MatchCount)
                            % gameList.MatchCount;

            gameList.SetGameIndex(gameIndex);
            RaiseSelectionChanged();
        }

        /// <summary>
        /// Moves to a game in a list, both identified by position - the list within the set, and
        /// the game within the list. Does not announce the move; the callers differ on when they
        /// want that.
        /// </summary>
        /// <summary>
        /// Puts the selection at a known row and column of the current set, and announces it.
        ///
        /// For a caller that remembers a place itself rather than using RememberPosition -
        /// text search, which has to restore inside its own result set on the way back in.
        /// </summary>
        public void MoveToPosition(int listIndex, int gameIndexInList)
        {
            if (CurrentSet?.GameLists == null
                || listIndex < 0 || listIndex >= CurrentSet.GameLists.Count)
            {
                return;
            }

            MoveToGame(listIndex, gameIndexInList);
            RaiseSelectionChanged();
        }

        private void MoveToGame(int listIndex, int gameIndexInList)
        {
            listCycle.SetCurrentIndex(listIndex);

            CurrentList = listCycle.GetItem(0);
            NextList = listCycle.GetItem(1);

            // this warms the current list's row itself, once the window has moved
            CurrentList.SetGameIndex(gameIndexInList);
        }

        private void RefreshSelection()
        {
            if (listCycle?.GenericList?.Count == 0)
            {
                NavigationFailed?.Invoke(this, EventArgs.Empty);
                return;
            }

            CurrentList = listCycle.GetItem(0);
            NextList = listCycle.GetItem(1);

            RaiseSelectionChanged();
        }

        private void RaiseSelectionChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region surviving a rebuild

        private ListCategoryType rememberedSetCategory;
        private string rememberedListValue;
        private string rememberedGameId;
        private int rememberedGameIndex;

        /// <summary>
        /// Notes where the user is, so they can be put back after the lists are rebuilt.
        /// Favouriting a game, rating it or launching it all change what the lists contain.
        /// </summary>
        public void RememberPosition()
        {
            rememberedSetCategory = CurrentSet.ListCategoryType;

            // identifies which list we are in within the set - would be better as a guid. This
            // has to be the list's value and not its description: the description carries the
            // game count when ShowGameCountInList is on, and the count changes on exactly the
            // rebuilds this is trying to survive - so "Favorites (12)" would never be found
            // again once it had become "Favorites (11)".
            rememberedListValue = CurrentList.ListTypeValue;

            rememberedGameId = CurrentList.SelectedGame.Game.Id;
            rememberedGameIndex = CurrentList.CurrentGameIndex;
        }

        /// <summary>
        /// Puts the user back where they were, as nearly as the rebuilt lists allow. Call after
        /// the lists have been rebuilt.
        /// </summary>
        public void RestorePosition()
        {
            ShowCategory(rememberedSetCategory);

            // By value alone. This used to also compare a per-list category, which no code ever
            // assigned - so it was a constant against itself and always passed. See the note in
            // GameList where it lived (B-35a).
            int listIndex = CurrentSet.GameLists.FindIndex(
                list => list.ListTypeValue == rememberedListValue);

            if (listIndex < 0)
            {
                // the list was not there so pick any random game
                MoveToRandomGame();
                return;
            }

            int gameIndex = RestoredGameIndex(CurrentSet.GameLists[listIndex]);

            if (gameIndex < 0)
            {
                MoveToRandomGame();
                return;
            }

            MoveToGame(listIndex, gameIndex);
            RaiseSelectionChanged();
        }

        /// <summary>
        /// Where to land in a list that has just been rebuilt, as an index within that list
        /// (RULE-BROWSE-010): the same game if it is still there, then whatever has taken its
        /// place, then the game before that, then the top of the list. -1 if the list has
        /// nothing to land on at all.
        ///
        /// This used to be expressed as positions across the whole set, so restoring a position
        /// meant adding the list's start index and then searching every list in the set to find
        /// the list it had started from.
        /// </summary>
        private int RestoredGameIndex(GameList gameList)
        {
            List<GameMatch> games = gameList?.MatchingGames;
            if (games == null)
            {
                return -1;
            }

            // the same game, still in the same list
            int sameGame = games.FindIndex(match => match.Game.Id == rememberedGameId);
            if (sameGame >= 0)
            {
                return sameGame;
            }

            // it has gone - take whatever has taken its place, then the one before that, then
            // the first in the list. The middle case is only reachable when the list is exactly
            // as long as the old index, so the index it returns is always inside the list; the
            // set-wide form of this arithmetic could land in the neighbouring list instead.
            if (games.Count > rememberedGameIndex)
            {
                return rememberedGameIndex;
            }

            if (games.Count > rememberedGameIndex - 1 && rememberedGameIndex - 1 >= 0)
            {
                return rememberedGameIndex - 1;
            }

            if (games.Count > 0)
            {
                return 0;
            }

            return -1;
        }

        #endregion
    }
}
