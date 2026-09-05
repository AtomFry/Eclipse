using System.Linq;
using Eclipse.Models;
using Xunit;

namespace Eclipse.Tests.Search
{
    /// <summary>
    /// VER-SEARCH-014 - keyboard cursor movement, tested without a single WPF key event.
    ///
    /// This is the part of the search screen a user spends the most button presses on, so it is
    /// worth pinning exhaustively: every character reachable, movement wrapping where the plan
    /// says it wraps, and vertical movement reporting when it would leave the grid rather than
    /// deciding for itself what is above or below.
    /// </summary>
    public class SearchKeyboardTests
    {
        // ------------------------------------------------------------------
        // Layout.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(SearchKeyboardLayout.Alphabetical)]
        [InlineData(SearchKeyboardLayout.Qwerty)]
        public void Every_letter_and_digit_is_reachable(SearchKeyboardLayout layout)
        {
            SearchKeyboard keyboard = new SearchKeyboard(layout);

            string typed = new string(keyboard.Keys
                                              .Where(key => key.Kind == SearchKeyKind.Character)
                                              .Select(key => key.Character)
                                              .OrderBy(character => character)
                                              .ToArray());

            Assert.Equal("0123456789abcdefghijklmnopqrstuvwxyz", typed);
        }

        [Theory]
        [InlineData(SearchKeyboardLayout.Alphabetical)]
        [InlineData(SearchKeyboardLayout.Qwerty)]
        public void Space_backspace_and_clear_are_keys_on_the_grid(SearchKeyboardLayout layout)
        {
            SearchKeyboard keyboard = new SearchKeyboard(layout);

            Assert.Single(keyboard.Keys, key => key.Kind == SearchKeyKind.Space);
            Assert.Single(keyboard.Keys, key => key.Kind == SearchKeyKind.Backspace);
            Assert.Single(keyboard.Keys, key => key.Kind == SearchKeyKind.Clear);
        }

        /// <summary>
        /// The plan's diagram: six columns by six rows for the characters, then a function row.
        /// </summary>
        [Fact]
        public void The_alphabetical_layout_is_six_by_six_plus_a_function_row()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Alphabetical);

