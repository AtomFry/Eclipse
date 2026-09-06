using System;
using System.Collections.Generic;
using System.Text;
using Eclipse.Models;

namespace Eclipse.Service.Search
{
    /// <summary>Which part of the search screen has the cursor.</summary>
    public enum SearchZone
    {
        /// <summary>
        /// The applied filters. Present only while there are some; sits above the keyboard.
        /// </summary>
        Chips,

        /// <summary>The key grid. Always present.</summary>
        Keyboard,

        /// <summary>
        /// The metadata terms offered for what has been typed. Present only while there are
        /// some; RULE-SEARCH-032 keeps the cursor out of it otherwise.
        /// </summary>
        Suggestions,

        /// <summary>The box art row. Present only while there are results.</summary>
        Results

        // Chips join this in stage 4d, when the applied filters become focusable and removable.
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

        /// <summary>How many metadata terms are offered at once, and how many from any one facet.</summary>
        public const int DefaultMaxSuggestions = 8;
        public const int DefaultMaxPerFacet = 3;

        /// <summary>
        /// How much has to be typed before suggestions are shown.
        ///
        /// A display threshold, not an engine limit - SuggestionRanker answers a one-character
        /// query perfectly well, and the golden corpus shows it answering usefully.
        ///
        /// ON TRIAL AT ONE. It shipped at two on the reasoning that one character matches too
        /// much to be informative and the list would thrash on the next keystroke. That is a
        /// guess about a real library, and it cannot be checked while the threshold itself hides
        /// the evidence - so it is set to one to be looked at. The question being answered is
        /// whether a single letter offers something worth having on a library of thousands, or
        /// eight arbitrary high-count values. On a d-pad every character is expensive enough that
        /// the answer is worth finding out rather than assuming.
        /// </summary>
        public const int SuggestFromCharacters = 1;

        private static readonly SearchHit[] NoHits = new SearchHit[0];
        private static readonly Suggestion[] NoSuggestions = new Suggestion[0];
        private static readonly SearchRowResult[] NoRows = new SearchRowResult[0];

        private readonly ISearchIndexSource indexSource;
        private readonly StringBuilder query = new StringBuilder();
        private readonly List<SearchFilter> filters = new List<SearchFilter>();

        // The games the applied filters leave, or null for no constraint. Recomputed whenever the
        // filters change rather than maintained incrementally: filters are removable in any
        // order, so an incrementally narrowed set would have to be unwound.
        private int[] filterSet;

        // How many suggestions this session offers. Taken by constructor rather than read from
        // the settings singleton, so the session stays free of everything but its index source.
        private readonly int maxSuggestions;

        /// <param name="maxSuggestions">
        /// How many metadata terms to offer at once. Zero or less offers none, which is a
        /// legitimate way to turn suggestions off.
        /// </param>
        public SearchSession(ISearchIndexSource indexSource, SearchKeyboardLayout layout,
                             int maxSuggestions = DefaultMaxSuggestions)
        {
            this.indexSource = indexSource;
            this.maxSuggestions = maxSuggestions;
            Keyboard = new SearchKeyboard(layout);
            Results = NoHits;
            Suggestions = NoSuggestions;
        }

        /// <summary>Raised whenever anything a screen would draw has changed.</summary>
        public event EventHandler Changed;

        /// <summary>The key grid and its cursor.</summary>
        public SearchKeyboard Keyboard { get; }

        /// <summary>What has been typed so far. Never null.</summary>
        public string Query => query.ToString();

        public bool HasQuery => query.Length > 0;

        /// <summary>
        /// Every row this search produces, primary first - see SearchRows. One row until two
        /// constraints are applied, then one more for each constraint left out.
        /// </summary>
        public IReadOnlyList<SearchRowResult> Rows { get; private set; } = NoRows;

        /// <summary>
        /// The primary rows results, best first. Empty, never null.
        ///
        /// The search the user actually asked for, which is what the status line counts and what
        /// HasResults is about. The near misses beneath it are in Rows.
        /// </summary>
        public IReadOnlyList<SearchHit> Results { get; private set; }

