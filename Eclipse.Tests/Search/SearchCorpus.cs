using System;
using System.Collections.Generic;
using System.Linq;
using Eclipse.Models;
using Eclipse.Service.Search;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// The golden corpus of docs/plans/text-search.md 12.1: a small, hand-curated library that
    /// realistic searches can be run against without a catalog, without LaunchBox and without
    /// Big Box.
    ///
    /// WHY IT IS SMALL. Seventy-two games, every one here for a stated reason. The value of a
    /// corpus like this is that when an assertion fails a person reads the diff and knows at once
    /// whether the ranking got better or worse. Several hundred generated records destroy that:
    /// nobody reads them, nobody can adjudicate a reordering, and the suite becomes something
    /// people re-baseline rather than think about.
    ///
    /// WHAT IT IS FOR. The unit tests each pin one component. They cannot catch the failures that
    /// matter most, which are interactions - an analyser change that widens a prefix range, a
    /// ranking constant that fixes "sonic" and breaks "zelda", a suggestion that promises a count
    /// it cannot deliver. Those only show up when the whole engine runs over data shaped like a
    /// real library.
    ///
    /// THE METADATA. Added in stage 4e for VER-SEARCH-031. It is deliberately not decoration:
    /// the shapes the filter feature needs are all present and named in the entries below - a
    /// game carrying two genres at once, a publisher whose name contains a word that is also a
    /// genre, several platforms, and values reachable only through another filter.
    /// </summary>
    public static class SearchCorpus
    {
        /// <summary>
        /// The library, ordered and indexed the way GameCatalog orders its own: by title. That
        /// makes catalog index a meaningful tiebreak here for the same reason it is one there,
        /// so ranking ties in these tests break the way they will in the product.
        /// </summary>
        public static IReadOnlyList<SearchableGame> Games { get; }

        /// <summary>The corpus, indexed for title search. Built once for the whole test run.</summary>
        public static TitleIndex Index { get; }

        /// <summary>The corpus's metadata, indexed for filtering.</summary>
        public static FacetIndex Facets { get; }

        static SearchCorpus()
        {
            Entry[] library = Library()
                .OrderBy(entry => entry.Title, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Games = library
                .Select((entry, catalogIndex) => new SearchableGame(catalogIndex, entry.Title, entry.Popularity))
                .ToArray();

            Index = TitleIndex.Build(Games);

            Facets = FacetIndex.Build(library
                .Select((entry, catalogIndex) => new GameFacets(catalogIndex, entry.FacetValues()))
                .ToArray());
        }

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

        /// <summary>Runs a query within a set of filters, and returns the matching titles.</summary>
        public static IReadOnlyList<string> Search(string buffer, params SearchFilter[] filters)
        {
            int[] filterSet = FilterSet.Apply(filters, Facets);

            return Titles(SearchEngine.Search(SearchQuery.Parse(buffer), Index,
                                              SearchEngine.DefaultMaxResults, filterSet));
        }

        /// <summary>The titles surviving a set of filters alone, in catalog order.</summary>
        public static IReadOnlyList<string> Filtered(params SearchFilter[] filters)
        {
            int[] surviving = FilterSet.Apply(filters, Facets) ?? new int[0];

            return surviving.Select(catalogIndex => Index.Game(catalogIndex).DisplayTitle).ToList();
        }

        /// <summary>The metadata terms offered for what has been typed, within a set of filters.</summary>
        public static IReadOnlyList<Suggestion> Suggest(string buffer, params SearchFilter[] filters)
        {
            return SuggestionRanker.Rank(buffer, Facets, filters, FilterSet.Apply(filters, Facets),
                                         SearchSession.DefaultMaxSuggestions,
                                         SearchSession.DefaultMaxPerFacet);
        }

        public static SearchFilter Genre(string value) => new SearchFilter(ListCategoryType.Genre, value);
        public static SearchFilter Platform(string value) => new SearchFilter(ListCategoryType.Platform, value);
        public static SearchFilter Publisher(string value) => new SearchFilter(ListCategoryType.Publisher, value);
        public static SearchFilter Developer(string value) => new SearchFilter(ListCategoryType.Developer, value);
        public static SearchFilter Series(string value) => new SearchFilter(ListCategoryType.Series, value);
        public static SearchFilter Year(string value) => new SearchFilter(ListCategoryType.ReleaseYear, value);

        /// <summary>One game, with as much metadata as the case it exists for needs.</summary>
        private sealed class Entry
        {
            public string Title;
            public int Popularity;
            public string Platform;
            public string Year;
            public string Series;
            public string Developer;
            public string Publisher;
            public string Genres;

            public IReadOnlyList<FacetValue> FacetValues()
            {
                List<FacetValue> values = new List<FacetValue>();

                Add(values, ListCategoryType.Platform, Platform);
                Add(values, ListCategoryType.ReleaseYear, Year);
                Add(values, ListCategoryType.Series, Series);
                Add(values, ListCategoryType.Developer, Developer);
                Add(values, ListCategoryType.Publisher, Publisher);

                if (Genres != null)
                {
                    foreach (string genre in Genres.Split(','))
                    {
                        Add(values, ListCategoryType.Genre, genre.Trim());
                    }
                }

                return values;
            }

            private static void Add(List<FacetValue> values, ListCategoryType facet, string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(new FacetValue(facet, value));
                }
            }
        }

        private static Entry Game(string title, int popularity, string platform = null, string year = null,
                                  string series = null, string developer = null, string publisher = null,
                                  string genres = null)
        {
            return new Entry
            {
                Title = title,
                Popularity = popularity,
                Platform = platform,
                Year = year,
                Series = series,
                Developer = developer,
                Publisher = publisher,
                Genres = genres
            };
        }

        private static IEnumerable<Entry> Library()
        {
            return new[]
            {
                // --- A series with numbered entries and a base game, plus a long edition of a
                //     short one. The brevity tiebreak is judged here.
                Game("Sonic the Hedgehog", 5, "Sega Genesis", "1991", "Sonic", "Sonic Team", "Sega", "Platform"),
                Game("Sonic the Hedgehog 2", 5, "Sega Genesis", "1992", "Sonic", "Sonic Team", "Sega", "Platform"),
                Game("Sonic the Hedgehog 3", 4, "Sega Genesis", "1994", "Sonic", "Sonic Team", "Sega", "Platform"),
                Game("Sonic the Hedgehog 2 Special Edition Collection", 1, "Sega Genesis", "1992", "Sonic", "Sonic Team", "Sega", "Platform"),

                // --- Titles sharing a prefix without sharing a series, and a word that is a
                //     prefix of an unrelated word.
                Game("Sonic & Knuckles", 4, "Sega Genesis", "1994", "Sonic", "Sonic Team", "Sega", "Platform"),
                Game("Sonic Adventure", 4, "Sega Dreamcast", "1998", "Sonic", "Sonic Team", "Sega", "Platform, Adventure"),
                Game("Sonic CD", 4, "Sega CD", "1993", "Sonic", "Sonic Team", "Sega", "Platform"),
                Game("Sonic Spinball", 2, "Sega Genesis", "1993", "Sonic", "Sega", "Sega", "Pinball"),
                Game("SonSon", 1, "Arcade", "1984", null, "Capcom", "Capcom", "Platform"),

                // --- Sonic-adjacent with none of the series' words in its title, so "sonic" is
                //     not accidentally treated as a genre.
                Game("Dr. Robotnik's Mean Bean Machine", 2, "Sega Genesis", "1993", "Sonic", "Compile", "Sega", "Puzzle"),

                // --- A slash-separated multi-title, the shape RULE-SEARCH-001 exists for on the
                //     voice side. Text search does not split it; both halves' words simply index.
                Game("Sonic 3D Blast / Sonic 3D Flickies' Island", 2, "Sega Genesis", "1996", "Sonic", "Traveller's Tales", "Sega", "Platform"),

                // --- Leading articles and colons. The article is not special-cased anywhere:
                //     matching is token based, so word order does not matter.
                Game("The Legend of Zelda", 5, "Nintendo Entertainment System", "1986", "The Legend of Zelda", "Nintendo", "Nintendo", "Action, Adventure"),
                Game("Zelda II: The Adventure of Link", 3, "Nintendo Entertainment System", "1987", "The Legend of Zelda", "Nintendo", "Nintendo", "Action, Adventure"),
                Game("The Legend of Zelda: A Link to the Past", 5, "Super Nintendo", "1991", "The Legend of Zelda", "Nintendo", "Nintendo", "Action, Adventure"),
                Game("The Legend of Zelda: Ocarina of Time", 5, "Nintendo 64", "1998", "The Legend of Zelda", "Nintendo", "Nintendo", "Action, Adventure"),
                Game("The Legend of Zelda: Majora's Mask", 4, "Nintendo 64", "2000", "The Legend of Zelda", "Nintendo", "Nintendo", "Action, Adventure"),
                Game("A Boy and His Blob", 2, "Nintendo Entertainment System", "1989", null, "Imagineering", "Absolute", "Puzzle, Platform"),
                Game("The Adventures of Batman & Robin", 3, "Sega Genesis", "1995", null, "Clockwork Tortoise", "Sega", "Action"),

                // --- Roman numerals across a whole series, including the base game with no
                //     numeral and the X that the voice path refuses to fold.
                Game("Final Fantasy", 4, "Nintendo Entertainment System", "1987", "Final Fantasy", "Square", "Square", "RPG"),
                Game("Final Fantasy II", 3, "Nintendo Entertainment System", "1988", "Final Fantasy", "Square", "Square", "RPG"),
                Game("Final Fantasy IV", 4, "Super Nintendo", "1991", "Final Fantasy", "Square", "Square", "RPG"),
                Game("Final Fantasy VI", 5, "Super Nintendo", "1994", "Final Fantasy", "Square", "Square", "RPG"),
                Game("Final Fantasy VII", 5, "Sony PlayStation", "1997", "Final Fantasy", "Square", "Square", "RPG"),
                Game("Final Fantasy VIII", 4, "Sony PlayStation", "1999", "Final Fantasy", "Square", "Square", "RPG"),
                Game("Final Fantasy X", 4, "Sony PlayStation 2", "2001", "Final Fantasy", "Square", "Square", "RPG"),
                Game("Final Fantasy Tactics", 4, "Sony PlayStation", "1997", "Final Fantasy", "Square", "Square", "RPG, Strategy"),

                // --- Numerals again, mixed with subtitles and an out-of-sequence "Super".
                Game("Castlevania", 4, "Nintendo Entertainment System", "1986", "Castlevania", "Konami", "Konami", "Action, Platform"),
                Game("Castlevania II: Simon's Quest", 3, "Nintendo Entertainment System", "1987", "Castlevania", "Konami", "Konami", "Action, Platform"),
                Game("Castlevania III: Dracula's Curse", 4, "Nintendo Entertainment System", "1989", "Castlevania", "Konami", "Konami", "Action, Platform"),
                Game("Castlevania: Symphony of the Night", 5, "Sony PlayStation", "1997", "Castlevania", "Konami", "Konami", "Action, RPG"),
                Game("Super Castlevania IV", 4, "Super Nintendo", "1991", "Castlevania", "Konami", "Konami", "Action, Platform"),

                // --- The other side of folding X: a title where X is a letter, not a ten.
                Game("Mega Man", 4, "Nintendo Entertainment System", "1987", "Mega Man", "Capcom", "Capcom", "Action, Platform"),
                Game("Mega Man 2", 5, "Nintendo Entertainment System", "1988", "Mega Man", "Capcom", "Capcom", "Action, Platform"),
                Game("Mega Man 3", 4, "Nintendo Entertainment System", "1990", "Mega Man", "Capcom", "Capcom", "Action, Platform"),
                Game("Mega Man X", 5, "Super Nintendo", "1993", "Mega Man", "Capcom", "Capcom", "Action, Platform"),
                Game("Mega Man Legends", 3, "Sony PlayStation", "1997", "Mega Man", "Capcom", "Capcom", "Action, RPG"),

                // --- A large family sharing two leading words, which is what makes a two-term
                //     query worth having.
                Game("Super Mario Bros.", 5, "Nintendo Entertainment System", "1985", "Super Mario", "Nintendo", "Nintendo", "Platform"),
                Game("Super Mario Bros. 2", 4, "Nintendo Entertainment System", "1988", "Super Mario", "Nintendo", "Nintendo", "Platform"),
                Game("Super Mario Bros. 3", 5, "Nintendo Entertainment System", "1990", "Super Mario", "Nintendo", "Nintendo", "Platform"),
                Game("Super Mario World", 5, "Super Nintendo", "1991", "Super Mario", "Nintendo", "Nintendo", "Platform"),
                Game("Super Mario 64", 5, "Nintendo 64", "1996", "Super Mario", "Nintendo", "Nintendo", "Platform"),
                Game("Super Mario Kart", 4, "Super Nintendo", "1992", "Super Mario", "Nintendo", "Nintendo", "Racing"),
                Game("Dr. Mario", 3, "Nintendo Entertainment System", "1990", "Super Mario", "Nintendo", "Nintendo", "Puzzle"),
                Game("Mario Paint", 2, "Super Nintendo", "1992", "Super Mario", "Nintendo", "Nintendo", "Creative"),

                // --- Diacritics.
                Game("Pokémon Red Version", 5, "Game Boy", "1996", "Pokémon", "Game Freak", "Nintendo", "RPG"),
                Game("Pokémon Blue Version", 5, "Game Boy", "1996", "Pokémon", "Game Freak", "Nintendo", "RPG"),
                Game("Pokémon Yellow Version", 4, "Game Boy", "1998", "Pokémon", "Game Freak", "Nintendo", "RPG"),
                Game("Ōkami", 4, "Sony PlayStation 2", "2006", null, "Clover Studio", "Capcom", "Action, Adventure"),

                // --- Apostrophes and ampersands.
                Game("Rock n' Roll Racing", 3, "Super Nintendo", "1993", null, "Blizzard", "Interplay", "Racing"),
                Game("Bill & Ted's Excellent Video Game Adventure", 1, "Nintendo Entertainment System", "1991", null, "Rocket Science", "LJN", "Adventure"),
                Game("Ys III: Wanderers from Ys", 2, "Super Nintendo", "1991", "Ys", "Nihon Falcom", "American Sammy", "RPG"),

                // --- Sports. The metadata shapes VER-SEARCH-031 needs: games carrying two genres
                //     at once, and a publisher whose name contains a word that is also a genre.
                Game("John Madden Football", 3, "Sega Genesis", "1990", "Madden", "EA", "EA Sports", "Sports, Football"),
                Game("Tecmo Super Bowl", 4, "Nintendo Entertainment System", "1991", null, "Tecmo", "Tecmo", "Sports, Football"),
                Game("NHL Hockey", 3, "Sega Genesis", "1991", "NHL", "EA", "EA Sports", "Sports, Hockey"),
                Game("FIFA International Soccer", 3, "Sega Genesis", "1993", "FIFA", "EA", "EA Sports", "Sports, Soccer"),
                Game("NBA Jam", 5, "Sega Genesis", "1993", "NBA Jam", "Midway", "Acclaim", "Sports, Basketball"),

                // --- More prefix variety on the letters a user types first.
                Game("Streets of Rage", 4, "Sega Genesis", "1991", "Streets of Rage", "Sega", "Sega", "Beat em up"),
                Game("Streets of Rage 2", 5, "Sega Genesis", "1992", "Streets of Rage", "Sega", "Sega", "Beat em up"),
                Game("Star Fox", 4, "Super Nintendo", "1993", "Star Fox", "Nintendo", "Nintendo", "Shooter"),
                Game("Super Metroid", 5, "Super Nintendo", "1994", "Metroid", "Nintendo", "Nintendo", "Action, Adventure"),
                Game("Secret of Mana", 4, "Super Nintendo", "1993", "Mana", "Square", "Square", "RPG"),
                Game("Shining Force", 3, "Sega Genesis", "1992", "Shining", "Camelot", "Sega", "RPG, Strategy"),
                Game("Contra", 4, "Nintendo Entertainment System", "1988", "Contra", "Konami", "Konami", "Shooter"),
                Game("Super C", 3, "Nintendo Entertainment System", "1990", "Contra", "Konami", "Konami", "Shooter"),
                Game("Contra III: The Alien Wars", 4, "Super Nintendo", "1992", "Contra", "Konami", "Konami", "Shooter"),

                // --- Short, single-token titles, where the brevity tiebreak is at its strongest.
                Game("Tetris", 5, "Game Boy", "1989", "Tetris", "Nintendo", "Nintendo", "Puzzle"),
                Game("Doom", 5, "PC", "1993", "Doom", "id Software", "id Software", "Shooter"),
                Game("R-Type", 3, "Arcade", "1987", "R-Type", "Irem", "Irem", "Shooter"),
                Game("EarthBound", 4, "Super Nintendo", "1994", "Mother", "Ape", "Nintendo", "RPG"),
                Game("Chrono Trigger", 5, "Super Nintendo", "1995", "Chrono", "Square", "Square", "RPG"),
                Game("Metal Gear Solid", 5, "Sony PlayStation", "1998", "Metal Gear", "Konami", "Konami", "Action, Stealth"),
                Game("Duke Nukem 3D", 3, "PC", "1996", "Duke Nukem", "3D Realms", "GT Interactive", "Shooter")
            };
        }
    }
}
