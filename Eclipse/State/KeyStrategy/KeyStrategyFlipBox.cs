namespace Eclipse.State.KeyStrategy
{

    public class KeyStrategyFlipBox : IKeyStrategy
    {
        public void DoKeyFunction(EclipseStateContext eclipseStateContext, EclipseState eclipseState)
        {
            if (IsValidForState(eclipseState))
            {
                var backImage = eclipseStateContext.MainWindowViewModel.CurrentGameList.SelectedGame.GameFiles.BackImage;
                var bigBackImage = eclipseStateContext.MainWindowViewModel.CurrentGameList.SelectedGame.GameFiles.BigBackImage;

                eclipseStateContext.MainWindowViewModel.CurrentGameList.SelectedGame.GameFiles.BackImage =
                    eclipseStateContext.MainWindowViewModel.CurrentGameList.SelectedGame.GameFiles.FrontImage;

                eclipseStateContext.MainWindowViewModel.CurrentGameList.SelectedGame.GameFiles.BigBackImage =
                    eclipseStateContext.MainWindowViewModel.CurrentGameList.SelectedGame.GameFiles.BigFrontImage;

                eclipseStateContext.MainWindowViewModel.CurrentGameList.SelectedGame.GameFiles.FrontImage = backImage;
                eclipseStateContext.MainWindowViewModel.CurrentGameList.SelectedGame.GameFiles.BigFrontImage = bigBackImage;
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