        public int ResultCount => Results.Count;

        public bool HasResults => Results.Count > 0;

        /// <summary>The metadata terms currently offered, best first. Empty, never null.</summary>
        public IReadOnlyList<Suggestion> Suggestions { get; private set; }

        public bool HasSuggestions => Suggestions.Count > 0;

        /// <summary>Which suggestion is highlighted. Always inside the list.</summary>
        public int SelectedSuggestionIndex { get; private set; }

        /// <summary>The highlighted suggestion, or null when none are offered.</summary>
        public Suggestion SelectedSuggestion =>
            HasSuggestions ? Suggestions[SelectedSuggestionIndex] : null;

        /// <summary>
        /// The metadata filters applied, grouped by facet.
        ///
        /// Within a facet they are in the order they were added, and the facet groups appear in
        /// the order their first filter arrived - so the row still reads as a record of what the
        /// user did, at the level that matters.
        ///
        /// GROUPED RATHER THAN STRICTLY CHRONOLOGICAL, because the joining words have to be
        /// readable. Filters of one facet may combine with OR while everything else combines with
        /// AND, so a row that interleaved them - platform, genre, platform - would render as
        /// "Genesis and Sports or SNES", which states a grouping the algebra does not use. Keeping
        /// each facet's filters together means every joining word is true where it stands.
        /// </summary>
        public IReadOnlyList<SearchFilter> Filters => filters;

        /// <summary>
        /// The same filters as the chip row draws them, each carrying how it reads against the
        /// one before it.
        /// </summary>
        public IReadOnlyList<AppliedFilter> Chips { get; private set; } = new AppliedFilter[0];

        public bool HasFilters => filters.Count > 0;

        /// <summary>Which chip is highlighted. Always inside the row.</summary>
        public int SelectedChipIndex { get; private set; }

        /// <summary>The highlighted chip, or null when none are applied.</summary>
        public AppliedFilter? SelectedChip =>
            HasFilters ? Chips[SelectedChipIndex] : (AppliedFilter?)null;

        public bool IsOnChips => Zone == SearchZone.Chips;

        /// <summary>Which zone the cursor is in.</summary>
        public SearchZone Zone { get; private set; } = SearchZone.Keyboard;

        public bool IsOnResults => Zone == SearchZone.Results;

        public bool IsOnSuggestions => Zone == SearchZone.Suggestions;

        /// <summary>
        /// Where in the results the user was when they last left the screen, so returning puts
        /// them back (RULE-SEARCH-035).
        ///
        /// Opaque to this class - it is written and read by whatever owns the results row, and
        /// never interpreted here. It exists on the session rather than in that owner because
        /// the session is the thing that survives leaving the screen.
        /// </summary>
        public int RememberedResultIndex { get; set; }

        /// <summary>
        /// Which row the user was in, alongside where in it (RULE-SEARCH-073 gave a search more
        /// than one). Without this, returning to a search would put the user at the right column
        /// of the wrong row.
        /// </summary>
        public int RememberedRowIndex { get; set; }

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
        /// RULE-SEARCH-038 - clears the query while there is one, and every applied filter once
        /// there is not.
        ///
        /// Context-sensitive because the input surface has no room for a second key, and because
        /// per-chip removal alone does not scale to the case it is most needed in: a user four
        /// filters deep who has over-narrowed wants to start again, not to make four deliberate
        /// presses on a row they must first navigate to. The key says which of the two it will do
        /// (see ClearsFilters), which is what stops a context-sensitive destructive key being a
        /// surprise.
        /// </summary>
        public void Clear()
        {
            if (query.Length > 0)
            {
                query.Length = 0;
                OnQueryEdited();
                return;
            }

            ClearFilters();
        }

        /// <summary>
        /// Whether the clear key would remove the filters rather than the query - which is what
        /// the key labels itself from.
        /// </summary>
        public bool ClearsFilters => query.Length == 0 && filters.Count > 0;

