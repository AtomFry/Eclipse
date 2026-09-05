using System.Windows.Controls;

namespace Eclipse.View
{
    /// <summary>
    /// The text search screen's markup, and nothing else.
    ///
    /// Deliberately empty of logic. Everything the screen shows is a binding onto
    /// SearchViewModel, which is itself a projection of SearchSession - so there is nothing here
    /// to get wrong and nothing that would need a test. The view renders state and forwards
    /// nothing: input reaches search through the ordinary route, MainWindowView to
    /// MainWindowViewModel to EclipseStateContext to TextSearchState.
    ///
    /// The one thing worth knowing: DataContext is set by the host in MainWindowView, to the
    /// SearchViewModel rather than to the whole theme's view model. Every binding in the markup
    /// is therefore against search state directly.
    ///
    /// MANUAL VERIFICATION. What cannot reasonably be unit tested here is whether the thing
    /// looks right and reads at television distance. See the scenarios listed in
    /// docs/plans/text-search.md 12.2 - VER-SEARCH-015, 016 and 017 - plus:
    ///
    ///   * the keyboard fills its area in both layouts, and the selected key is unmistakable
    ///     from across a room
    ///   * the result row updates while typing without visible stutter
    ///   * the selected result's title is legible and long titles wrap rather than clip
    ///   * the screen covers what was behind it, and leaves nothing behind on the way out
    /// </summary>
    public partial class SearchView : UserControl
    {
        public SearchView()
        {
            InitializeComponent();
        }
    }
}
