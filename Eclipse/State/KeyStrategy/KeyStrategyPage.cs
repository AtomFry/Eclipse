
namespace Eclipse.State.KeyStrategy
{
    // Jumps the selection a page through the current list. One class for both directions - the
    // two used to be separate files differing only in ++ versus -- and which end they wrapped at.
    //
    // Which physical key does this, if any, is a user setting: PageUpFunction and
    // PageDownFunction bind each key to one of ten functions. That is why the state-validity
    // check lives here rather than in the states - a state with a page key cannot know what the
    // key is bound to, so each function has to declare where it applies. Paging only means
    // anything while browsing the row, so that is the only state it accepts.
    public class KeyStrategyPage : IKeyStrategy
    {
        // Forward is towards the end of the list, matching what the Page Down key is called.
        public static KeyStrategyPage Forward()
        {
            return new KeyStrategyPage(1);
        }

        public static KeyStrategyPage Backward()
        {
            return new KeyStrategyPage(-1);
        }

        private readonly int direction;

        private KeyStrategyPage(int direction)
        {
            this.direction = direction;
        }

        public void DoKeyFunction(EclipseStateContext eclipseStateContext, EclipseState eclipseState)
        {
            if (!IsValidForState(eclipseState))
            {
                return;
            }

            if (direction > 0)
            {
                eclipseStateContext.MainWindowViewModel.Navigator.PageForward();
            }
            else
            {
                eclipseStateContext.MainWindowViewModel.Navigator.PageBackward();
            }
        }

        public bool IsValidForState(EclipseState eclipseState)
        {
            return eclipseState is SelectingGameState;
        }
    }
}
