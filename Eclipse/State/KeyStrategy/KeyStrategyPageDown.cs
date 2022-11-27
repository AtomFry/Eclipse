using Eclipse.Helpers;

namespace Eclipse.State.KeyStrategy
{
    public class KeyStrategyPageDown : IKeyStrategy
    {
        public void DoKeyFunction(EclipseStateContext eclipseStateContext, EclipseState eclipseState)
        {
            if (IsValidForState(eclipseState))
            {
                // get the index of the current game in the list 
                int currentIndex =
                    eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetStartIndex +
                    eclipseStateContext.MainWindowViewModel.CurrentGameList.CurrentGameIndex;

                // get the number of games in the list 
                int gamesInList =
                    eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetEndIndex -
                    eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetStartIndex + 1;

                int pageAmount = EclipseConstants.GamesToPage;
                if (gamesInList <= pageAmount)
                {
                    // for lists shorter than the page jump, move half of the list length
                    pageAmount = gamesInList / 2;
                }

                int nextIndex = currentIndex;

                // for lists longer than the page jump, cycle forward 
                for (int i = 0; i < pageAmount; i++)
                {
                    nextIndex++;
                    if (nextIndex > eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetEndIndex)
                    {
                        nextIndex = eclipseStateContext.MainWindowViewModel.CurrentGameList.ListSetStartIndex;
                    }
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
