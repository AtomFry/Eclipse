using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Service.Search;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// The golden corpus of docs/plans/text-search.md 12.1: a small, hand-curated library that
    /// realistic searches can be run against without a catalog, without LaunchBox and without
    /// Big Box.
    ///
    /// WHY IT IS SMALL. Sixty-odd games, every one of them here for a stated reason. The value
    /// of a corpus like this is that when an assertion fails a person reads the diff and knows
    /// at once whether the ranking got better or worse. Several hundred generated records
    /// destroy that: nobody reads them, nobody can adjudicate a reordering, and the suite
    /// becomes something people re-baseline rather than think about.
    ///
    /// WHAT IT IS FOR. The unit tests each pin one component. They cannot catch the failures
    /// that matter most, which are interactions - an analyser change that widens a prefix range,
    /// a ranking constant that fixes "sonic" and breaks "zelda". Those only show up when the
    /// whole engine runs against titles shaped like real ones.
    ///
    /// WHAT IT DOES NOT CARRY YET. Facet values - genres, platforms, publishers, developers.
    /// Those arrive with the metadata filters in stage 4, and VER-SEARCH-031 extends this same
    /// fixture rather than starting a second one. The shapes that stage will need are already
    /// present in the titles here (a sports/football game, several platforms' worth of series,
    /// a publisher whose name contains a genre word); only the metadata is missing.
    /// </summary>
    public static class SearchCorpus
    {
        /// <summary>
        /// The library, ordered and indexed the way GameCatalog orders its own: by title. That
        /// makes catalog index a meaningful tiebreak here for the same reason it is one there,
        /// so ranking ties in these tests break the way they will in the product.
        /// </summary>
        public static IReadOnlyList<SearchableGame> Games { get; } = BuildGames();

        /// <summary>The corpus, indexed. Built once for the whole test run.</summary>
        public static TitleIndex Index { get; } = TitleIndex.Build(Games);

        /// <summary>The titles a search returned, in rank order.</summary>
        public static IReadOnlyList<string> Titles(IEnumerable<SearchHit> hits)
        {
            return hits.Select(hit => Index.Game(hit.CatalogIndex).DisplayTitle).ToList();
        }

        /// <summary>Runs a query against the corpus and returns the matching titles, best first.</summary>
        public static IReadOnlyList<string> Search(string buffer, int maxResults = SearchEngine.DefaultMaxResults)
        {
            return Titles(SearchEngine.Search(SearchQuery.Parse(buffer), Index, maxResults));
        }

        private static SearchableGame[] BuildGames()
        {
            (string Title, int Popularity)[] library =
            {
                // --- A series with numbered entries and a base game, plus a long edition of a
                //     short one. The brevity bonus is judged here.
                ("Sonic the Hedgehog", 5),
                ("Sonic the Hedgehog 2", 5),
                ("Sonic the Hedgehog 3", 4),
                ("Sonic the Hedgehog 2 Special Edition Collection", 1),

                // --- Titles sharing a prefix without sharing a series, and a word that is a
                //     prefix of an unrelated word. "son" must not become useless.
                ("Sonic & Knuckles", 4),
                ("Sonic Adventure", 4),
                ("Sonic CD", 4),
                ("Sonic Spinball", 2),
                ("SonSon", 1),

                // --- A related game with none of the series' words in its title, so that
                //     "sonic" is not accidentally treated as a genre.
                ("Dr. Robotnik's Mean Bean Machine", 2),

                // --- A slash-separated multi-title, the shape RULE-SEARCH-001 exists for on the
                //     voice side. Text search does not split it; both halves' words simply index.
                ("Sonic 3D Blast / Sonic 3D Flickies' Island", 2),

                // --- Leading articles and colons. The article is not special-cased anywhere:
                //     matching is token based, so word order does not matter.
                ("The Legend of Zelda", 5),
                ("Zelda II: The Adventure of Link", 3),
                ("The Legend of Zelda: A Link to the Past", 5),
                ("The Legend of Zelda: Ocarina of Time", 5),
                ("The Legend of Zelda: Majora's Mask", 4),
                ("A Boy and His Blob", 2),
                ("The Adventures of Batman & Robin", 3),

                // --- Roman numerals across a whole series, including the base game with no
                //     numeral at all and the X that the voice path refuses to fold.
                ("Final Fantasy", 4),
                ("Final Fantasy II", 3),
                ("Final Fantasy IV", 4),
                ("Final Fantasy VI", 5),
                ("Final Fantasy VII", 5),
                ("Final Fantasy VIII", 4),
                ("Final Fantasy X", 4),
                ("Final Fantasy Tactics", 4),

                // --- Numerals again, mixed with subtitles and an out-of-sequence "Super".
                ("Castlevania", 4),
                ("Castlevania II: Simon's Quest", 3),
                ("Castlevania III: Dracula's Curse", 4),
                ("Castlevania: Symphony of the Night", 5),
                ("Super Castlevania IV", 4),

                // --- The other side of folding X: a title where X is a letter, not a ten.
                ("Mega Man", 4),
                ("Mega Man 2", 5),
                ("Mega Man 3", 4),
                ("Mega Man X", 5),
                ("Mega Man Legends", 3),

                // --- A large family sharing two leading words, which is what makes a two-term
                //     query worth having.
                ("Super Mario Bros.", 5),
                ("Super Mario Bros. 2", 4),
                ("Super Mario Bros. 3", 5),
                ("Super Mario World", 5),
                ("Super Mario 64", 5),
                ("Super Mario Kart", 4),
                ("Dr. Mario", 3),
                ("Mario Paint", 2),

                // --- Diacritics.
                ("Pokémon Red Version", 5),
                ("Pokémon Blue Version", 5),
                ("Pokémon Yellow Version", 4),
                ("Ōkami", 4),

                // --- Apostrophes and ampersands.
                ("Rock n' Roll Racing", 3),
                ("Bill & Ted's Excellent Video Game Adventure", 1),
                ("Ys III: Wanderers from Ys", 2),

                // --- Sports, for the metadata filters that arrive in stage 4. The titles carry
                //     the words now; the facet values come with VER-SEARCH-031.
                ("John Madden Football", 3),
                ("NHL Hockey", 3),
                ("NBA Jam", 5),
                ("FIFA International Soccer", 3),
                ("Tecmo Super Bowl", 4),

                // --- More prefix variety on the letters a user types first.
                ("Streets of Rage", 4),
                ("Streets of Rage 2", 5),
                ("Star Fox", 4),
                ("Super Metroid", 5),
                ("Secret of Mana", 4),
                ("Shining Force", 3),
                ("Contra", 4),
                ("Super C", 3),
                ("Contra III: The Alien Wars", 4),

                // --- Short, single-token titles, where the brevity bonus is at its strongest.
                ("Tetris", 5),
                ("Doom", 5),
                ("R-Type", 3),
                ("EarthBound", 4),
                ("Chrono Trigger", 5),
                ("Metal Gear Solid", 5),
                ("Duke Nukem 3D", 3)
            };

            return library
                .OrderBy(entry => entry.Title, StringComparer.OrdinalIgnoreCase)
                .Select((entry, catalogIndex) => new SearchableGame(catalogIndex, entry.Title, entry.Popularity))
                .ToArray();
        }
    }
}
