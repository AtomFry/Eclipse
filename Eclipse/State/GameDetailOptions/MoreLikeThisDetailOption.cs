using Eclipse.Models;
using Eclipse.Service;

namespace Eclipse.State.GameDetailOptions
{
    // Rebuilds the lists around the selected game and closes the overlay to show them.
    public class MoreLikeThisDetailOption : GameDetailOptionItem
    {
        public override GameDetailOption Kind => GameDetailOption.MoreLikeThis;

        public override void Activate(EclipseStateContext eclipseStateContext)
        {
            AttractModeService.Instance.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.IsDisplayingMoreInfo = false;
            eclipseStateContext.MainWindowViewModel.DoMoreLikeCurrentGame();

            eclipseStateContext.TransitionToState(eclipseStateContext.GetState(typeof(SelectingGameState)));
        }
    }
}
