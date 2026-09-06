using System.Collections.Generic;
using System.Linq;
using Eclipse.Models;
using Eclipse.Service;
using Eclipse.Tests.Fakes;
using Xunit;

namespace Eclipse.Tests
{
    /// <summary>
    /// VER-BROWSE-005 - where the user lands after the lists are rebuilt. RULE-BROWSE-010.
    ///
    /// Ranked first in VERIFICATION.md's list of what most needs covering, and uncovered until
    /// now: "Four-deep fallback, entirely undocumented outside the code, user-visible every time
    /// they favourite something, and `B-14` rewrites it." It was blocked on being able to build a
    /// game without Big Box, which the fake now allows.
    ///
    /// Characterization. Every assertion here is what the code does today, so that `B-14` has
    /// something to preserve rather than a description of it.
    ///
    /// THE FALLBACK, in the order RestoredGameIndex tries it:
    ///   1. the same game, wherever it has moved to in the list
    ///   2. failing that, the same index
    ///   3. failing that, the index before it
    ///   4. failing that, the top of the list
    ///   5. and if the list itself has gone, a random game from anywhere in the set
    /// </summary>
    public class PositionRestorationTests
    {
        private static GameMatch GameNamed(string id)
        {
            return new GameMatch(FakeGame.With(id, "Game " + id), null);
        }

        private static GameListSet SetOf(ListCategoryType category, params (string Name, string[] Games)[] lists)
        {
            return new GameListSet
            {
                ListCategoryType = category,
                GameLists = lists
                    .Select(list => new GameList(list.Name, list.Games.Select(GameNamed).ToList()))
                    .ToList()
            };
        }

        /// <summary>A navigator showing one set, sitting on a known game.</summary>
        private static GameListNavigator Showing(GameListSet set, string listName, string gameId)
        {
            GameListNavigator navigator = new GameListNavigator();
            navigator.InstallSet(set);
            navigator.ShowCategory(set.ListCategoryType);

            while (navigator.CurrentList.ListTypeValue != listName)
            {
                navigator.MoveToNextList();
            }

            while (navigator.CurrentList.SelectedGame.Game.Id != gameId)
            {
                navigator.MoveToNextGame();
            }

            return navigator;
        }

        private static string LandedOn(GameListNavigator navigator)
        {
            return navigator.CurrentList?.SelectedGame?.Game?.Id;
        }

        // ------------------------------------------------------------------
        // 1. The same game, wherever it has moved to.
        // ------------------------------------------------------------------

        [Fact]
        public void The_same_game_in_the_same_list_is_where_the_user_lands()
        {
            GameListNavigator navigator = Showing(
                SetOf(ListCategoryType.Genre, ("Action", new[] { "a", "b", "c" })), "Action", "b");

            navigator.RememberPosition();

            navigator.InstallSet(SetOf(ListCategoryType.Genre, ("Action", new[] { "a", "b", "c" })));
            navigator.RestorePosition();

            Assert.Equal("b", LandedOn(navigator));
        }

        /// <summary>
        /// The point of matching on the game rather than the index: favouriting something above
        /// the user shifts everything down, and they should still be looking at their game.
        /// </summary>
        [Fact]
        public void The_same_game_is_followed_when_the_rebuild_moves_it()
        {
            GameListNavigator navigator = Showing(
                SetOf(ListCategoryType.Genre, ("Action", new[] { "a", "b", "c" })), "Action", "c");

            navigator.RememberPosition();

            // two games arrive above it
            navigator.InstallSet(SetOf(ListCategoryType.Genre, ("Action", new[] { "x", "y", "a", "b", "c" })));
            navigator.RestorePosition();

            Assert.Equal("c", LandedOn(navigator));
        }

        [Fact]
        public void The_right_list_is_found_by_name_rather_than_by_position()
        {
            GameListNavigator navigator = Showing(
                SetOf(ListCategoryType.Genre,
                      ("Action", new[] { "a", "b" }),
                      ("Puzzle", new[] { "p", "q" })), "Puzzle", "q");

            navigator.RememberPosition();

            // a list arrives before it, so its position in the set changes
            navigator.InstallSet(SetOf(ListCategoryType.Genre,
                                       ("Adventure", new[] { "m" }),
                                       ("Action", new[] { "a", "b" }),
                                       ("Puzzle", new[] { "p", "q" })));
            navigator.RestorePosition();

            Assert.Equal("Puzzle", navigator.CurrentList.ListTypeValue);
            Assert.Equal("q", LandedOn(navigator));
        }

        // ------------------------------------------------------------------
        // 2. The same index, when the game has gone.
        // ------------------------------------------------------------------

