using Eclipse.Models;
using Eclipse.Service;

namespace Eclipse.State.GameDetailOptions
{
    // Launches the selected game. Left and right cycle between a game's additional versions,
    // which is why this is the only option where those keys do anything visible on screen.
    public class PlayDetailOption : GameDetailOptionItem
    {
        public override GameDetailOption Kind => GameDetailOption.Play;

        public override void Activate(EclipseStateContext eclipseStateContext)
        {
            // stop rather than restart - a game is about to take over the screen
            AttractModeService.Instance.StopAttractMode();

            eclipseStateContext.MainWindowViewModel.PlayCurrentGame();
        }

        public override void MoveLeft(EclipseStateContext eclipseStateContext)
        {
            eclipseStateContext.MainWindowViewModel.CurrentGameList?.Game1?.GameFiles?.GameVersionList?.CycleBackward();
        }

        public override void MoveRight(EclipseStateContext eclipseStateContext)
        {
            eclipseStateContext.MainWindowViewModel.CurrentGameList?.Game1?.GameFiles?.GameVersionList?.CycleForward();
        }
    }
}