        // An edit produces a different result set, in which the old place means nothing.
        private void OnQueryEdited()
        {
            RememberedResultIndex = 0;
            RememberedRowIndex = 0;
            RunQuery();
        }

        #endregion

        #region moving

        /// <summary>
        /// Moves left within the focused zone.
        ///
        /// From the suggestion list that means back to the keyboard - the two sit side by side,
        /// so the suggestions have nothing to their left but the keys.
        /// </summary>
        public void MoveLeft(bool held)
        {
            if (Zone == SearchZone.Chips)
            {
                SelectedChipIndex = SelectedChipIndex == 0 ? Chips.Count - 1 : SelectedChipIndex - 1;
                Raise();
                return;
            }

            if (Zone == SearchZone.Suggestions)
            {
                if (!held)
                {
                    Zone = SearchZone.Keyboard;
                }

                Raise();
                return;
            }

            Keyboard.MoveLeft();
            Raise();
        }

        /// <summary>
        /// Moves right within the focused zone.
        ///
        /// RULE-SEARCH-030 - horizontal movement does not wrap where an adjacent zone lies that
        /// way, so the rightmost key steps into the suggestion list rather than back round to the
        /// left of the row. Holding still wraps (RULE-SEARCH-037), which is what keeps fast
        /// travel across the key grid from shooting out of it.
        /// </summary>
        public void MoveRight(bool held)
        {
            if (Zone == SearchZone.Chips)
            {
                SelectedChipIndex = SelectedChipIndex == Chips.Count - 1 ? 0 : SelectedChipIndex + 1;
                Raise();
                return;
            }

            if (Zone == SearchZone.Suggestions)
            {
                Raise();
                return;
            }

            if (!held && HasSuggestions && Keyboard.IsAtEndOfRow)
            {
                Zone = SearchZone.Suggestions;
                Raise();
                return;
            }

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
            if (Zone == SearchZone.Chips)
            {
                // Nothing above the chips, so wrap round to the bottom of the screen.
                LeaveChipsUpward();
                Raise();
                return;
            }

            if (Zone == SearchZone.Suggestions)
            {
                // Off the top of the list and into the chips, if there are any.
                if (SelectedSuggestionIndex == 0 && !held && HasFilters)
                {
                    Zone = SearchZone.Chips;
                }
                else
                {
                    SelectedSuggestionIndex = SelectedSuggestionIndex == 0
                        ? Suggestions.Count - 1
                        : SelectedSuggestionIndex - 1;
                }

                Raise();
                return;
            }

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

            if (held)
            {
                // held movement never crosses a zone, so wrap inside the keyboard
                Keyboard.MoveToLastRow();
                Raise();
                return;
            }

            // The zone order is chips, then keyboard, then results - so up from the top row goes
            // to the chips where there are any, and only past them to the results.
            if (HasFilters)
            {
                Zone = SearchZone.Chips;
            }
            else if (HasResults)
            {
                Zone = SearchZone.Results;
            }
            else
            {
                Keyboard.MoveToLastRow();
            }

            Raise();
        }

        private void LeaveChipsUpward()
        {
            if (HasResults)
            {
                Zone = SearchZone.Results;
                return;
            }

            Zone = SearchZone.Keyboard;
            Keyboard.MoveToLastRow();
        }