        /// <summary>
        /// The case this rule exists for: un-favouriting the game you are looking at. It leaves
        /// the list, and the user lands on whatever has taken its place rather than at the top.
        /// </summary>
        [Fact]
        public void Whatever_took_the_games_place_is_where_the_user_lands()
        {
            GameListNavigator navigator = Showing(
                SetOf(ListCategoryType.Genre, ("Favorites", new[] { "a", "b", "c" })), "Favorites", "b");

            navigator.RememberPosition();

            // "b" is un-favourited; "c" moves up into index 1
            navigator.InstallSet(SetOf(ListCategoryType.Genre, ("Favorites", new[] { "a", "c" })));
            navigator.RestorePosition();

            Assert.Equal("c", LandedOn(navigator));
        }

        // ------------------------------------------------------------------
        // 3. The index before it, when the list is now exactly that short.
        // ------------------------------------------------------------------

        /// <summary>
        /// Reachable only when the list is exactly as long as the old index - remove the last
        /// game and there is nothing at that index any more, so the user lands one before it.
        /// </summary>
        [Fact]
        public void Removing_the_last_game_lands_the_user_on_the_one_before_it()
        {
            GameListNavigator navigator = Showing(
                SetOf(ListCategoryType.Genre, ("Favorites", new[] { "a", "b", "c" })), "Favorites", "c");

            navigator.RememberPosition();

            navigator.InstallSet(SetOf(ListCategoryType.Genre, ("Favorites", new[] { "a", "b" })));
            navigator.RestorePosition();

            Assert.Equal("b", LandedOn(navigator));
        }

        // ------------------------------------------------------------------
        // 4. The top of the list.
        // ------------------------------------------------------------------

        [Fact]
        public void A_list_that_has_shrunk_past_both_indices_lands_the_user_at_its_top()
        {
            GameListNavigator navigator = Showing(
                SetOf(ListCategoryType.Genre, ("Favorites", new[] { "a", "b", "c", "d", "e" })), "Favorites", "e");

            navigator.RememberPosition();

            navigator.InstallSet(SetOf(ListCategoryType.Genre, ("Favorites", new[] { "z" })));
            navigator.RestorePosition();

            Assert.Equal("z", LandedOn(navigator));
        }

        // ------------------------------------------------------------------
        // 5. A random game, when the list itself has gone.
        // ------------------------------------------------------------------

        /// <summary>
        /// Un-favouriting the last favourite takes the whole list with it. There is nothing to
        /// restore to, so the user is put somewhere valid rather than nowhere.
        /// </summary>
        [Fact]
        public void A_list_that_has_gone_entirely_lands_the_user_somewhere_valid()
        {
            GameListNavigator navigator = Showing(
                SetOf(ListCategoryType.Genre,
                      ("Favorites", new[] { "a" }),
                      ("Action", new[] { "b", "c" })), "Favorites", "a");

            navigator.RememberPosition();

            navigator.InstallSet(SetOf(ListCategoryType.Genre, ("Action", new[] { "b", "c" })));
            navigator.RestorePosition();

            Assert.Equal("Action", navigator.CurrentList.ListTypeValue);
            Assert.Contains(LandedOn(navigator), new[] { "b", "c" });
        }

        [Fact]
        public void A_renamed_list_is_treated_as_a_list_that_has_gone()
        {
            GameListNavigator navigator = Showing(
                SetOf(ListCategoryType.Genre,
                      ("Action", new[] { "a", "b" }),
                      ("Puzzle", new[] { "p" })), "Action", "b");

            navigator.RememberPosition();

            navigator.InstallSet(SetOf(ListCategoryType.Genre,
                                       ("Action Games", new[] { "a", "b" }),
                                       ("Puzzle", new[] { "p" })));
            navigator.RestorePosition();

            Assert.NotNull(LandedOn(navigator));
        }

        // ------------------------------------------------------------------
        // The set, not just the list.
        // ------------------------------------------------------------------

        /// <summary>
        /// Restoration switches back to the category the user was browsing, not whichever set
        /// happens to be showing when the rebuild finishes.
        /// </summary>
        [Fact]
        public void The_category_the_user_was_browsing_is_restored_too()
        {
            GameListNavigator navigator = new GameListNavigator();

            navigator.InstallSet(SetOf(ListCategoryType.Genre, ("Action", new[] { "a", "b" })));
            navigator.InstallSet(SetOf(ListCategoryType.Platform, ("SNES", new[] { "s", "t" })));

            navigator.ShowCategory(ListCategoryType.Platform);
            while (navigator.CurrentList.SelectedGame.Game.Id != "t")
            {
                navigator.MoveToNextGame();
            }

            navigator.RememberPosition();

            // the user is moved elsewhere before the rebuild finishes
            navigator.ShowCategory(ListCategoryType.Genre);

            navigator.InstallSet(SetOf(ListCategoryType.Platform, ("SNES", new[] { "s", "t" })));
            navigator.RestorePosition();

            Assert.Equal(ListCategoryType.Platform, navigator.CurrentSet.ListCategoryType);
            Assert.Equal("t", LandedOn(navigator));
        }

