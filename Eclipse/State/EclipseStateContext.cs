using Eclipse.View;

namespace Eclipse.State
{
    public class EclipseStateContext
    {
        public EclipseState CurrentState { get; private set; }

        public MainWindowViewModel MainWindowViewModel { get; private set; }

        public EclipseStateContext(MainWindowViewModel viewModel)
        {
            MainWindowViewModel = viewModel;
            TransitionToState(new LoadingState());
        }

        public void TransitionToState(EclipseState newState)
        {
            CurrentState = newState;
            CurrentState.EnterState(this);
        }

        public bool OnUp(bool held)
        {
            return CurrentState.OnUp(this, held);
        }

        public bool OnDown(bool held)
        {
            return CurrentState.OnDown(this, held);
        }

        public bool OnLeft(bool held)
        {
            return CurrentState.OnLeft(this, held);
        }

        public bool OnRight(bool held)
        {
            return CurrentState.OnRight(this, held);
        }

        public bool OnPageUp()
        {
            return CurrentState.OnPageUp(this);    
        }

        public bool OnPageDown()
        {
            return CurrentState.OnPageDown(this);
        }

        public bool OnEnter()
        {
            return CurrentState.OnEnter(this);
        }

        public bool OnEscape()
        {
            return CurrentState.OnEscape(this);
        }
    }
}