namespace Eclipse.State.KeyStrategy
{

    public class KeyStrategyPageUp : IKeyStrategy
    {
        public void DoKeyFunction(EclipseStateContext eclipseStateContext, EclipseState eclipseState)
        {
            if (IsValidForState(eclipseState))
            {
                int currentIndex = eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetStartIndex +
                                    eclipseStateContext.MainWindowViewModel.CurrentGameList.CurrentGameIndex;

                int nextIndex = currentIndex - 7;

                if (nextIndex < eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetStartIndex)
                {
                    nextIndex = eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetEndIndex;
                }

                eclipseStateContext.MainWindowViewModel.DoRandomGame(nextIndex);
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
