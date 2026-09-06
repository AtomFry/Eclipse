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
            public FacetIndex Facets { get; set; } = FacetIndex.Empty;
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
                session.MoveRight(false);
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

            session.MoveRight(false);
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

        // ------------------------------------------------------------------
        // Stage 4c - suggestions and filters.
        // ------------------------------------------------------------------

        /// <summary>
        ///   0  Sonic the Hedgehog   Sega Genesis   Sonic (series)   Sonic Team
        ///   1  Sonic Adventure      Dreamcast      Sonic (series)   Sonic Team
        ///   2  Streets of Rage      Sega Genesis
        /// </summary>
        private static FakeIndexSource SonicSource()
        {
            FakeIndexSource source = ReadySource("Sonic the Hedgehog", "Sonic Adventure", "Streets of Rage");

            source.Facets = FacetIndex.Build(new[]
            {
                new GameFacets(0, new[]
                {
                    new FacetValue(ListCategoryType.Platform, "Sega Genesis"),
                    new FacetValue(ListCategoryType.Series, "Sonic"),
                    new FacetValue(ListCategoryType.Developer, "Sonic Team")
                }),
                new GameFacets(1, new[]
                {
                    new FacetValue(ListCategoryType.Platform, "Dreamcast"),
                    new FacetValue(ListCategoryType.Series, "Sonic"),
                    new FacetValue(ListCategoryType.Developer, "Sonic Team")
                }),
                new GameFacets(2, new[]
                {
                    new FacetValue(ListCategoryType.Platform, "Sega Genesis")
                })
            });

            return source;
        }

        private static SearchSession SonicSession()
        {
            return new SearchSession(SonicSource(), SearchKeyboardLayout.Alphabetical);
        }

        /// <summary>
        /// Walks the cursor into the suggestion list.
        ///
        /// Right only crosses out of the keyboard from the end of a row (RULE-SEARCH-030) -
        /// otherwise there would be no way to traverse the key grid rightward at all - so
        /// reaching the suggestions means walking to the row's end first. Bounded, so a change
        /// that makes the zone unreachable fails the test rather than hanging it.
        /// </summary>
        private static void EnterSuggestions(SearchSession session)
        {
            for (int guard = 0; guard < 50 && session.Zone != SearchZone.Suggestions; guard++)
            {
                session.MoveRight(false);
            }

            Assert.Equal(SearchZone.Suggestions, session.Zone);
        }

        [Fact]
        public void A_new_session_has_no_filters_and_no_suggestions()
        {
            SearchSession session = SonicSession();

            Assert.Empty(session.Filters);
            Assert.False(session.HasFilters);
            Assert.Empty(session.Suggestions);
            Assert.Null(session.SelectedSuggestion);
        }

        /// <summary>
        /// RULE-SEARCH-059's display threshold. The ranker answers a one-character query happily;
        /// whether the session *shows* it is a separate decision, currently on trial at one
        /// character.
        ///
        /// Written against the constant rather than a number, so moving the threshold is a
        /// one-line change that does not drag a test with it.
        /// </summary>
        [Fact]
        public void Suggestions_appear_at_the_threshold_and_not_before()
        {
            SearchSession session = SonicSession();
            const string typed = "son";

            for (int index = 0; index < typed.Length; index++)
            {
                session.Append(typed[index]);

                bool pastTheThreshold = index + 1 >= SearchSession.SuggestFromCharacters;
                Assert.Equal(pastTheThreshold, session.HasSuggestions);
            }
        }

        [Fact]
        public void Suggestions_offer_the_metadata_terms_matching_what_was_typed()
        {
            SearchSession session = SonicSession();

            Type(session, "son");

            Assert.Contains(session.Suggestions, s => s.Value == "Sonic" && s.Facet == ListCategoryType.Series);
            Assert.Contains(session.Suggestions, s => s.Value == "Sonic Team" && s.Facet == ListCategoryType.Developer);
        }

        [Fact]
        public void Deleting_back_below_the_threshold_takes_the_suggestions_away()
        {
            SearchSession session = SonicSession();

            Type(session, "son");
            Assert.NotEmpty(session.Suggestions);

            while (session.Query.Length >= SearchSession.SuggestFromCharacters)
            {
                session.Backspace();
            }

            Assert.Empty(session.Suggestions);
        }

        // ------------------------------------------------------------------
        // Applying a filter - RULE-SEARCH-060, 061, 062.
        // ------------------------------------------------------------------

        /// <summary>
        /// The invariant: the query buffer is scratch space used to discover a metadata term, and
        /// selecting one consumes it. That is what makes the requested flow work - type "spo",
        /// pick Sports, type "fo", pick Football.
        /// </summary>
        [Fact]
        public void Selecting_a_suggestion_applies_it_and_consumes_the_query()
        {
            SearchSession session = SonicSession();
            Type(session, "son");
            EnterSuggestions(session);

            // Both match as a prefix with the same count, so facet order decides and the series
            // leads the developer.
            Assert.Equal("Sonic", session.SelectedSuggestion.Value);

            session.ApplySelectedSuggestion();

            Assert.Equal(string.Empty, session.Query);
            Assert.Single(session.Filters);
            Assert.Equal(new SearchFilter(ListCategoryType.Series, "Sonic"), session.Filters[0]);
        }

        /// <summary>RULE-SEARCH-062 - the list the cursor stood in has just been rebuilt away.</summary>
        [Fact]
        public void Selecting_a_suggestion_returns_the_cursor_to_the_keyboard()
        {
            SearchSession session = SonicSession();
            Type(session, "son");
            EnterSuggestions(session);

            session.ApplySelectedSuggestion();

            Assert.Equal(SearchZone.Keyboard, session.Zone);
        }

        /// <summary>
        /// With the query consumed, the filter is the whole search - so the results are the games
        /// it leaves rather than nothing.
        /// </summary>
        [Fact]
        public void A_filter_with_no_query_produces_the_games_it_leaves()
        {
            SearchSession session = SonicSession();

            session.ToggleFilter(new SearchFilter(ListCategoryType.Series, "Sonic"));

            Assert.Equal(string.Empty, session.Query);
            Assert.Equal(new[] { 0, 1 }, Found(session));
        }

        [Fact]
        public void A_filter_and_a_query_narrow_together()
        {
            SearchSession session = SonicSession();

            session.ToggleFilter(new SearchFilter(ListCategoryType.Platform, "Sega Genesis"));
            Assert.Equal(new[] { 0, 2 }, Found(session));

            Type(session, "sonic");
            Assert.Equal(new[] { 0 }, Found(session));
        }

        [Fact]
        public void A_query_matching_nothing_inside_a_filter_finds_nothing()
        {
            SearchSession session = SonicSession();

            session.ToggleFilter(new SearchFilter(ListCategoryType.Platform, "Dreamcast"));
            Type(session, "streets");

            Assert.Empty(session.Results);
        }

        /// <summary>RULE-SEARCH-061 - selecting an applied term removes it.</summary>
        [Fact]
        public void Selecting_an_applied_filter_removes_it()
        {
            SearchSession session = SonicSession();
            SearchFilter sonic = new SearchFilter(ListCategoryType.Series, "Sonic");

            session.ToggleFilter(sonic);
            Assert.Single(session.Filters);

            session.ToggleFilter(sonic);
            Assert.Empty(session.Filters);
            Assert.Empty(session.Results);
        }

        [Fact]
        public void The_same_filter_cannot_be_applied_twice()
        {
            SearchSession session = SonicSession();

            session.ToggleFilter(new SearchFilter(ListCategoryType.Series, "Sonic"));
            session.ToggleFilter(new SearchFilter(ListCategoryType.Series, "SONIC"));

            Assert.Empty(session.Filters);
        }

        [Fact]
        public void Filters_are_kept_in_the_order_they_were_added()
        {
            SearchSession session = SonicSession();

            session.ToggleFilter(new SearchFilter(ListCategoryType.Platform, "Sega Genesis"));
            session.ToggleFilter(new SearchFilter(ListCategoryType.Series, "Sonic"));

            Assert.Equal(ListCategoryType.Platform, session.Filters[0].Facet);
            Assert.Equal(ListCategoryType.Series, session.Filters[1].Facet);
        }

        [Fact]
        public void Clearing_the_filters_widens_back_to_nothing()
        {
            SearchSession session = SonicSession();

            session.ToggleFilter(new SearchFilter(ListCategoryType.Series, "Sonic"));
            Assert.NotEmpty(session.Results);

            session.ClearFilters();

            Assert.Empty(session.Filters);
            Assert.Empty(session.Results);
        }

        [Fact]
        public void Clearing_the_filters_leaves_the_query_alone()
        {
            SearchSession session = SonicSession();

            session.ToggleFilter(new SearchFilter(ListCategoryType.Platform, "Sega Genesis"));
            Type(session, "sonic");

            session.ClearFilters();

            Assert.Equal("sonic", session.Query);

            // Both Sonic games, back without the platform narrowing them. Ordered by the ranking
            // rather than by catalog position, so compared as a set.
            Assert.Equal(new[] { 0, 1 }, Found(session).OrderBy(game => game));
        }

        // ------------------------------------------------------------------
        // The suggestion zone.
        // ------------------------------------------------------------------

        /// <summary>
        /// RULE-SEARCH-030 - the suggestion list sits beside the keyboard, so the rightmost key
        /// steps into it rather than wrapping back round the row.
        /// </summary>
        [Fact]
        public void Right_from_the_end_of_a_keyboard_row_reaches_the_suggestions()
        {
            SearchSession session = SonicSession();
            Type(session, "son");

            while (!session.Keyboard.IsAtEndOfRow)
            {
                session.MoveRight(false);
            }
            session.MoveRight(false);

            Assert.Equal(SearchZone.Suggestions, session.Zone);
        }

        /// <summary>RULE-SEARCH-037 - holding still wraps, so fast travel cannot leave the grid.</summary>
        [Fact]
        public void Held_right_wraps_within_the_keyboard_rather_than_reaching_the_suggestions()
        {
            SearchSession session = SonicSession();
            Type(session, "son");

            for (int press = 0; press < 20; press++)
            {
                session.MoveRight(true);
                Assert.Equal(SearchZone.Keyboard, session.Zone);
            }
        }

        [Fact]
        public void Right_does_not_leave_the_keyboard_when_nothing_is_suggested()
        {
            SearchSession session = SonicSession();

            for (int press = 0; press < 20; press++)
            {
                session.MoveRight(false);
                Assert.Equal(SearchZone.Keyboard, session.Zone);
            }
        }

        [Fact]
        public void Left_from_the_suggestions_returns_to_the_keyboard()
        {
            SearchSession session = SonicSession();
            Type(session, "son");
            EnterSuggestions(session);

            session.MoveLeft(false);

            Assert.Equal(SearchZone.Keyboard, session.Zone);
        }

        [Fact]
        public void Up_and_down_move_through_the_suggestion_list()
        {
            SearchSession session = SonicSession();
            Type(session, "son");
            EnterSuggestions(session);
            Assert.True(session.Suggestions.Count > 1);

            Assert.Equal(0, session.SelectedSuggestionIndex);

            session.MoveDown(false);
            Assert.Equal(1, session.SelectedSuggestionIndex);

            session.MoveUp(false);
            Assert.Equal(0, session.SelectedSuggestionIndex);
        }

        [Fact]
        public void The_suggestion_selection_wraps_upward()
        {
            SearchSession session = SonicSession();
            Type(session, "son");
            EnterSuggestions(session);

            session.MoveUp(false);

            Assert.Equal(session.Suggestions.Count - 1, session.SelectedSuggestionIndex);
        }

        [Fact]
        public void Down_from_the_last_suggestion_reaches_the_results()
        {
            SearchSession session = SonicSession();
            Type(session, "son");
            EnterSuggestions(session);

            while (session.SelectedSuggestionIndex < session.Suggestions.Count - 1)
            {
                session.MoveDown(false);
            }
            session.MoveDown(false);

            Assert.Equal(SearchZone.Results, session.Zone);
        }

        /// <summary>
        /// RULE-SEARCH-032 - a zone with no content never holds the cursor. Typing on past the
        /// point where anything is suggested must not strand the user in a list that has gone.
        /// </summary>
        [Fact]
        public void Typing_the_suggestions_away_moves_the_cursor_out_of_them()
        {
            SearchSession session = SonicSession();
            Type(session, "son");
            EnterSuggestions(session);

            Type(session, "zzz");

            Assert.Empty(session.Suggestions);
            Assert.Equal(SearchZone.Keyboard, session.Zone);
        }

        /// <summary>
        /// RULE-SEARCH-054 through the session: what a suggestion promises is what selecting it
        /// delivers, because counts are taken against the filters already applied.
        /// </summary>
        [Fact]
        public void Suggestion_counts_narrow_as_filters_are_applied()
        {
            SearchSession session = SonicSession();

            Type(session, "sega");
            Assert.Equal(2, session.Suggestions.Single(s => s.Value == "Sega Genesis").Count);

            session.ToggleFilter(new SearchFilter(ListCategoryType.Series, "Sonic"));
            Type(session, "sega");

            Assert.Equal(1, session.Suggestions.Single(s => s.Value == "Sega Genesis").Count);
        }

        // ------------------------------------------------------------------
        // Stage 4d - the chip row.
        // ------------------------------------------------------------------

        /// <summary>Walks the cursor into the chip row. Bounded, so an unreachable zone fails.</summary>
        private static void EnterChips(SearchSession session)
        {
            for (int guard = 0; guard < 50 && session.Zone != SearchZone.Chips; guard++)
            {
                session.MoveUp(false);
            }

            Assert.Equal(SearchZone.Chips, session.Zone);
        }

        private static SearchSession FilteredSession(params SearchFilter[] filters)
        {
            SearchSession session = SonicSession();

            foreach (SearchFilter filter in filters)
            {
                session.ToggleFilter(filter);
            }

            return session;
        }

        private static SearchFilter Platform(string value) =>
            new SearchFilter(ListCategoryType.Platform, value);

        private static SearchFilter Series(string value) =>
            new SearchFilter(ListCategoryType.Series, value);

        private static SearchFilter Developer(string value) =>
            new SearchFilter(ListCategoryType.Developer, value);

        // ------------------------------------------------------------------
        // Reaching and leaving the row.
        // ------------------------------------------------------------------

        [Fact]
        public void With_no_filters_there_is_no_chip_row_to_reach()
        {
            SearchSession session = SonicSession();

            for (int press = 0; press < 20; press++)
            {
                session.MoveUp(false);
                Assert.NotEqual(SearchZone.Chips, session.Zone);
            }
        }

        /// <summary>
        /// The zone order is chips, then keyboard, then results - so up from the top keyboard row
        /// reaches the chips, and only goes past them to the results when there are none.
        /// </summary>
        [Fact]
        public void Up_from_the_top_of_the_keyboard_reaches_the_chips()
        {
            SearchSession session = FilteredSession(Series("Sonic"));

            EnterChips(session);
        }

        [Fact]
        public void Down_from_the_chips_returns_to_the_keyboard()
        {
            SearchSession session = FilteredSession(Series("Sonic"));
            EnterChips(session);

            session.MoveDown(false);

            Assert.Equal(SearchZone.Keyboard, session.Zone);
            Assert.True(session.Keyboard.IsOnFirstRow);
        }

        /// <summary>RULE-SEARCH-037 - held movement never crosses a zone boundary.</summary>
        [Fact]
        public void Held_up_never_reaches_the_chips()
        {
            SearchSession session = FilteredSession(Series("Sonic"));

            for (int press = 0; press < 20; press++)
            {
                session.MoveUp(true);
                Assert.Equal(SearchZone.Keyboard, session.Zone);
            }
        }

        [Fact]
        public void Up_from_the_first_suggestion_reaches_the_chips()
        {
            SearchSession session = FilteredSession(Series("Sonic"));
            Type(session, "son");
            EnterSuggestions(session);

            Assert.Equal(0, session.SelectedSuggestionIndex);
            session.MoveUp(false);

            Assert.Equal(SearchZone.Chips, session.Zone);
        }

        // ------------------------------------------------------------------
        // Moving within the row.
        // ------------------------------------------------------------------

        [Fact]
        public void Left_and_right_move_between_chips()
        {
            SearchSession session = FilteredSession(Series("Sonic"), Platform("Sega Genesis"));
            EnterChips(session);

            Assert.Equal(0, session.SelectedChipIndex);

            session.MoveRight(false);
            Assert.Equal(1, session.SelectedChipIndex);

            session.MoveLeft(false);
            Assert.Equal(0, session.SelectedChipIndex);
        }

        [Fact]
        public void The_chip_selection_wraps()
        {
            SearchSession session = FilteredSession(Series("Sonic"), Platform("Sega Genesis"));
            EnterChips(session);

            session.MoveLeft(false);
            Assert.Equal(1, session.SelectedChipIndex);

            session.MoveRight(false);
            Assert.Equal(0, session.SelectedChipIndex);
        }

        [Fact]
        public void The_selected_chip_is_the_one_the_cursor_is_on()
        {
            SearchSession session = FilteredSession(Series("Sonic"), Platform("Sega Genesis"));
            EnterChips(session);
            session.MoveRight(false);

            Assert.Equal(Platform("Sega Genesis"), session.SelectedChip.Value.Filter);
        }

        // ------------------------------------------------------------------
        // Removing - RULE-SEARCH-064, 065. VER-SEARCH-025.
        // ------------------------------------------------------------------

        /// <summary>
        /// Step 5 of the requested script: remove one filter and the results widen again, with
        /// nothing retyped.
        /// </summary>
        [Fact]
        public void Removing_a_chip_widens_the_results_without_retyping()
        {
            SearchSession session = FilteredSession(Series("Sonic"), Platform("Sega Genesis"));
            Assert.Equal(new[] { 0 }, Found(session));

            EnterChips(session);
            session.MoveRight(false);
            session.RemoveSelectedChip();

            Assert.Single(session.Filters);
            Assert.Equal(new[] { 0, 1 }, Found(session));
        }

        [Fact]
        public void Removing_a_chip_leaves_the_query_alone()
        {
            SearchSession session = FilteredSession(Platform("Sega Genesis"));
            Type(session, "sonic");

            EnterChips(session);
            session.RemoveSelectedChip();

            Assert.Equal("sonic", session.Query);
            Assert.Empty(session.Filters);
        }

        /// <summary>
        /// RULE-SEARCH-032 - removing the last chip takes the row away, so the cursor cannot be
        /// left standing in a zone that no longer exists.
        /// </summary>
        [Fact]
        public void Removing_the_last_chip_moves_the_cursor_out_of_the_row()
        {
            SearchSession session = FilteredSession(Series("Sonic"));
            EnterChips(session);

            session.RemoveSelectedChip();

            Assert.Empty(session.Filters);
            Assert.Equal(SearchZone.Keyboard, session.Zone);
        }

        [Fact]
        public void Removing_a_chip_keeps_the_cursor_inside_the_row()
        {
            SearchSession session = FilteredSession(Series("Sonic"), Platform("Sega Genesis"));
            EnterChips(session);
            session.MoveRight(false);

            session.RemoveSelectedChip();

            Assert.Single(session.Filters);
            Assert.InRange(session.SelectedChipIndex, 0, session.Chips.Count - 1);
        }

        [Fact]
        public void Removing_a_chip_when_there_are_none_does_nothing()
        {
            SearchSession session = SonicSession();

            session.RemoveSelectedChip();

            Assert.Empty(session.Filters);
            Assert.Null(session.SelectedChip);
        }

        /// <summary>
        /// Filters can be removed in any order and the set that survives is always exactly what
        /// the remaining ones say - nothing is stored incrementally.
        /// </summary>
        [Fact]
        public void Filters_can_be_removed_in_any_order()
        {
            SearchSession first = FilteredSession(Series("Sonic"), Platform("Sega Genesis"));
            EnterChips(first);
            first.RemoveSelectedChip();

            SearchSession second = FilteredSession(Platform("Sega Genesis"), Series("Sonic"));
            EnterChips(second);
            while (!second.SelectedChip.Value.Filter.Equals(Series("Sonic")))
            {
                second.MoveRight(false);
            }
            second.RemoveSelectedChip();

            Assert.Equal(Found(first), Found(second));
        }

        // ------------------------------------------------------------------
        // Grouping and joining words - RULE-SEARCH-067.
        // ------------------------------------------------------------------

        [Fact]
        public void The_first_chip_has_no_joining_word()
        {
            SearchSession session = FilteredSession(Series("Sonic"));

            Assert.Equal(FilterJoin.None, session.Chips[0].Join);
        }

        [Fact]
        public void Chips_of_different_facets_join_with_and()
        {
            SearchSession session = FilteredSession(Series("Sonic"), Platform("Sega Genesis"));

            Assert.Equal(FilterJoin.And, session.Chips[1].Join);
        }

        /// <summary>
        /// A game has exactly one platform, so two platform filters accept either - and the row
        /// has to say so, because the alternative is a hidden rule the user is expected to know.
        /// </summary>
        [Fact]
        public void Two_chips_of_a_single_valued_facet_join_with_or()
        {
            SearchSession session = FilteredSession(Platform("Sega Genesis"), Platform("Dreamcast"));

            Assert.Equal(FilterJoin.Or, session.Chips[1].Join);
        }

        /// <summary>
        /// Two developers join with "or" as well - every facet does now (RULE-SEARCH-051). This
        /// asserted "and" until the algebra was made uniform, on the reasoning that a game can
        /// carry several developers. It can, in the schema; in a real library it does not, so the
        /// "and" it drew was describing a filter pair that could never match.
        /// </summary>
        [Fact]
        public void Two_chips_of_any_one_facet_join_with_or()
        {
            SearchSession session = FilteredSession(Developer("Sonic Team"), Series("Sonic"));
            session.ToggleFilter(Developer("Sega AM2"));

            Assert.Equal(2, session.Chips.Count(chip => chip.Facet == ListCategoryType.Developer));
            Assert.Equal(FilterJoin.Or, session.Chips[1].Join);
        }


        /// <summary>
        /// The reason filters are grouped rather than kept strictly chronological. Interleaved -
        /// platform, series, platform - a pairwise reading would render "Genesis and Sonic or
        /// Dreamcast", which states a grouping the algebra does not use. Keeping each facet's
        /// filters together makes every joining word true where it stands.
        /// </summary>
        [Fact]
        public void A_filter_joins_the_others_of_its_facet_rather_than_the_end_of_the_row()
        {
            SearchSession session = FilteredSession(Platform("Sega Genesis"), Series("Sonic"));
            session.ToggleFilter(Platform("Dreamcast"));

            Assert.Equal(ListCategoryType.Platform, session.Chips[0].Facet);
            Assert.Equal(ListCategoryType.Platform, session.Chips[1].Facet);
            Assert.Equal(ListCategoryType.Series, session.Chips[2].Facet);

            Assert.Equal(FilterJoin.None, session.Chips[0].Join);
            Assert.Equal(FilterJoin.Or, session.Chips[1].Join);
            Assert.Equal(FilterJoin.And, session.Chips[2].Join);
        }

        [Fact]
        public void Facet_groups_appear_in_the_order_their_first_filter_arrived()
        {
            SearchSession session = FilteredSession(Series("Sonic"), Platform("Sega Genesis"));
            session.ToggleFilter(Platform("Dreamcast"));

            Assert.Equal(ListCategoryType.Series, session.Chips[0].Facet);
            Assert.Equal(ListCategoryType.Platform, session.Chips[1].Facet);
        }

        [Fact]
        public void The_chip_row_agrees_with_the_filters_it_draws()
        {
            SearchSession session = FilteredSession(Series("Sonic"), Platform("Sega Genesis"));

            Assert.Equal(session.Filters.Count, session.Chips.Count);
            Assert.Equal(session.Filters, session.Chips.Select(chip => chip.Filter));
        }

        // ------------------------------------------------------------------
        // Clear-all - RULE-SEARCH-038, 068.
        // ------------------------------------------------------------------

        [Fact]
        public void Clear_removes_the_query_while_there_is_one()
        {
            SearchSession session = FilteredSession(Series("Sonic"));
            Type(session, "sonic");

            session.Clear();

            Assert.Equal(string.Empty, session.Query);
            Assert.Single(session.Filters);
        }

        /// <summary>
        /// With the query already gone, the same key takes the filters off - the way out of an
        /// over-narrowed search in one press rather than one press per chip.
        /// </summary>
        [Fact]
        public void Clear_removes_every_filter_once_the_query_is_empty()
        {
            SearchSession session = FilteredSession(Series("Sonic"), Platform("Sega Genesis"));

            session.Clear();

            Assert.Empty(session.Filters);
            Assert.Empty(session.Results);
        }

        [Fact]
        public void Clear_with_nothing_to_clear_does_nothing()
        {
            SearchSession session = SonicSession();
            int changes = 0;
            session.Changed += (sender, args) => changes++;

            session.Clear();

            Assert.Equal(0, changes);
        }

        /// <summary>
        /// The key labels itself from this, which is what stops a context-sensitive destructive
        /// key from being a surprise.
        /// </summary>
        [Fact]
        public void The_clear_key_says_which_of_its_two_jobs_it_will_do()
        {
            SearchSession session = SonicSession();
            Assert.False(session.ClearsFilters);

            session.ToggleFilter(Series("Sonic"));
            Assert.True(session.ClearsFilters);

            Type(session, "so");
            Assert.False(session.ClearsFilters);
        }

        [Fact]
        public void Pressing_the_clear_key_twice_removes_the_query_then_the_filters()
        {
            SearchSession session = FilteredSession(Series("Sonic"));
            Type(session, "sonic");

            session.Clear();
            Assert.Equal(string.Empty, session.Query);
            Assert.Single(session.Filters);

            session.Clear();
            Assert.Empty(session.Filters);
        }

        // ------------------------------------------------------------------
        // Stage 5c - the session resolves every row, not just the primary.
        // RULE-SEARCH-073 … 078. What the rows ARE is pinned in SearchRowsTests, below this
        // boundary; what is asked here is that each one finds what its own constraints leave.
        // ------------------------------------------------------------------

        private static IReadOnlyList<int> RowGames(SearchSession session, int row)
        {
            return session.Rows[row].Hits.Select(hit => hit.CatalogIndex).ToList();
        }

        private static IReadOnlyList<string> RowNames(SearchSession session)
        {
            return session.Rows.Select(row => row.Name).ToList();
        }

        [Fact]
        public void One_constraint_produces_one_row()
        {
            SearchSession session = FilteredSession(Platform("Sega Genesis"));

            Assert.Single(session.Rows);
            Assert.True(session.Rows[0].IsPrimary);
        }

        [Fact]
        public void The_primary_rows_hits_are_the_sessions_results()
        {
            SearchSession session = FilteredSession(Platform("Sega Genesis"), Series("Sonic"));

            Assert.Equal(session.Results, session.Rows[0].Hits);
        }

        /// <summary>
        /// The heart of it: each row is the search with one constraint taken out, and finds what
        /// that leaves. Sonic the Hedgehog is the only Genesis game in the Sonic series, so the
        /// primary is one game while dropping either filter finds two different pairs.
        /// </summary>
        [Fact]
        public void Each_row_finds_what_its_own_constraints_leave()
        {
            SearchSession session = FilteredSession(Platform("Sega Genesis"), Series("Sonic"));

            Assert.Equal(3, session.Rows.Count);

            Assert.Equal(new[] { 0 }, RowGames(session, 0));
            Assert.Equal(new[] { 0, 2 }, RowGames(session, 1));
            Assert.Equal(new[] { 0, 1 }, RowGames(session, 2));

            Assert.Equal(
                new[] { "Search: Sega Genesis · Sonic", "Sega Genesis", "Sonic" },
                RowNames(session));
        }

        /// <summary>
        /// RULE-SEARCH-074 - the typed text is a constraint like any other, so it gets a row that
        /// drops it. Here that row is the only one with anything in it, which is exactly the case
        /// it exists for: the filters are fine and the spelling is not.
        /// </summary>
        [Fact]
        public void The_query_gets_a_row_that_leaves_it_out()
        {
            SearchSession session = FilteredSession(Series("Sonic"));
            Type(session, "streets");

            Assert.Empty(session.Rows[0].Hits);
            Assert.Equal(new[] { 0, 1 }, RowGames(session, 1));
        }

        [Fact]
        public void A_search_that_finds_nothing_still_has_a_primary_row()
        {
            SearchSession session = FilteredSession(Series("Sonic"));
            Type(session, "streets");

            Assert.True(session.Rows[0].IsPrimary);
            Assert.False(session.HasResults);
        }

        /// <summary>
        /// A secondary row is normally a superset of the primary - it carries every constraint
        /// but one. The exception is dropping one of two filters on a single-valued facet, where
        /// the OR (RULE-SEARCH-052) makes the row a SUBSET: "Genesis or Dreamcast" holds Sonic
        /// Adventure, and taking Dreamcast away leaves a row with nothing in it. Such a row is
        /// dropped rather than installed as an empty strip under a heading.
        /// </summary>
        [Fact]
        public void A_secondary_row_that_finds_nothing_is_dropped()
        {
            SearchSession session = FilteredSession(Platform("Sega Genesis"), Platform("Dreamcast"));
            Type(session, "adventure");

            Assert.Equal(
                new[] { "Search: adventure · Sega Genesis · Dreamcast", "Sega Genesis · Dreamcast", "adventure · Dreamcast" },
                RowNames(session));

            Assert.Equal(new[] { 1 }, RowGames(session, 0));
            Assert.Equal(new[] { 0, 1, 2 }, RowGames(session, 1));
            Assert.Equal(new[] { 1 }, RowGames(session, 2));
        }

        [Fact]
        public void Removing_a_filter_rebuilds_the_rows()
        {
            SearchSession session = FilteredSession(Platform("Sega Genesis"), Series("Sonic"));
            Assert.Equal(3, session.Rows.Count);

            session.ToggleFilter(Series("Sonic"));

            Assert.Single(session.Rows);
        }

        /// <summary>
        /// The remembered place is a row and a column together (RULE-SEARCH-035). An edit clears
        /// both, for the reason it always cleared the column: a new result set is a new set of
        /// rows, in which the old place means nothing.
        /// </summary>
        [Fact]
        public void Editing_the_query_forgets_the_row_as_well_as_the_place_in_it()
        {
            SearchSession session = FilteredSession(Platform("Sega Genesis"), Series("Sonic"));
            session.RememberedRowIndex = 2;
            session.RememberedResultIndex = 1;

            session.Append('s');

            Assert.Equal(0, session.RememberedRowIndex);
            Assert.Equal(0, session.RememberedResultIndex);
        }
    }
}
