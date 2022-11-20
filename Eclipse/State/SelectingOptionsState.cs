using Eclipse.Models;
using Eclipse.Service;
using Eclipse.State.KeyStrategy;
using System;

namespace Eclipse.State
{
    public class SelectingOptionsState : EclipseState
    {
        private readonly AttractModeService attractModeService;

        public SelectingOptionsState()
        {
            attractModeService = AttractModeService.Instance;
        }

        public void EnterState(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.IsPickingCategory = true;
        }

        public bool OnDown(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.OptionList.CycleForward();
            return true;
        }

        public bool OnEnter(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();

            Option<ListCategoryType> option = eclipseStateContext.MainWindowViewModel.OptionList.SelectedOption;
            switch (option.EnumOption)
            {
                case ListCategoryType.VoiceSearch:
                    eclipseStateContext.DoVoiceSearch();
                    break;

                case ListCategoryType.RandomGame:
                    eclipseStateContext.MainWindowViewModel.DoRandomGame();
                    eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
                    break;

                case ListCategoryType.ReleaseYear:
                    eclipseStateContext.MainWindowViewModel.ResetGameLists(ListCategoryType.ReleaseYear);
                    eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
                    break;

                case ListCategoryType.Platform:
                    eclipseStateContext.MainWindowViewModel.ResetGameLists(ListCategoryType.Platform);
                    eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));

                    break;

                case ListCategoryType.Developer:
                    eclipseStateContext.MainWindowViewModel.ResetGameLists(ListCategoryType.Developer);
                    eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
                    break;

                case ListCategoryType.Genre:
                    eclipseStateContext.MainWindowViewModel.ResetGameLists(ListCategoryType.Genre);
                    eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
                    break;

                case ListCategoryType.Playlist:
                    eclipseStateContext.MainWindowViewModel.ResetGameLists(ListCategoryType.Playlist);
                    eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
                    break;

                case ListCategoryType.PlayMode:
                    eclipseStateContext.MainWindowViewModel.ResetGameLists(ListCategoryType.PlayMode);
                    eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
                    break;

                case ListCategoryType.Publisher:
                    eclipseStateContext.MainWindowViewModel.ResetGameLists(ListCategoryType.Publisher);
                    eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
                    break;

                case ListCategoryType.Series:
                    eclipseStateContext.MainWindowViewModel.ResetGameLists(ListCategoryType.Series);
                    eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
                    break;
            }
            eclipseStateContext.MainWindowViewModel.IsPickingCategory = false;
            return true;
        }

        public bool OnEscape(EclipseStateContext eclipseStateContext)
        {
            attractModeService.RestartAttractMode();
            // go to big box options
            return false;
        }

        public bool OnLeft(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.CurrentGameList.CycleBackward();
            eclipseStateContext.MainWindowViewModel.IsPickingCategory = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
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
            eclipseStateContext.MainWindowViewModel.IsPickingCategory = false;
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
            return true;
        }

        public bool OnUp(EclipseStateContext eclipseStateContext, bool held)
        {
            attractModeService.RestartAttractMode();
            eclipseStateContext.MainWindowViewModel.OptionList.CycleBackward();
            return true;
        }
    }
}
