using Eclipse.Service;
using Eclipse.View;
using System;
using System.Collections.Generic;

namespace Eclipse.State
{
    public class EclipseStateContext
    {
        private Dictionary<Type, EclipseState> _stateCache = new Dictionary<Type, EclipseState>();

        public EclipseState GetState(Type type)
        {
            if (!_stateCache.ContainsKey(type))
            {
                EclipseState instance = Activator.CreateInstance(type) as EclipseState;
                _stateCache.Add(type, instance);
            }
            return _stateCache[type];
        }

        public EclipseState CurrentState { get; private set; }

        public MainWindowViewModel MainWindowViewModel { get; private set; }

        public EclipseStateContext(MainWindowViewModel viewModel)
        {
            MainWindowViewModel = viewModel;

            TransitionToState(GetState(typeof(LoadingState)));
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

        public void DoVoiceSearch()
        {
            if (EclipseSettingsDataProvider.Instance.EclipseSettings.EnableVoiceSearch)
            {
                TransitionToState(GetState(typeof(VoiceRecognitionState)));
            }
        }
    }
}