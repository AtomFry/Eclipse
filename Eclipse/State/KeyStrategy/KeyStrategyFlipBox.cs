namespace Eclipse.State.KeyStrategy
{

    public class KeyStrategyFlipBox : IKeyStrategy
    {
        public void DoKeyFunction(EclipseStateContext eclipseStateContext, EclipseState eclipseState)
        {
            if (IsValidForState(eclipseState))
            {
                var backImage = eclipseStateContext.MainWindowViewModel.CurrentGameList.Game1.GameFiles.BackImage;
                var bigBackImage = eclipseStateContext.MainWindowViewModel.CurrentGameList.Game1.GameFiles.BigBackImage;

                eclipseStateContext.MainWindowViewModel.CurrentGameList.Game1.GameFiles.BackImage =
                    eclipseStateContext.MainWindowViewModel.CurrentGameList.Game1.GameFiles.FrontImage;

                eclipseStateContext.MainWindowViewModel.CurrentGameList.Game1.GameFiles.BigBackImage =
                    eclipseStateContext.MainWindowViewModel.CurrentGameList.Game1.GameFiles.BigFrontImage;

                eclipseStateContext.MainWindowViewModel.CurrentGameList.Game1.GameFiles.FrontImage = backImage;
                eclipseStateContext.MainWindowViewModel.CurrentGameList.Game1.GameFiles.BigFrontImage = bigBackImage;
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
