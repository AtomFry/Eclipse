using System.Collections.Generic;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// What the user has typed, split into the part they have finished and the part they are
    /// still in the middle of.
    ///
    /// That distinction is the whole of as-you-type search. A term the user has moved on from
    /// is a whole word and is matched exactly; the term under the cursor is a prefix by
    /// definition, because the next keystroke may extend it. Matching the last term exactly
    /// would mean results only appearing on the keystroke that happened to complete a word,
    /// which on a d-pad is most of the way through a search that already cost twenty presses.
    ///
    /// This is the same split Elasticsearch calls match_bool_prefix, and it is why the plan's
    /// fuzzy stage (docs/plans/text-search.md 5.4) can say "the last term is never fuzzy while
    /// it is still being typed" - the two ideas need the same piece of information.
    ///
    /// Pure. Parses text and nothing else.
    /// </summary>
    public sealed class SearchQuery
    {
        private static readonly string[] NoTerms = new string[0];

        /// <summary>An empty query. Matches nothing rather than everything - see IsEmpty.</summary>
        public static SearchQuery Empty { get; } = new SearchQuery(NoTerms, null);

        private SearchQuery(IReadOnlyList<string> completeTerms, string prefixTerm)
        {
            CompleteTerms = completeTerms;
            PrefixTerm = prefixTerm;
        }

        /// <summary>Terms the user has finished typing. Matched exactly.</summary>
        public IReadOnlyList<string> CompleteTerms { get; }

        /// <summary>
        /// The term still under the cursor, matched as a prefix, or null when the buffer ends at
        /// a word boundary - which happens when the user has typed a space, or typed nothing.
        /// </summary>
        public string PrefixTerm { get; }

        public bool IsEmpty => CompleteTerms.Count == 0 && PrefixTerm == null;

        /// <summary>How many terms have to match for a game to survive. All of them do (AND).</summary>
        public int TermCount => CompleteTerms.Count + (PrefixTerm == null ? 0 : 1);

        /// <summary>
        /// Turns a raw query buffer into terms.
        ///
        /// Whether the final token is complete is decided from the raw buffer rather than from
        /// the analysed text, because analysis trims the trailing space that carries exactly
        /// that information.
        /// </summary>
        public static SearchQuery Parse(string buffer)
        {
            IReadOnlyList<string> tokens = TextAnalyzer.Tokenize(buffer);
            if (tokens.Count == 0)
            {
                return Empty;
            }

            if (EndsAtWordBoundary(buffer))
            {
                return new SearchQuery(tokens, null);
            }

            // the last token is still being typed
            string[] complete = new string[tokens.Count - 1];
            for (int index = 0; index < complete.Length; index++)
            {
                complete[index] = tokens[index];
            }

            return new SearchQuery(complete, tokens[tokens.Count - 1]);
        }

        // An apostrophe continues a word - it is dropped by the analyser without splitting one -
        // so a buffer ending in one is still mid-term. Everything else that is not a letter or a
        // digit ends the term the user was typing.
        private static bool EndsAtWordBoundary(string buffer)
        {
            char last = buffer[buffer.Length - 1];

            if (char.IsLetterOrDigit(last))
            {
                return false;
            }

            return last != '\'' && last != '’' && last != '‘' && last != '`';
        }

        public override string ToString()
        {
            string complete = string.Join(" ", CompleteTerms);
            return PrefixTerm == null ? complete : $"{complete} {PrefixTerm}~".TrimStart();
        }
    }
}
