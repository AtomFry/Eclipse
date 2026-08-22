using Eclipse.Models;
using Eclipse.Service;
using System.Linq;
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

            // Two of the options do something of their own; the other eight all mean the same
            // thing - browse by this category, then go back to the row. They used to be eight
            // copies of the same two lines, one per category.
            ListCategoryType category = eclipseStateContext.MainWindowViewModel.OptionList.SelectedOption.EnumOption;

            if (category == ListCategoryType.VoiceSearch)
            {
                eclipseStateContext.DoVoiceSearch();
            }
            else if (category == ListCategoryType.RandomGame)
            {
                eclipseStateContext.MainWindowViewModel.Navigator.MoveToRandomGame();
                GoBackToBrowsing(eclipseStateContext);
            }
            else if (GameListBuilder.BrowsableCategories.Contains(category))
            {
                eclipseStateContext.MainWindowViewModel.Navigator.ShowCategory(category);
                GoBackToBrowsing(eclipseStateContext);
            }

            eclipseStateContext.MainWindowViewModel.IsPickingCategory = false;
            return true;
        }

        private static void GoBackToBrowsing(EclipseStateContext eclipseStateContext)
        {
            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
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

            eclipseStateContext.MainWindowViewModel.Navigator.MoveToPreviousGame();
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
