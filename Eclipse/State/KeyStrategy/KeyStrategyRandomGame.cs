namespace Eclipse.State.KeyStrategy
{
    public class KeyStrategyRandomGame : IKeyStrategy
    {
        public void DoKeyFunction(EclipseStateContext eclipseStateContext, EclipseState eclipseState)
        {
            if (IsValidForState(eclipseState))
            {
                eclipseStateContext.MainWindowViewModel.Navigator.MoveToRandomGame();
                eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
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
