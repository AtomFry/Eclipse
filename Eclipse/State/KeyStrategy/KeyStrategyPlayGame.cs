namespace Eclipse.State.KeyStrategy
{
    public class KeyStrategyPlayGame : IKeyStrategy
    {
        public void DoKeyFunction(EclipseStateContext eclipseStateContext, EclipseState eclipseState)
        {
            if (IsValidForState(eclipseState))
            {
                eclipseStateContext.MainWindowViewModel.PlayCurrentGame();
            }
        }

        public bool IsValidForState(EclipseState eclipseState)
        {
            bool isValidforState = false;

            if (eclipseState is SelectingGameState)
            {
                isValidforState = true;
            }

            return isValidforState;
        }
    }
}
