using Eclipse.Service;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Eclipse.Models
{
    // a set of gamelists for browsing games by different categories (i.e. games by platform, games by genre, etc...)
    public class GameListSet
    {
        private List<GameList> gameLists;
        public List<GameList> GameLists
        {
            get
            {
                return gameLists;
            }
            set
            {
                gameLists = value;

                // A set used to number every game across all its lists here, writing a start
                // index onto each list so that a random pick could be a single number in that
                // space. Nothing needs that any more - random game counts the lists off as it
                // walks them - and writing it was actively harmful: "more like this" builds a
                // set from GameList instances borrowed from the genre, platform and series sets,
                // so numbering them overwrote the positions those sets were navigating by.
                totalGameCount = null;
            }
        }
        public ListCategoryType ListCategoryType { get; set; }

        // get total count of games across all lists
        private int? totalGameCount;
        public int TotalGameCount
        {
            get
            {
                if (totalGameCount == null)
                {
                    totalGameCount = gameLists.Sum(gameList => gameList.MatchCount);
                }

                return totalGameCount.GetValueOrDefault();
            }
        }
    }

    public class GameList : INotifyPropertyChanged
    {
        private EclipseSettings EclipseSettings = EclipseSettingsDataProvider.Instance.EclipseSettings;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        private ListCycle<GameMatch> gameCycle;

        public int SortOrder { get; set; }

        private List<GameMatch> matchingGames;
        public List<GameMatch> MatchingGames
        {
            get { return matchingGames; }
            set
            {
                if (matchingGames != value)
                {
                    matchingGames = value;

                    gameCycle = new ListCycle<GameMatch>(MatchingGames, 13);
                    gameCycle.CycleBackward();
                    RefreshGames();

                    PropertyChanged(this, new PropertyChangedEventArgs("MatchingGames"));
                }
            }
        }

        // A ListCategoryType lived here until B-35(a) was resolved. Nothing ever assigned it, so
        // every list in the product carried the default; the one place that read it - position
        // restoration's list match - was therefore comparing a constant against itself and always
        // passing, matching on ListTypeValue alone.
        //
        // Populating it would not have helped. Restoration already switches to the remembered
        // *set* before searching it, and every list within one set would be given that same set's
        // category - so the comparison would still have been a constant against itself. There is
        // no category a custom list could carry that the set does not already say. It was
        // redundant with rememberedSetCategory rather than under-used, so it is gone.
        //
        // VER-BROWSE-005 covers what the match does now.

        // to be used with selecting random games and reset the game cycle to the right spot
        public void SetGameIndex(int newIndex)
        {
            gameCycle.SetCurrentIndex(newIndex, true);
            RefreshGames();
            WarmRowImages();
        }

        public int CurrentGameIndex
        {
            get
            {
                return gameCycle.GetIndexValue(1);
            }
        }

        public GameList()
        {
        }

        public GameList(string _listTypeValue, List<GameMatch> _matchingGames, int sortOrder = 9999)
        {
            ListTypeValue = _listTypeValue;
            MatchingGames = _matchingGames;
            SortOrder = sortOrder;

            ListDescription = ListTypeValue;
            if (EclipseSettingsDataProvider.Instance.EclipseSettings.ShowGameCountInList)
            {
                ListDescription = $"{ListDescription} ({MatchCount})";
            }
        }

        public void CycleForward()
        {
            gameCycle.CycleForward();
            RefreshGames();
            WarmRowImages();
        }

        public void CycleBackward()
        {
            gameCycle.CycleBackward();
            RefreshGames();
            WarmRowImages();
        }

        private GameMatch[] SnapshotSlots()
        {
            GameMatch[] slots = new GameMatch[SlotCount];

            slots[0] = PreviousGame;
            slots[1] = SelectedGame;
            UpcomingGames.CopyTo(slots, 2);

            return slots;
        }

        // The cycle supplies thirteen slots: one behind the selection, the selection, and eleven
        // ahead of it.
        private const int SlotCount = 13;
        private const int UpcomingSlotCount = SlotCount - 2;

        private void RefreshGames()
        {
            // at the start of the list, reset the previous game at index 0 to null
            if (gameCycle.GetIndexValue(1) == 0)
            {
                PreviousGame = null;
            }
            else
            {
                PreviousGame = gameCycle.GetItem(0);
            }

            SelectedGame = GameForSlot(1);

            for (int slot = 2; slot < SlotCount; slot++)
            {
                // Assigning through the indexer replaces one entry and notifies for that entry
                // alone, so only the container whose game changed is updated. Clearing and
                // refilling the collection would regenerate the whole row on every keypress.
                UpcomingGames[slot - 2] = GameForSlot(slot);
            }
        }

        /// <summary>
        /// Decodes the artwork around this list's window before it is needed. Returns
        /// immediately; the decoding happens on the thread pool. Without it the first view of a
        /// game decodes its box art on the UI thread, inside the slot assignments above.
        ///
        /// Deliberately not called from RefreshGames. Every list refreshes its slots when it is
        /// built, and a library has hundreds of lists across the eight category sets - warming
        /// there would queue tens of thousands of decodes at startup for rows nobody is looking
        /// at. Only the two lists actually on screen are worth warming, so the callers are the
        /// navigation methods and the view model when it changes which lists are displayed.
        /// </summary>
        public void WarmRowImages()
        {
            if (gameCycle == null)
            {
                return;
            }

            RowImageDecoder.Instance.WarmWindow(MatchingGames, gameCycle.GetIndexValue(0), SlotCount);
        }

        /// <summary>
        /// The games this list currently has on screen, in the order the media pump should
        /// hydrate them: the selected game first, because its video, clear logo and bezel are
        /// what the user is actually looking at, then the row ahead of it, then the single slot
        /// behind it.
        ///
        /// Entries can be null - the slot behind the selection is empty at the top of a list,
        /// and a list shorter than the row leaves gaps unless repeat-to-fill is on.
        /// </summary>
        public IReadOnlyList<GameMatch> SlotGamesInHydrationOrder()
        {
            GameMatch[] slots = SnapshotSlots();
            List<GameMatch> ordered = new List<GameMatch>(SlotCount);

            for (int slot = 1; slot < SlotCount; slot++)
            {
                ordered.Add(slots[slot]);
            }
            ordered.Add(slots[0]);

            return ordered;
        }

        /// <summary>
        /// The game for a slot, or null where the list is too short to fill it. Repeat-to-fill
        /// wraps the list round instead of leaving a gap (RULE-BROWSE-016).
        /// </summary>
        private GameMatch GameForSlot(int slot)
        {
            // A list with no games at all has nothing to wrap round, so repeat-to-fill cannot
            // apply. The old form of this check would have indexed an empty list; no list
            // reaches here empty today, but the guard costs nothing and the crash was real.
            if (MatchCount == 0)
            {
                return null;
            }

            if ((MatchCount > slot - 1) || EclipseSettings.RepeatGamesToFillScreen)
            {
                return gameCycle.GetItem(slot);
            }

            return null;
        }

        // Identity and ordering. The raw category value - a genre name, a platform, a release
        // year, or a custom list's name - with nothing appended to it. Lists are sorted by this
        // and position restoration finds its way back to a list by matching it after a rebuild.
        //
        // ListDescription below is the display form of the same thing and is deliberately not
        // usable for either job: it carries the game count when ShowGameCountInList is on, so it
        // changes whenever the list's membership changes - which is exactly when restoration
        // needs to recognise the list.
        private string listTypeValue;
        public string ListTypeValue
        {
            get { return listTypeValue; }
            set
            {
                if (listTypeValue != value)
                {
                    listTypeValue = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("ListTypeValue"));
                }
            }
        }

        private string listDescription;
        public string ListDescription
        {
            get { return listDescription; }
            set
            {
                if (listDescription != value)
                {
                    listDescription = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("ListDescription"));
                }
            }
        }

        // A recogniser confidence, and the two numbers a voice search ranks its lists by, used to
        // live here - on the class every browse category is made of, so a genre list carried
        // them too. They were only ever read by VoiceSearchResultBuilder, which had already
        // computed everything they were derived from; it now keeps them itself, and a fourth,
        // MaxTitleMatchType, turned out never to have been read at all.

        public int MatchCount
        {
            get
            {
                if (MatchingGames == null)
                {
                    return 0;
                }

                return MatchingGames.Count();
            }
        }

        // The window of games on screen, as three named parts rather than thirteen numbered
        // properties. The cycle still supplies thirteen slots; what changed is that the row no
        // longer needs a property per slot, and the two slots that mean something specific say
        // so in their names.
        //
        // PreviousGame is the dimmed slot behind the selection, SelectedGame is the one the
        // details pane and every game action read, and UpcomingGames is the rest of the row.
        // Previously these were Game0, Game1 and Game2..Game12 - names that said where a game
        // sat rather than what it was.

        private GameMatch previousGame;
        public GameMatch PreviousGame
        {
            get { return previousGame; }
            set
            {
                if (previousGame != value)
                {
                    previousGame = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("PreviousGame"));
                }
            }
        }

        private GameMatch selectedGame;
        public GameMatch SelectedGame
        {
            get { return selectedGame; }
            set
            {
                if (selectedGame != value)
                {
                    selectedGame = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedGame"));
                }
            }
        }

        /// <summary>
        /// The row after the selected game. Fixed length - the entries are replaced in place
        /// rather than the collection being rebuilt, so a move updates only the item containers
        /// whose game actually changed instead of regenerating the row.
        /// </summary>
        public ObservableCollection<GameMatch> UpcomingGames { get; } =
            new ObservableCollection<GameMatch>(new GameMatch[UpcomingSlotCount]);
    }
}