            Assert.Equal(7, keyboard.RowCount);
            Assert.Equal(39, keyboard.Keys.Count);
            Assert.Equal(new[] { 6, 6, 6, 6, 6, 6, 3 }, keyboard.Rows.Select(row => row.Count));
        }

        /// <summary>
        /// QWERTY keeps its real ragged shape rather than being forced into six columns, because
        /// a QWERTY that is not QWERTY has no reason to exist.
        /// </summary>
        [Fact]
        public void The_qwerty_layout_keeps_its_real_ragged_shape()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Qwerty);

            Assert.Equal(5, keyboard.RowCount);
            Assert.Equal(39, keyboard.Keys.Count);
            Assert.Equal(new[] { 10, 10, 9, 7, 3 }, keyboard.Rows.Select(row => row.Count));

            Assert.Equal("QWERTYUIOP", new string(keyboard.Keys.Where(key => key.Row == 1)
                                                               .Select(key => key.Label[0])
                                                               .ToArray()));
        }

        [Fact]
        public void The_alphabetical_layout_reads_in_alphabetical_order()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Alphabetical);

            Assert.Equal("ABCDEF", RowLabels(keyboard, 0));
            Assert.Equal("GHIJKL", RowLabels(keyboard, 1));
            Assert.Equal("YZ0123", RowLabels(keyboard, 4));
        }

        private static string RowLabels(SearchKeyboard keyboard, int row)
        {
            return new string(keyboard.Keys.Where(key => key.Row == row).Select(key => key.Label[0]).ToArray());
        }

        // ------------------------------------------------------------------
        // The cursor.
        // ------------------------------------------------------------------

        [Fact]
        public void The_cursor_starts_on_the_first_key()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Alphabetical);

            Assert.Equal('a', keyboard.SelectedKey.Character);
            Assert.True(keyboard.SelectedKey.Selected);
        }

        [Fact]
        public void Exactly_one_key_is_selected_at_any_time()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Alphabetical);

            keyboard.MoveRight();
            keyboard.MoveDown();
            keyboard.MoveLeft();

            Assert.Single(keyboard.Keys, key => key.Selected);
            Assert.True(keyboard.SelectedKey.Selected);
        }

        [Fact]
        public void Moving_right_walks_along_the_row()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Alphabetical);

            keyboard.MoveRight();
            Assert.Equal('b', keyboard.SelectedKey.Character);

            keyboard.MoveRight();
            Assert.Equal('c', keyboard.SelectedKey.Character);
        }

        /// <summary>
        /// RULE-SEARCH-030 - horizontal movement wraps while nothing sits beside the keyboard,
        /// which is the whole of stage 2. The suggestion column that changes this arrives in
        /// stage 4.
        /// </summary>
        [Fact]
        public void Moving_left_from_the_first_key_wraps_to_the_end_of_the_row()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Alphabetical);

            keyboard.MoveLeft();

            Assert.Equal('f', keyboard.SelectedKey.Character);
            Assert.Equal(0, keyboard.SelectedRow);
        }

        [Fact]
        public void Moving_right_from_the_last_key_wraps_to_the_start_of_the_row()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Alphabetical);

            for (int move = 0; move < 5; move++)
            {
                keyboard.MoveRight();
            }
            Assert.Equal('f', keyboard.SelectedKey.Character);

            keyboard.MoveRight();
            Assert.Equal('a', keyboard.SelectedKey.Character);
        }

        [Fact]
        public void Moving_down_keeps_the_column()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Alphabetical);

            keyboard.MoveRight();
            keyboard.MoveRight();
            Assert.Equal('c', keyboard.SelectedKey.Character);

            Assert.True(keyboard.MoveDown());
            Assert.Equal('i', keyboard.SelectedKey.Character);
        }

        // ------------------------------------------------------------------
        // Leaving the grid. The keyboard reports it rather than deciding it.
        // ------------------------------------------------------------------

        /// <summary>
        /// RULE-SEARCH-031 - at the top and bottom edges the caller takes over, because what lies
        /// above and below is a zone question and the keyboard has no zones.
        /// </summary>
        [Fact]
        public void Moving_up_from_the_top_row_reports_that_it_would_leave()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Alphabetical);

            Assert.True(keyboard.IsOnFirstRow);
            Assert.False(keyboard.MoveUp());
            Assert.Equal('a', keyboard.SelectedKey.Character);
        }

        [Fact]
        public void Moving_down_from_the_bottom_row_reports_that_it_would_leave()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Alphabetical);

            while (keyboard.MoveDown())
            {
            }

            Assert.True(keyboard.IsOnLastRow);
            Assert.False(keyboard.MoveDown());
        }

        [Fact]
        public void The_bottom_row_is_the_function_row()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Alphabetical);

            while (keyboard.MoveDown())
            {
            }

            Assert.Equal(SearchKeyKind.Space, keyboard.SelectedKey.Kind);
        }

        [Fact]
        public void The_cursor_can_be_sent_to_either_end_for_zone_cycling()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Alphabetical);

            keyboard.MoveToLastRow();
            Assert.True(keyboard.IsOnLastRow);

            keyboard.MoveToFirstRow();
            Assert.True(keyboard.IsOnFirstRow);
        }

        // ------------------------------------------------------------------
        // Ragged rows.
        // ------------------------------------------------------------------

        /// <summary>
        /// QWERTY's bottom letter row is seven keys against the number row's ten, so moving down
        /// the right-hand side has to land somewhere rather than off the end.
        /// </summary>
        [Fact]
        public void Moving_into_a_shorter_row_clamps_rather_than_falling_off_it()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Qwerty);

            for (int move = 0; move < 9; move++)
            {
                keyboard.MoveRight();
            }
            Assert.Equal('0', keyboard.SelectedKey.Character);

            keyboard.MoveDown();
            Assert.Equal('p', keyboard.SelectedKey.Character);

            keyboard.MoveDown();
            Assert.Equal('l', keyboard.SelectedKey.Character);

            keyboard.MoveDown();
            Assert.Equal('m', keyboard.SelectedKey.Character);
        }

        /// <summary>
        /// Passing through a short row must not drag the cursor sideways for good - the same
        /// thing a text editor does with a short line.
        /// </summary>
        [Fact]
        public void Passing_through_a_short_row_and_back_returns_to_the_original_column()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Qwerty);

            for (int move = 0; move < 9; move++)
            {
                keyboard.MoveRight();
            }
            Assert.Equal('0', keyboard.SelectedKey.Character);

            keyboard.MoveDown();
            keyboard.MoveDown();
            keyboard.MoveDown();
            Assert.Equal('m', keyboard.SelectedKey.Character);

            keyboard.MoveUp();
            keyboard.MoveUp();
            keyboard.MoveUp();
            Assert.Equal('0', keyboard.SelectedKey.Character);
        }

        /// <summary>
        /// Moving sideways is the user aiming somewhere new, so it replaces the remembered
        /// column rather than adding to it.
        /// </summary>
        [Fact]
        public void Moving_sideways_resets_where_the_cursor_is_aiming()
        {
            SearchKeyboard keyboard = new SearchKeyboard(SearchKeyboardLayout.Qwerty);

            for (int move = 0; move < 9; move++)
            {
                keyboard.MoveRight();
            }

            keyboard.MoveDown();
            keyboard.MoveDown();
            keyboard.MoveDown();

            // "zxcvbnm" - column 6 is m, so stepping left aims at column 5, n
            keyboard.MoveLeft();
            Assert.Equal('n', keyboard.SelectedKey.Character);

            // and aiming at 5 rather than the original 9 puts it on "asdfghjkl"[5]
            keyboard.MoveUp();
            Assert.Equal('h', keyboard.SelectedKey.Character);
        }

        /// <summary>
        /// Every key's stated coordinates must be where the cursor actually finds it, because
        /// the view draws from Rows and the cursor moves by Row and Column - two descriptions of
        /// the same arrangement that would otherwise be free to drift apart.
        /// </summary>
        [Theory]
        [InlineData(SearchKeyboardLayout.Alphabetical)]
        [InlineData(SearchKeyboardLayout.Qwerty)]
        public void Every_key_knows_where_it_actually_is(SearchKeyboardLayout layout)
        {
            SearchKeyboard keyboard = new SearchKeyboard(layout);

            for (int row = 0; row < keyboard.Rows.Count; row++)
            {
                for (int column = 0; column < keyboard.Rows[row].Count; column++)
                {
                    SearchKey key = keyboard.Rows[row][column];

                    Assert.Equal(row, key.Row);
                    Assert.Equal(column, key.Column);
                }
            }
        }

        [Theory]
        [InlineData(SearchKeyboardLayout.Alphabetical)]
        [InlineData(SearchKeyboardLayout.Qwerty)]
        public void The_rows_hold_every_key_exactly_once(SearchKeyboardLayout layout)
        {
            SearchKeyboard keyboard = new SearchKeyboard(layout);

            Assert.Equal(keyboard.Keys.Count, keyboard.Rows.Sum(row => row.Count));
            Assert.Equal(keyboard.Keys.Count, keyboard.Rows.SelectMany(row => row).Distinct().Count());
        }
    }
}