        /// <summary>Moves down: within the keyboard, or out of it at the bottom edge. See MoveUp.</summary>
        public void MoveDown(bool held)
        {
            if (Zone == SearchZone.Chips)
            {
                Zone = SearchZone.Keyboard;
                Keyboard.MoveToFirstRow();
                Raise();
                return;
            }

            if (Zone == SearchZone.Suggestions)
            {
                // Off the bottom of the list and into the results, if there are any - the same
                // shape as leaving the keyboard's bottom row.
                if (SelectedSuggestionIndex == Suggestions.Count - 1 && !held && HasResults)
                {
                    Zone = SearchZone.Results;
                }
                else
                {
                    SelectedSuggestionIndex = SelectedSuggestionIndex == Suggestions.Count - 1
                        ? 0
                        : SelectedSuggestionIndex + 1;
                }

                Raise();
                return;
            }

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

        #region filters

        /// <summary>
        /// Applies the highlighted suggestion, or removes it if it is already applied
        /// (RULE-SEARCH-060/061/062).
        ///
        /// THE INVARIANT: the query buffer is scratch space used to discover a metadata term, and
        /// selecting one consumes it. Everything about the flow follows from that sentence - the
        /// buffer empties because its job is finished, and the free text that survives is by
        /// definition only text the user chose <em>not</em> to turn into a filter, which is
        /// exactly the text they meant as a title search.
        /// </summary>
        public void ApplySelectedSuggestion()
        {
            Suggestion selected = SelectedSuggestion;
            if (selected == null)
            {
                return;
            }

            ToggleFilter(selected.AsFilter());
        }

        /// <summary>Adds a filter, or removes it if it is already applied.</summary>
        public void ToggleFilter(SearchFilter filter)
        {
            if (FilterSet.Contains(filters, filter))
            {
                filters.RemoveAll(applied => applied.Equals(filter));
            }
            else
            {
                Insert(filter);
            }

            query.Length = 0;
            RememberedResultIndex = 0;
            RememberedRowIndex = 0;

            // The cursor goes back to the keyboard, because the list it was standing in is about
            // to be rebuilt around a query that no longer exists.
            Zone = SearchZone.Keyboard;

            RecomputeFilterSet();
            RunQuery();
        }

        /// <summary>
        /// Removes the highlighted chip, widening the results again without retyping anything
        /// (RULE-SEARCH-064/065).
        /// </summary>
        public void RemoveSelectedChip()
        {
            AppliedFilter? selected = SelectedChip;
            if (selected == null)
            {
                return;
            }

            filters.RemoveAll(applied => applied.Equals(selected.Value.Filter));
            RememberedResultIndex = 0;
            RememberedRowIndex = 0;

            RecomputeFilterSet();
            RunQuery();
        }

        // Filters of one facet are kept together, so the joining words the chip row draws are
        // true where they stand - see the note on Filters. A new filter joins the end of its
        // facet's run, or starts a new one at the end of the row.
        private void Insert(SearchFilter filter)
        {
            int lastOfFacet = -1;

            for (int index = 0; index < filters.Count; index++)
            {
                if (filters[index].Facet == filter.Facet)
                {
                    lastOfFacet = index;
                }
            }

            if (lastOfFacet >= 0)
            {
                filters.Insert(lastOfFacet + 1, filter);
            }
            else
            {
                filters.Add(filter);
            }
        }

        /// <summary>Removes every filter, leaving the query alone.</summary>
        public void ClearFilters()
        {
            if (filters.Count == 0)
            {
                return;
            }

            filters.Clear();
            RememberedResultIndex = 0;
            RememberedRowIndex = 0;

            RecomputeFilterSet();
            RunQuery();
        }

        private void RecomputeFilterSet()
        {
            filterSet = FilterSet.Apply(filters, indexSource?.Facets);
            RefreshChips();
        }

        /// <summary>
        /// Rebuilds the chip row, working out how each filter reads against the one before it.
        ///
        /// Every adjacent pair gets a word, because every pair genuinely combines: across facets
        /// it is always AND, and within one it is whichever RULE-SEARCH-051/052 says. Grouping the
        /// filters by facet on insert is what makes that safe to render pairwise.
        /// </summary>
        private void RefreshChips()
        {
            AppliedFilter[] chips = new AppliedFilter[filters.Count];

            for (int index = 0; index < filters.Count; index++)
            {
                FilterJoin join = FilterJoin.None;

                if (index > 0)
                {
                    // Mechanical now that the algebra is uniform: same facet as the chip before
                    // it means or, a different facet means and. The facet grouping in AddFilter
                    // is what makes reading it pairwise correct.
                    join = filters[index].Facet == filters[index - 1].Facet
                        ? FilterJoin.Or
                        : FilterJoin.And;
                }

                chips[index] = new AppliedFilter(filters[index], join);
            }

            Chips = chips;

            if (SelectedChipIndex >= chips.Length)
            {
                SelectedChipIndex = 0;
            }
        }

        #endregion

        #region results

        /// <summary>
        /// Re-runs the query against the index and republishes the results and suggestions.
        ///
        /// Called on every keystroke. The engine is a pure function, so this is the whole of
        /// "the results changed": there is no incremental state to keep in step, and nothing to
        /// invalidate.
        /// </summary>
        private void RunQuery()
        {
            List<SearchRowResult> resolved = new List<SearchRowResult>();

            foreach (SearchRow row in SearchRows.For(Query, filters))
            {
                IReadOnlyList<SearchHit> hits = Resolve(row);

                // A secondary row with nothing in it is simply not a row. The primary is kept
                // whatever it holds, because HasResults is the question "did this search find
                // anything" and RULE-SEARCH-044 answers it by hiding the browsing surface.
                //
                // An empty secondary is rarer than it looks: a row carries every constraint but
                // one, so it is normally a superset of the primary. The exception is dropping one
                // of two filters on a single-valued facet, where the OR (RULE-SEARCH-052) makes
                // the row a subset instead - "NES or SNES" minus SNES can find nothing where the
                // pair found something.
                if (row.IsPrimary || hits.Count > 0)
                {
                    resolved.Add(new SearchRowResult(row, hits));
                }
            }

            Rows = resolved;
            Results = resolved[0].Hits;

            RefreshSuggestions();

            // A zone with no content cannot hold the cursor (RULE-SEARCH-032). Being stranded in
            // one that has just vanished is how a user ends up pressing buttons that do nothing -
            // and removing the last chip is exactly that case.
            if ((!HasResults && Zone == SearchZone.Results)
                || (!HasSuggestions && Zone == SearchZone.Suggestions)
                || (!HasFilters && Zone == SearchZone.Chips))
            {
                Zone = SearchZone.Keyboard;
            }

            Raise();
        }

        private void RefreshSuggestions()
        {
            Suggestions = Query.Length < SuggestFromCharacters
                ? NoSuggestions
                : SuggestionRanker.Rank(Query,
                                        indexSource?.Facets,
                                        filters,
                                        filterSet,
                                        maxSuggestions,
                                        DefaultMaxPerFacet);

            if (SelectedSuggestionIndex >= Suggestions.Count)
            {
                SelectedSuggestionIndex = 0;
            }
        }

        /// <summary>
        /// One row's games.
        ///
        /// The same three cases the single row always had - text, filters alone, or neither -
        /// asked of one row's constraints rather than of the session's. The primary reuses the
        /// filter set already computed for the suggestions; a secondary resolves its own, which
        /// is the extra work the fan-out costs and what stage 5a measured.
        /// </summary>
        private IReadOnlyList<SearchHit> Resolve(SearchRow row)
        {
            ISearchIndex index = indexSource?.Index;
            if (index == null)
            {
                return NoHits;
            }

            int[] games = row.IsPrimary ? filterSet : FilterSet.Apply(row.Filters, indexSource?.Facets);
            SearchQuery parsed = SearchQuery.Parse(row.Query);

            if (!parsed.IsEmpty)
            {
                return SearchEngine.Search(parsed, index, LiveResultLimit, games);
            }

            // Filters but no text. The filters are the whole query, so the games they leave are
            // the results - in catalog order, which is the library's own sort-title order,
            // because with nothing typed there is no relevance to rank by.
            return games != null ? Browse(games) : NoHits;
        }

        private static SearchHit[] Browse(int[] games)
        {
            int count = Math.Min(games.Length, LiveResultLimit);
            SearchHit[] hits = new SearchHit[count];

            for (int index = 0; index < count; index++)
            {
                hits[index] = new SearchHit(games[index], SearchRank.Browsed);
            }

            return hits;
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
