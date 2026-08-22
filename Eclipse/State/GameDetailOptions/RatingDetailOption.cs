using Eclipse.Models;
using Eclipse.Service;

namespace Eclipse.State.GameDetailOptions
{
    // Sets the user's star rating. The only option with anything to do on the way in and out:
    // the rating is written into the game as the user moves it, so arriving has to remember
    // the original value, leaving by navigation commits it, and Escape puts it back.
    public class RatingDetailOption : GameDetailOptionItem
    {
        private const float RatingStep = 0.5f;

        public override GameDetailOption Kind => GameDetailOption.Rating;

        public override void OnSelected(EclipseStateContext eclipseStateContext)
        {
            eclipseStateContext.MainWindowViewModel.IsRatingGame = true;
            eclipseStateContext.MainWindowViewModel.BeginRatingCurrentGame();
        }

        public override void OnDeselected(EclipseStateContext eclipseStateContext)
        {
            eclipseStateContext.MainWindowViewModel.IsRatingGame = false;
            eclipseStateContext.MainWindowViewModel.SaveRatingCurrentGame();
        }

        public override void OnCancelled(EclipseStateContext eclipseStateContext)
        {
            eclipseStateContext.MainWindowViewModel.IsRatingGame = false;
            eclipseStateContext.MainWindowViewModel.CancelRatingCurrentGame();
        }

        public override void Activate(EclipseStateContext eclipseStateContext)
        {
            AttractModeService.Instance.RestartAttractMode();

            eclipseStateContext.MainWindowViewModel.SaveRatingCurrentGame();
        }

        public override void MoveLeft(EclipseStateContext eclipseStateContext)
        {
            eclipseStateContext.MainWindowViewModel.RateCurrentGame(-RatingStep);
        }

        public override void MoveRight(EclipseStateContext eclipseStateContext)
        {
            eclipseStateContext.MainWindowViewModel.RateCurrentGame(RatingStep);
        }
    }
}
