using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Eclipse.Models
{
    /// <summary>What pressing a key does. Character keys type; the rest act on the query.</summary>
    public enum SearchKeyKind
    {
        Character,
        Space,
        Backspace,
        Clear
    }

    /// <summary>
    /// One key on the on-screen keyboard.
    ///
    /// Carries its own grid position so the view can lay the keyboard out from the model rather
    /// than duplicating the arrangement in markup - the two layouts have different shapes, and a
    /// hand-written grid would have to be kept in step with both.
    /// </summary>
    public sealed class SearchKey : INotifyPropertyChanged
    {
        public SearchKey(SearchKeyKind kind, string label, int row, int column, char character = '\0')
        {
            Kind = kind;
            Label = label;
            Row = row;
            Column = column;
            Character = character;
        }

        public SearchKeyKind Kind { get; }

        /// <summary>What the key shows. Not the character - the function keys have words.</summary>
        public string Label { get; }

        /// <summary>The character a Character key types. '\0' for every other kind.</summary>
        public char Character { get; }

        /// <summary>Where the cursor has to be for this key to be the selected one.</summary>
        public int Row { get; }
        public int Column { get; }

        private bool selected;

        /// <summary>Whether the cursor is on this key. The only mutable thing about a key.</summary>
        public bool Selected
        {
            get { return selected; }
            set
            {
                if (selected != value)
                {
                    selected = value;
                    PropertyChanged(this, new PropertyChangedEventArgs("Selected"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public override string ToString()
        {
            return $"{Kind}:{Label} ({Row},{Column})";
        }
    }

    /// <summary>
    /// The on-screen keyboard: which keys there are, where they sit, and where the cursor is.
    ///
    /// Separate from SearchSession on purpose. This is cursor arithmetic over a grid and nothing
    /// else - no query, no results, no index - so the movement rules can be tested exhaustively
    /// without simulating a keystroke, and the session can be tested without simulating a
    /// keyboard.
    ///
    /// ROWS ARE RAGGED. The alphabetical layout is a tidy six by six, but QWERTY is not a grid
    /// and forcing it into one produces something that is not QWERTY and therefore has no reason
    /// to exist. Rows carry their own length and vertical movement clamps into the target row,
    /// remembering the column the user was aiming at so that travelling down through a short row
    /// and back up returns them where they started - the same thing a text editor does.
    ///
    /// See docs/plans/text-search.md 4.3.
    /// </summary>
    public sealed class SearchKeyboard
    {
        private readonly List<List<SearchKey>> rows;

        // Where the cursor actually is.
        private int row;
        private int column;

        // The column the user was last aiming at. Vertical movement clamps into a short row for
        // display but keeps aiming here, so passing through a short row does not drag the cursor
        // sideways permanently.
        private int desiredColumn;

        public SearchKeyboard(SearchKeyboardLayout layout)
        {
            Layout = layout;
            rows = Build(layout);
            Rows = rows.ConvertAll(keyRow => (IReadOnlyList<SearchKey>)keyRow);
            SelectedKey.Selected = true;
        }

        public SearchKeyboardLayout Layout { get; }

        /// <summary>
        /// The keys, a row at a time - which is how the view lays them out.
        ///
        /// Rows rather than a flat list with grid coordinates, because the rows are ragged and
        /// the keys within a row simply share its width. That gives the function row's three
        /// keys their extra width for free, and it means a QWERTY row of seven and a number row
        /// of ten both fill the keyboard without anything having to compute a span.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<SearchKey>> Rows { get; }

        /// <summary>Every key, flattened. For tests and for anything that wants the whole set.</summary>
        public IReadOnlyList<SearchKey> Keys
        {
            get
            {
                List<SearchKey> all = new List<SearchKey>();
                foreach (List<SearchKey> keyRow in rows)
                {
                    all.AddRange(keyRow);
                }
                return all;
            }
        }

        public int RowCount => rows.Count;

        public SearchKey SelectedKey => rows[row][column];

        public int SelectedRow => row;
        public int SelectedColumn => column;

        /// <summary>Whether the cursor is on the top row - so a caller knows Up would leave.</summary>
        public bool IsOnFirstRow => row == 0;

        /// <summary>Whether the cursor is on the bottom row - so a caller knows Down would leave.</summary>
        public bool IsOnLastRow => row == rows.Count - 1;

        /// <summary>
        /// Moves left, wrapping within the row.
        ///
        /// Wraps because in stage 2 no zone sits beside the keyboard. RULE-SEARCH-030 says
        /// horizontal movement does not wrap where an adjacent zone lies that way - which
        /// becomes true when the suggestion column arrives in stage 4, and is the point at which
        /// this grows a "would leave" return value like the vertical moves have.
        /// </summary>
        public void MoveLeft()
        {
            Select(row, column == 0 ? rows[row].Count - 1 : column - 1);
        }

        /// <summary>Moves right, wrapping within the row. See MoveLeft.</summary>
        public void MoveRight()
        {
            Select(row, column == rows[row].Count - 1 ? 0 : column + 1);
        }

        /// <summary>
        /// Moves up a row. False if the cursor is already on the top row, which is the caller's
        /// cue to leave the keyboard for the zone above - the keyboard itself has no opinion
        /// about what is up there.
        /// </summary>
        public bool MoveUp()
        {
            if (IsOnFirstRow)
            {
                return false;
            }

            SelectAiming(row - 1);
            return true;
        }

        /// <summary>Moves down a row. False on the bottom row. See MoveUp.</summary>
        public bool MoveDown()
        {
            if (IsOnLastRow)
            {
                return false;
            }

            SelectAiming(row + 1);
            return true;
        }

        /// <summary>Wraps to the top row, keeping the aimed-at column. For zone cycling.</summary>
        public void MoveToFirstRow()
        {
            SelectAiming(0);
        }

        /// <summary>Wraps to the bottom row, keeping the aimed-at column. For zone cycling.</summary>
        public void MoveToLastRow()
        {
            SelectAiming(rows.Count - 1);
        }

        private void SelectAiming(int newRow)
        {
            int target = Math.Min(desiredColumn, rows[newRow].Count - 1);

            SelectedKey.Selected = false;
            row = newRow;
            column = target;
            SelectedKey.Selected = true;

            // deliberately not updating desiredColumn - that is what makes a short row a
            // pass-through rather than a place the cursor gets stuck
        }

        private void Select(int newRow, int newColumn)
        {
            SelectedKey.Selected = false;
            row = newRow;
            column = newColumn;
            desiredColumn = newColumn;
            SelectedKey.Selected = true;
        }

        private static List<List<SearchKey>> Build(SearchKeyboardLayout layout)
        {
            string[] letterRows = layout == SearchKeyboardLayout.Qwerty
                ? new[] { "1234567890", "qwertyuiop", "asdfghjkl", "zxcvbnm" }
                : new[] { "abcdef", "ghijkl", "mnopqr", "stuvwx", "yz0123", "456789" };

            List<List<SearchKey>> built = new List<List<SearchKey>>();

            for (int rowIndex = 0; rowIndex < letterRows.Length; rowIndex++)
            {
                List<SearchKey> keyRow = new List<SearchKey>();

                for (int columnIndex = 0; columnIndex < letterRows[rowIndex].Length; columnIndex++)
                {
                    char character = letterRows[rowIndex][columnIndex];
                    keyRow.Add(new SearchKey(SearchKeyKind.Character,
                                             char.ToUpperInvariant(character).ToString(),
                                             rowIndex,
                                             columnIndex,
                                             character));
                }

                built.Add(keyRow);
            }

            // The function row. Space, backspace and clear are keys because the input contract
            // has nowhere else to put them - Eclipse sees eight inputs and none of them is a
            // second button. Being three keys in a row that others fill with seven to ten is
            // what makes them wide; nothing has to say so.
            int functionRow = built.Count;

            built.Add(new List<SearchKey>
            {
                new SearchKey(SearchKeyKind.Space, "space", functionRow, 0),
                new SearchKey(SearchKeyKind.Backspace, "delete", functionRow, 1),
                new SearchKey(SearchKeyKind.Clear, "clear", functionRow, 2)
            });

            return built;
        }
    }
}
