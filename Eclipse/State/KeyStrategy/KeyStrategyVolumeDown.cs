namespace Eclipse.State.KeyStrategy
{
    public class KeyStrategyVolumeDown : IKeyStrategy
    {
        public void DoKeyFunction(EclipseStateContext eclipseStateContext, EclipseState eclipseState)
        {
            eclipseStateContext.MainWindowViewModel.CallAdjustVideoVolumeFunction(-0.1);
        }

        public bool IsValidForState(EclipseState eclipseState)
        {
            // adjusting the volume is valid for any state
            return true;
        }
    }
}
