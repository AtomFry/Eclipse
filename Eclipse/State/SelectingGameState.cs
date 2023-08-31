using Eclipse.Service;
using Eclipse.State.KeyStrategy;

namespace Eclipse.State
{
    public class SelectingGameState : EclipseState
    {
        private readonly AttractModeService attractModeService;

        public SelectingGameState()
        {
            attractModeService = AttractModeService.Instance;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.IsPickingCategory = false;
            eclipseStateContext.MainWindowViewModel.IsDisplayingFeature = false;
            eclipseStateContext.MainWindowViewModel.IsDisplayingMoreInfo = false;
            eclipseStateContext.MainWindowViewModel.IsDisplayingResults = true;
            eclipseStateContext.MainWindowViewModel.CallGameChangeFunction();
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.CycleListForward();
            return true;
        }

        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            if (EclipseSettingsDataProvider.Instance.EclipseSettings.BypassDetails)
            {
                attractModeService.StopAttractMode();
                eclipseStateContext.MainWindowViewModel.PlayCurrentGame();
            }
            else
            {
                attractModeService.RestartAttractMode();
                eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(GameDetailOptionPlayState)));
            }

            return true;
        }

        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            if (EclipseSettingsDataProvider.Instance.EclipseSettings.DisplayOptionsOnEscape)
            {
                eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingOptionsState)));
                return true;
            }

            return false;
        }

        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            if (EclipseSettingsDataProvider.Instance.EclipseSettings.OpenSettingsPaneOnLeft
                && eclipseStateContext.MainWindowViewModel.CurrentGameList.CurrentGameIndex == 0
                && !held)
            {
                // first game and going left and not held down, open the options 
                eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingOptionsState)));
                return true;
            }

            if (!EclipseSettingsDataProvider.Instance.EclipseSettings.OpenSettingsPaneOnLeft
                || eclipseStateContext.MainWindowViewModel.CurrentGameList.CurrentGameIndex != 0)
            {
                eclipseStateContext.MainWindowViewModel.CurrentGameList.CycleBackward();
                eclipseStateContext.MainWindowViewModel.CallGameChangeFunction();
                return true;
            }

            return true;
        }

        public bool OnPageDown(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            KeyStrategyCache.Instance.PageDownStrategy.DoKeyFunction(eclipseStateContext, this);
            return true;
        }

        public bool OnPageUp(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            KeyStrategyCache.Instance.PageUpStrategy.DoKeyFunction(eclipseStateContext, this);
            return true;
        }

        public bool OnRight(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.CurrentGameList.CycleForward();

            eclipseStateContext.MainWindowViewModel.CallGameChangeFunction();
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            // if displaying first list and featured game is selected, display featured game
            if (EclipseSettingsDataProvider.Instance.EclipseSettings.DisplayFeaturedGame
                && eclipseStateContext.MainWindowViewModel.listCycle.GetIndexValue(0) == 0)
            {
                eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(FeatureOptionPlayState))); 
                return true;
            }

            eclipseStateContext.MainWindowViewModel.CycleListBackward();
            return true;
        }
    }
}