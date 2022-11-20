namespace Eclipse.State.KeyStrategy
{
    public class KeyStrategyRandomGame : IKeyStrategy
    {
        public void DoKeyFunction(EclipseStateContext eclipseStateContext, EclipseState eclipseState)
        {
            if (IsValidForState(eclipseState))
            {
                eclipseStateContext.MainWindowViewModel.DoRandomGame();
                eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            }
        }

        public bool IsValidForState(EclipseState eclipseState)
        {
            bool isValidforState = false;

            if ((eclipseState is FeatureOptionMoreInfoState)
                || (eclipseState is FeatureOptionPlayState)
                || (eclipseState is GameDetailOptionFavoriteState)
                || (eclipseState is GameDetailOptionMoreState)
                || (eclipseState is GameDetailOptionPlayState)
                || (eclipseState is GameDetailOptionRatingState)
                || (eclipseState is SelectingGameState)
                || (eclipseState is SelectingOptionsState))
            {
                isValidforState = true;
            }

            return isValidforState;
        }

    }
}
