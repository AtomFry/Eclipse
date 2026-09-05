using System.Collections.Generic;
using System.Linq;
using Eclipse.Models;
using Eclipse.Service.Search;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// The search screen's state machine, exercised without a single WPF key event, dispatcher
    /// or Big Box object.
    ///
    /// Covers the transitions Stage 2 names: entering, empty query, typing, deleting, changing
    /// the query, receiving results, selecting a result, committing, leaving, returning, and the
    /// index not being ready.
    /// </summary>
    public class SearchSessionTests
    {
        /// <summary>An index source whose availability the test controls.</summary>
        private sealed class FakeIndexSource : ISearchIndexSource
        {
            public SearchAvailability Availability { get; set; } = SearchAvailability.Ready;
            public ISearchIndex Index { get; set; }
        }

        private static FakeIndexSource ReadySource(params string[] titles)
        {
            List<SearchableGame> games = new List<SearchableGame>();
            for (int index = 0; index < titles.Length; index++)
            {
                games.Add(new SearchableGame(index, titles[index], 0));
            }

            return new FakeIndexSource
            {
                Availability = SearchAvailability.Ready,
                Index = TitleIndex.Build(games)
            };
        }

        private static SearchSession SessionOf(params string[] titles)
        {
            return new SearchSession(ReadySource(titles), SearchKeyboardLayout.Alphabetical);
        }

        /// <summary>Types a whole string through the session, one character at a time.</summary>
        private static void Type(SearchSession session, string text)
        {
            foreach (char character in text)
            {
                session.Append(character);
            }
        }

        private static IReadOnlyList<int> Found(SearchSession session)
        {
            return session.Results.Select(hit => hit.CatalogIndex).ToList();
        }

        // ------------------------------------------------------------------
        // Entering, and the empty query.
        // ------------------------------------------------------------------

        [Fact]
        public void A_new_session_opens_empty_on_the_keyboard()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");

            Assert.Equal(string.Empty, session.Query);
            Assert.False(session.HasQuery);
            Assert.Empty(session.Results);
            Assert.Equal(SearchZone.Keyboard, session.Zone);
            Assert.False(session.IsOnResults);
        }

        /// <summary>
        /// An empty query finds nothing rather than everything. Showing the whole library the
        /// moment the screen opens would be a wall of box art that means nothing.
        /// </summary>
        [Fact]
        public void An_empty_query_produces_no_results()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog", "Super Mario Bros");

            session.Enter();

            Assert.Empty(session.Results);
            Assert.Equal(0, session.ResultCount);
            Assert.False(session.HasResults);
        }

        [Fact]
        public void Entering_raises_a_change_so_the_screen_can_draw_itself()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            int changes = 0;
            session.Changed += (sender, args) => changes++;

            session.Enter();

            Assert.Equal(1, changes);
        }

        // ------------------------------------------------------------------
        // Typing.
        // ------------------------------------------------------------------

        [Fact]
        public void Typing_builds_the_query_and_finds_games()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog", "Super Mario Bros");

            Type(session, "son");

            Assert.Equal("son", session.Query);
            Assert.True(session.HasQuery);
            Assert.Equal(new[] { 0 }, Found(session));
        }

        [Fact]
        public void Every_keystroke_re_runs_the_search()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog", "Sonata", "Super Mario Bros");

            session.Append('s');
            Assert.Equal(3, session.ResultCount);

            session.Append('o');
            Assert.Equal(2, session.ResultCount);

            session.Append('n');
            Assert.Equal(2, session.ResultCount);

            session.Append('i');
            Assert.Equal(1, session.ResultCount);
        }

        [Fact]
        public void Each_keystroke_raises_exactly_one_change()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            int changes = 0;
            session.Changed += (sender, args) => changes++;

            Type(session, "son");

            Assert.Equal(3, changes);
        }

        [Fact]
        public void A_space_separates_terms()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog", "Sonic Adventure");

            Type(session, "sonic hedge");

            Assert.Equal(new[] { 0 }, Found(session));
        }

        /// <summary>
        /// Holding the space key on an empty query is an easy accident on a d-pad, and a leading
        /// space can only produce an empty term.
        /// </summary>
        [Fact]
        public void A_leading_space_is_ignored()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");

            session.Append(' ');

            Assert.Equal(string.Empty, session.Query);
        }

        // ------------------------------------------------------------------
        // Deleting and clearing.
        // ------------------------------------------------------------------

        [Fact]
        public void Backspace_removes_a_character_and_widens_the_results()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog", "Sonata");

            Type(session, "soni");
            Assert.Equal(1, session.ResultCount);

            session.Backspace();
            Assert.Equal("son", session.Query);
            Assert.Equal(2, session.ResultCount);
        }

        [Fact]
        public void Backspace_on_an_empty_query_does_nothing()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            int changes = 0;
            session.Changed += (sender, args) => changes++;

            session.Backspace();

            Assert.Equal(string.Empty, session.Query);
            Assert.Equal(0, changes);
        }

        [Fact]
        public void Clear_empties_the_query_and_the_results()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");

            Type(session, "sonic");
            Assert.NotEmpty(session.Results);

            session.Clear();

            Assert.Equal(string.Empty, session.Query);
            Assert.Empty(session.Results);
        }

        [Fact]
        public void Clear_on_an_empty_query_does_nothing()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            int changes = 0;
            session.Changed += (sender, args) => changes++;

            session.Clear();

            Assert.Equal(0, changes);
        }

        /// <summary>
        /// Rapid query changes - typing a whole word and deleting it again - must leave the
        /// session exactly where it started. Nothing is cached across a keystroke, so nothing
        /// can be left behind.
        /// </summary>
        [Fact]
        public void Typing_and_deleting_a_whole_word_returns_to_the_starting_state()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog", "Super Mario Bros", "Sonata");

            Type(session, "sonic the hedgehog");
            for (int press = 0; press < "sonic the hedgehog".Length; press++)
            {
                session.Backspace();
            }

            Assert.Equal(string.Empty, session.Query);
            Assert.Empty(session.Results);
            Assert.Equal(SearchZone.Keyboard, session.Zone);
            Assert.Equal(0, session.RememberedResultIndex);
        }

        /// <summary>
        /// However the query got to its current text - typed, deleted, cleared and retyped - the
        /// results must be exactly what the engine says about that text. Nothing is cached
        /// across a keystroke, and this is the assertion that keeps it that way.
        /// </summary>
        [Fact]
        public void Rapid_query_changes_always_agree_with_the_engine()
        {
            FakeIndexSource source = ReadySource("Sonic the Hedgehog", "Sonata", "Super Mario Bros", "Streets of Rage");
            SearchSession session = new SearchSession(source, SearchKeyboardLayout.Alphabetical);

            foreach (string buffer in new[] { "s", "so", "son", "so", "s", "st", "s", "sonic", "", "sonic hedge" })
            {
                session.Clear();
                Type(session, buffer);

                IReadOnlyList<SearchHit> expected =
                    SearchEngine.Search(SearchQuery.Parse(buffer), source.Index, SearchSession.LiveResultLimit);

                Assert.Equal(buffer, session.Query);
                Assert.Equal(expected.Select(hit => hit.CatalogIndex), Found(session));
            }
        }

        // ------------------------------------------------------------------
        // The keyboard, driven through the session.
        // ------------------------------------------------------------------

        [Fact]
        public void Pressing_a_character_key_types_it()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");

            session.PressSelectedKey();

            Assert.Equal("a", session.Query);
        }

        [Fact]
        public void Pressing_the_backspace_key_deletes()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            Type(session, "son");

            while (session.Keyboard.SelectedKey.Kind != SearchKeyKind.Backspace)
            {
                session.MoveKeyboardRight();
                if (session.Keyboard.SelectedKey.Column == 0)
                {
                    session.MoveDown(false);
                }
            }

            session.PressSelectedKey();

            Assert.Equal("so", session.Query);
        }

        [Fact]
        public void Pressing_the_space_key_types_a_space()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            Type(session, "sonic");

            while (session.Keyboard.SelectedKey.Kind != SearchKeyKind.Space)
            {
                session.MoveDown(false);
            }

            session.PressSelectedKey();

            Assert.Equal("sonic ", session.Query);
        }

        // ------------------------------------------------------------------
        // Zones.
        // ------------------------------------------------------------------

        [Fact]
        public void With_no_results_there_is_nowhere_to_go_but_the_keyboard()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");

            session.MoveUp(false);
            Assert.Equal(SearchZone.Keyboard, session.Zone);

            for (int press = 0; press < 10; press++)
            {
                session.MoveDown(false);
            }

            Assert.Equal(SearchZone.Keyboard, session.Zone);
        }

        [Fact]
        public void Moving_off_the_top_of_the_keyboard_reaches_the_results()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            Type(session, "son");

            session.MoveUp(false);

            Assert.Equal(SearchZone.Results, session.Zone);
        }

        [Fact]
        public void Moving_off_the_bottom_of_the_keyboard_reaches_the_results()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            Type(session, "son");

            while (!session.Keyboard.IsOnLastRow)
            {
                session.MoveDown(false);
            }
            session.MoveDown(false);

            Assert.Equal(SearchZone.Results, session.Zone);
        }

        /// <summary>
        /// RULE-SEARCH-037 - a user travelling across the key grid with the direction held must
        /// not shoot out of it into the results.
        /// </summary>
        [Fact]
        public void Held_movement_never_leaves_the_keyboard()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            Type(session, "son");

            for (int press = 0; press < 20; press++)
            {
                session.MoveDown(true);
                Assert.Equal(SearchZone.Keyboard, session.Zone);
            }

            for (int press = 0; press < 20; press++)
            {
                session.MoveUp(true);
                Assert.Equal(SearchZone.Keyboard, session.Zone);
            }
        }

        [Fact]
        public void Held_movement_wraps_within_the_keyboard()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            Type(session, "son");

            session.MoveUp(true);

            Assert.Equal(SearchZone.Keyboard, session.Zone);
            Assert.True(session.Keyboard.IsOnLastRow);
        }

        [Fact]
        public void Leaving_the_results_returns_to_the_keyboard()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            Type(session, "son");
            session.MoveUp(false);
            Assert.Equal(SearchZone.Results, session.Zone);

            session.MoveDown(false);

            Assert.Equal(SearchZone.Keyboard, session.Zone);
        }

        /// <summary>
        /// RULE-SEARCH-032 - a zone with no content is never focused. Deleting the query while
        /// standing in the results must not strand the cursor in a zone that has just vanished.
        /// </summary>
        [Fact]
        public void Emptying_the_results_moves_the_cursor_out_of_them()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            Type(session, "son");
            session.MoveUp(false);
            Assert.Equal(SearchZone.Results, session.Zone);

            session.Clear();

            Assert.Equal(SearchZone.Keyboard, session.Zone);
        }

        [Fact]
        public void Typing_a_term_that_matches_nothing_moves_the_cursor_out_of_the_results()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");
            Type(session, "son");
            session.MoveUp(false);

            Type(session, "zzz");

            Assert.Empty(session.Results);
            Assert.Equal(SearchZone.Keyboard, session.Zone);
        }
        // ------------------------------------------------------------------
        // Remembering the place in the results.
        //
        // Which result is selected is NOT owned here - the results are installed as an ordinary
        // GameList and the navigator moves through them, exactly as it does for every other list
        // in Eclipse. What the session keeps is only where the user was when they last left the
        // screen, so returning can put them back (RULE-SEARCH-035). It never interprets it.
        // ------------------------------------------------------------------

        [Fact]
        public void A_new_session_remembers_no_place_in_the_results()
        {
            Assert.Equal(0, SessionOf("Sonic the Hedgehog").RememberedResultIndex);
        }

        [Fact]
        public void The_remembered_place_survives_leaving_and_returning()
        {
            SearchSession session = SessionOf("Sonic A", "Sonic B", "Sonic C");
            Type(session, "sonic");

            // whoever owns the row writes the user's place here on the way out
            session.RememberedResultIndex = 2;

            session.Leave();
            session.Enter();

            Assert.Equal(2, session.RememberedResultIndex);
        }

        /// <summary>
        /// An edit produces a different result set, in which the old place means nothing - so it
        /// goes back to the best match, the one position that always means the same thing.
        /// </summary>
        [Theory]
        [InlineData("append")]
        [InlineData("backspace")]
        [InlineData("clear")]
        public void Editing_the_query_forgets_the_place_in_the_results(string edit)
        {
            SearchSession session = SessionOf("Sonic A", "Sonic B", "Sonic C");
            Type(session, "sonic");
            session.RememberedResultIndex = 2;

            switch (edit)
            {
                case "append":
                    session.Append('x');
                    break;
                case "backspace":
                    session.Backspace();
                    break;
                case "clear":
                    session.Clear();
                    break;
            }

            Assert.Equal(0, session.RememberedResultIndex);
        }

        [Fact]
        public void Moving_the_cursor_does_not_forget_the_place_in_the_results()
        {
            SearchSession session = SessionOf("Sonic A", "Sonic B", "Sonic C");
            Type(session, "sonic");
            session.RememberedResultIndex = 2;

            session.MoveKeyboardRight();
            session.MoveUp(false);
            session.MoveDown(false);

            Assert.Equal(2, session.RememberedResultIndex);
        }

        // ------------------------------------------------------------------
        // Committing.
        // ------------------------------------------------------------------

        /// <summary>
        /// The live row is capped; committing wants everything, because the whole ranked set
        /// becomes a list the user browses.
        /// </summary>
        [Fact]
        public void Committing_returns_the_whole_ranked_set_not_the_capped_row()
        {
            List<SearchableGame> games = new List<SearchableGame>();
            for (int index = 0; index < SearchSession.LiveResultLimit + 25; index++)
            {
                games.Add(new SearchableGame(index, "Sonic Game " + index, 0));
            }

            SearchSession session = new SearchSession(
                new FakeIndexSource { Index = TitleIndex.Build(games) },
                SearchKeyboardLayout.Alphabetical);

            Type(session, "sonic");

            Assert.Equal(SearchSession.LiveResultLimit, session.ResultCount);
            Assert.Equal(games.Count, session.AllResults().Count);
        }

        [Fact]
        public void The_committed_set_is_in_the_same_order_as_the_live_row()
        {
            SearchSession session = SessionOf("Sonic Adventure Collection Special", "Sonic", "Sonic Spinball");
            Type(session, "sonic");

            IReadOnlyList<SearchHit> all = session.AllResults();

            for (int index = 0; index < session.ResultCount; index++)
            {
                Assert.Equal(session.Results[index].CatalogIndex, all[index].CatalogIndex);
            }
        }

        [Fact]
        public void Committing_an_empty_query_returns_nothing()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog");

            Assert.Empty(session.AllResults());
        }

        // ------------------------------------------------------------------
        // Leaving and returning - RULE-SEARCH-035.
        // ------------------------------------------------------------------

        [Fact]
        public void Leaving_and_returning_keeps_the_query_and_the_results()
        {
            SearchSession session = SessionOf("Sonic the Hedgehog", "Sonic Adventure");
            Type(session, "sonic");
            int before = session.ResultCount;

            session.Leave();
            session.Enter();

            Assert.Equal("sonic", session.Query);
            Assert.Equal(before, session.ResultCount);
        }

        /// <summary>
        /// Leaving puts the cursor back on the keyboard, because the results row belongs to the
        /// browsing surface once search is closed - returning to find the keyboard unfocused
        /// would mean typing did nothing. Where the user was *in* the results is kept separately,
        /// in RememberedResultIndex.
        /// </summary>
        [Fact]
        public void Leaving_returns_the_cursor_to_the_keyboard()
        {
            SearchSession session = SessionOf("Sonic A", "Sonic B", "Sonic C");
            Type(session, "sonic");
            session.MoveUp(false);
            Assert.Equal(SearchZone.Results, session.Zone);

            session.Leave();
            session.Enter();

            Assert.Equal(SearchZone.Keyboard, session.Zone);
        }

        // ------------------------------------------------------------------
        // The index not being ready.
        // ------------------------------------------------------------------

        [Fact]
        public void Typing_before_the_index_is_ready_finds_nothing_and_does_not_throw()
        {
            SearchSession session = new SearchSession(
                new FakeIndexSource { Availability = SearchAvailability.Preparing, Index = null },
                SearchKeyboardLayout.Alphabetical);

            Type(session, "sonic");

            Assert.Equal("sonic", session.Query);
            Assert.Empty(session.Results);
            Assert.False(session.IsReady);
            Assert.Equal(SearchAvailability.Preparing, session.Availability);
        }

        /// <summary>
        /// The reason Enter re-runs rather than trusting what it left behind: a user who searched
        /// during startup and came back should find their query answered.
        /// </summary>
        [Fact]
        public void A_query_typed_before_the_index_was_ready_is_answered_on_returning()
        {
            FakeIndexSource source = new FakeIndexSource
            {
                Availability = SearchAvailability.Preparing,
                Index = null
            };

            SearchSession session = new SearchSession(source, SearchKeyboardLayout.Alphabetical);
            Type(session, "sonic");
            Assert.Empty(session.Results);

            source.Index = ReadySource("Sonic the Hedgehog").Index;
            source.Availability = SearchAvailability.Ready;

            session.Leave();
            session.Enter();

            Assert.Single(session.Results);
        }

        [Fact]
        public void A_failed_index_behaves_like_one_that_is_not_there()
        {
            SearchSession session = new SearchSession(
                new FakeIndexSource { Availability = SearchAvailability.Failed, Index = null },
                SearchKeyboardLayout.Alphabetical);

            Type(session, "sonic");

            Assert.Empty(session.Results);
            Assert.Equal(SearchAvailability.Failed, session.Availability);
        }

        [Fact]
        public void A_session_with_no_index_source_at_all_does_not_throw()
        {
            SearchSession session = new SearchSession(null, SearchKeyboardLayout.Alphabetical);

            Type(session, "sonic");

            Assert.Empty(session.Results);
            Assert.Equal(SearchAvailability.Disabled, session.Availability);
            Assert.Empty(session.AllResults());
        }

        [Fact]
        public void The_keyboard_layout_comes_from_the_session_it_was_built_with()
        {
            SearchSession alphabetical = new SearchSession(ReadySource("Sonic"), SearchKeyboardLayout.Alphabetical);
            SearchSession qwerty = new SearchSession(ReadySource("Sonic"), SearchKeyboardLayout.Qwerty);

            Assert.Equal('a', alphabetical.Keyboard.SelectedKey.Character);
            Assert.Equal('1', qwerty.Keyboard.SelectedKey.Character);
        }
    }
}
