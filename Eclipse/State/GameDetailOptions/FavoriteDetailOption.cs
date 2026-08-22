using Eclipse.Models;
using Eclipse.Service;

namespace Eclipse.State.GameDetailOptions
{
    // Toggles the game's favourite status. Enter deliberately leaves the overlay open so the
    // label can flip and the user can see what happened.
    public class FavoriteDetailOption : GameDetailOptionItem
    {
        public override GameDetailOption Kind => GameDetailOption.Favorite;

        public override void Activate(EclipseStateContext eclipseStateContext)
        {
            AttractModeService.Instance.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.FavoriteCurrentGame();
        }
    }
}
