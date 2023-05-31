namespace Eclipse.State.KeyStrategy
{
    public class KeyStrategyDisplayDetails : IKeyStrategy
    {
        public void DoKeyFunction(EclipseStateContext eclipseStateContext, EclipseState eclipseState)
        {
            if (IsValidForState(eclipseState))
            {
                eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(GameDetailOptionPlayState)));
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
