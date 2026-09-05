using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Eclipse.Service.Search
{
    /// <summary>
    /// The analysis chain: the first stage of the search engine, and the part most worth getting
    /// right, because everything downstream inherits its mistakes.
    ///
    /// Two operations, and the difference between them matters:
    ///
    ///   Normalize / Tokenize   applied identically to indexed titles and to query text. That
    ///                          symmetry is what makes the engine predictable - a title and a
    ///                          query that look the same to a person look the same here.
    ///
    ///   IndexTerms             applied to indexed titles only. It expands a token into the
    ///                          other ways the same thing is written, which is an indexing
    ///                          step, not a normalisation step: with "vii", "7" and "seven"
    ///                          all in the index pointing at the same game, a query only ever
    ///                          needs to be literal. Expanding the query as well would be
    ///                          redundant work for the same result.
    ///
    /// Pure. No games, no catalog, no session, no screen - see docs/plans/text-search.md 7.6.
    ///
    /// This is deliberately NOT shared with Models/GameTitleGrammarBuilder.cs, which does
    /// overlapping work for the voice grammar. That code's behaviour is RULE-SEARCH-001..007,
    /// is relied on by every voice result, and converging the two is a later exercise gated on
    /// the characterization tests in Eclipse.Tests/VoiceSearchCharacterizationTests.cs. Doing it
    /// while building text search would change voice results as a side effect.
    /// </summary>
    public static class TextAnalyzer
    {
        private static readonly char[] SpaceSplitter = { ' ' };
        private static readonly string[] NoTokens = new string[0];

        /// <summary>
        /// Folds text into the form the index and the query both speak: lower case, no accents,
        /// no punctuation, single-spaced.
        ///
        /// Order matters in one place. Apostrophes are dropped without leaving a space, so
        /// "Rock n' Roll" yields rock / n / roll rather than rock / n / / roll; everything else
        /// that is not a letter or a digit becomes a space. That has to happen before the
        /// general punctuation pass, not after it.
        /// </summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string decomposed = Decompose(text);
            StringBuilder builder = new StringBuilder(decomposed.Length);

            foreach (char character in decomposed)
            {
                // Accents, having been split off their letter by the decomposition above.
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (IsApostrophe(character))
                {
                    // dropped without a space, so the letters either side stay one word
                    continue;
                }

                if (character == '&')
                {
                    builder.Append(" and ");
                    continue;
                }

                if (character == '+')
                {
                    builder.Append(" plus ");
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                    continue;
                }

                builder.Append(' ');
            }

            return CollapseWhitespace(builder);
        }

        /// <summary>The tokens of a piece of text, in order. Never null.</summary>
        public static IReadOnlyList<string> Tokenize(string text)
        {
            string normalized = Normalize(text);
            if (normalized.Length == 0)
            {
                return NoTokens;
            }

            return normalized.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Every term a token should be findable by: the token itself, plus the other spellings
        /// of the same number where it is one. "vii" indexes as vii, 7 and seven, so the user
        /// can type whichever form is printed on the box.
        ///
        /// This is what replaces the voice path's destructive substitution (RULE-SEARCH-004),
        /// where the digit form was findable and the roman form was not - and with it goes that
        /// rule's exclusion of X, because an additive alternate for a letter never removes the
        /// ability to match the letter.
        /// </summary>
        public static IReadOnlyList<string> IndexTerms(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return NoTokens;
            }

            IReadOnlyList<string> alternates;
            if (TryGetNumeralAlternates(token, out alternates))
            {
                return alternates;
            }

            return new[] { token };
        }

        /// <summary>
        /// The other spellings of a token that is a number, including the token itself, or false
        /// where it is not one.
        ///
        /// Exposed separately from IndexTerms because scoring needs to ask the question without
        /// allocating: the overwhelming majority of tokens are not numerals, and this answers
        /// false for them with one dictionary probe.
        /// </summary>
        public static bool TryGetNumeralAlternates(string token, out IReadOnlyList<string> alternates)
        {
            string[] forms;
            if (token != null && NumeralForms.TryGetValue(token, out forms))
            {
                alternates = forms;
                return true;
            }

            alternates = null;
            return false;
        }

        // Unicode decomposition, so an accent becomes a separate mark that the pass above can
        // drop. Wrapped because Normalize throws on malformed input, and one bad title in a
        // library of thousands must not take the whole index build with it - the untouched
        // string still tokenizes, it just keeps its accents.
        private static string Decompose(string text)
        {
            try
            {
                return text.Normalize(NormalizationForm.FormKD);
            }
            catch (ArgumentException)
            {
                return text;
            }
        }

        private static bool IsApostrophe(char character)
        {
            return character == '\'' || character == '’' || character == '‘' || character == '`';
        }

        private static string CollapseWhitespace(StringBuilder builder)
        {
            StringBuilder collapsed = new StringBuilder(builder.Length);
            bool lastWasSpace = true;

            for (int index = 0; index < builder.Length; index++)
            {
                char character = builder[index];

                if (character == ' ')
                {
                    if (!lastWasSpace)
                    {
                        collapsed.Append(' ');
                        lastWasSpace = true;
                    }
                    continue;
                }

                collapsed.Append(character);
                lastWasSpace = false;
            }

            // one trailing space at most, from the loop above
            if (collapsed.Length > 0 && collapsed[collapsed.Length - 1] == ' ')
            {
                collapsed.Length = collapsed.Length - 1;
            }

            return collapsed.ToString();
        }

        // Every spelling of the numbers a game title actually uses, keyed by each spelling so a
        // lookup from any form finds all of them.
        //
        // One is deliberately absent from the roman forms: "i" is the English word far more
        // often than it is the numeral, and folding it would make every title containing "I"
        // findable by "one". Ten upwards keep theirs - see IndexTerms.
        private static readonly Dictionary<string, string[]> NumeralForms = BuildNumeralForms();

        private static Dictionary<string, string[]> BuildNumeralForms()
        {
            (string Word, string Roman)[] numbers =
            {
                ("one", null),
                ("two", "ii"),
                ("three", "iii"),
                ("four", "iv"),
                ("five", "v"),
                ("six", "vi"),
                ("seven", "vii"),
                ("eight", "viii"),
                ("nine", "ix"),
                ("ten", "x"),
                ("eleven", "xi"),
                ("twelve", "xii"),
                ("thirteen", "xiii"),
                ("fourteen", "xiv"),
                ("fifteen", "xv"),
                ("sixteen", "xvi"),
                ("seventeen", "xvii"),
                ("eighteen", "xviii"),
                ("nineteen", "xix"),
                ("twenty", "xx")
            };

            Dictionary<string, string[]> forms = new Dictionary<string, string[]>(StringComparer.Ordinal);

            for (int index = 0; index < numbers.Length; index++)
            {
                string digits = (index + 1).ToString(CultureInfo.InvariantCulture);

                List<string> spellings = new List<string> { digits, numbers[index].Word };
                if (numbers[index].Roman != null)
                {
                    spellings.Add(numbers[index].Roman);
                }

                string[] all = spellings.ToArray();
                foreach (string spelling in all)
                {
                    forms[spelling] = all;
                }
            }

            return forms;
        }
    }
}