        /// <summary>
        /// RULE-BROWSE-010 relies on the list's *value* rather than its description, because the
        /// description carries the game count when ShowGameCountInList is on - and the count
        /// changes on exactly the rebuilds this is trying to survive.
        /// </summary>
        [Fact]
        public void A_list_whose_size_changed_is_still_found()
        {
            GameListNavigator navigator = Showing(
                SetOf(ListCategoryType.Genre, ("Favorites", new[] { "a", "b", "c" })), "Favorites", "a");

            navigator.RememberPosition();

            navigator.InstallSet(SetOf(ListCategoryType.Genre, ("Favorites", new[] { "a", "b" })));
            navigator.RestorePosition();

            Assert.Equal("Favorites", navigator.CurrentList.ListTypeValue);
            Assert.Equal("a", LandedOn(navigator));
        }

        // ------------------------------------------------------------------
        // Announcing the move.
        // ------------------------------------------------------------------

        /// <summary>
        /// Every public move ends by saying so - that is the whole reason the navigator exists as
        /// one object. Restoration is no exception.
        /// </summary>
        [Fact]
        public void Restoring_announces_the_move()
        {
            GameListNavigator navigator = Showing(
                SetOf(ListCategoryType.Genre, ("Action", new[] { "a", "b" })), "Action", "b");

            navigator.RememberPosition();
            navigator.InstallSet(SetOf(ListCategoryType.Genre, ("Action", new[] { "a", "b" })));

            int announced = 0;
            navigator.SelectionChanged += (sender, args) => announced++;

            navigator.RestorePosition();

            Assert.True(announced > 0);
        }

        // ------------------------------------------------------------------
        // Jumping to an end of the set - MoveToFirstList / MoveToLastList.
        //
        // Text search walks its rows as a block and has to land at one end of it: leaving the
        // rows downward rewinds them, and wrapping up into them enters at the bottom. Without
        // that the row is left parked wherever the cursor happened to leave it, and the second
        // trip round the loop skips the middle rows entirely (RULE-SEARCH-079).
        // ------------------------------------------------------------------

        private static GameListSet ThreeLists()
        {
            return SetOf(ListCategoryType.TextSearch,
                         ("one", new[] { "a", "b" }),
                         ("two", new[] { "c", "d" }),
                         ("three", new[] { "e", "f" }));
        }

        [Fact]
        public void Moving_to_the_last_list_lands_on_it()
        {
            GameListNavigator navigator = Showing(ThreeLists(), "one", "a");

            navigator.MoveToLastList();

            Assert.Equal(2, navigator.CurrentListIndex);
            Assert.Equal("three", navigator.CurrentList.ListTypeValue);
            Assert.True(navigator.IsOnLastList);
        }

        [Fact]
        public void Moving_to_the_first_list_lands_on_it()
        {
            GameListNavigator navigator = Showing(ThreeLists(), "three", "e");

            navigator.MoveToFirstList();

            Assert.Equal(0, navigator.CurrentListIndex);
            Assert.Equal("one", navigator.CurrentList.ListTypeValue);
            Assert.True(navigator.IsOnFirstList);
        }

        /// <summary>
        /// The row keeps its own selected game. Rewinding the rows is a move between them, not a
        /// reset of what is selected inside them - the user who comes back to a row should find
        /// the game they left on it.
        /// </summary>
        [Fact]
        public void Jumping_between_lists_keeps_each_ones_selected_game()
        {
            GameListNavigator navigator = Showing(ThreeLists(), "two", "d");

            navigator.MoveToFirstList();
            navigator.MoveToList(1);

            Assert.Equal("d", LandedOn(navigator));
        }

        [Fact]
        public void Jumping_outside_the_set_does_nothing()
        {
            GameListNavigator navigator = Showing(ThreeLists(), "two", "c");

            navigator.MoveToList(-1);
            navigator.MoveToList(3);

            Assert.Equal(1, navigator.CurrentListIndex);
        }

        /// <summary>
        /// The whole of the reported bug, at the level this can be tested: after walking to the
        /// last row and rewinding - which is what leaving the rows downward now does - the next
        /// walk starts at the top again rather than at the end it was left at.
        /// </summary>
        [Fact]
        public void Rewinding_lets_the_walk_start_over()
        {
            GameListNavigator navigator = Showing(ThreeLists(), "one", "a");

            while (!navigator.IsOnLastList)
            {
                navigator.MoveToNextList();
            }

            navigator.MoveToFirstList();

            Assert.False(navigator.IsOnLastList);
            Assert.Equal("one", navigator.CurrentList.ListTypeValue);

            navigator.MoveToNextList();
            Assert.Equal("two", navigator.CurrentList.ListTypeValue);
        }
    }
}
