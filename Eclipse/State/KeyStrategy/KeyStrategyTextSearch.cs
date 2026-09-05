namespace Eclipse.State.KeyStrategy
{
    /// <summary>
    /// Opens the text search screen from a page key.
    ///
    /// Valid in the same states as voice search, and for the same reason: those are the screens
    /// the user can be looking at with a game selected. It is deliberately absent from the search
    /// screen itself - inside search the page keys mean Backspace and commit (RULE-SEARCH-042),
    /// which TextSearchState handles directly rather than through this cache.
    /// </summary>
    public class KeyStrategyTextSearch : IKeyStrategy
    {
        public void DoKeyFunction(EclipseStateContext eclipseStateContext, EclipseState eclipseState)
        {
            if (IsValidForState(eclipseState))
            {
                eclipseStateContext.DoTextSearch();
            }
        }

        public bool IsValidForState(EclipseState eclipseState)
        {
            bool isValidforState = false;

            if ((eclipseState is FeatureOptionMoreInfoState)
                || (eclipseState is FeatureOptionPlayState)
                || (eclipseState is GameDetailOptionsState)
                || (eclipseState is SelectingGameState)
                || (eclipseState is SelectingOptionsState))
            {
                isValidforState = true;
            }

            return isValidforState;
        }
    }
}
