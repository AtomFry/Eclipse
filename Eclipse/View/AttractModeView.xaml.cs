using Eclipse.Service;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Eclipse.View
{
    // The screen saver's visuals, and only those. Moved out of MainWindowView so the attract
    // mode service and state depend on IAttractModePresenter rather than on the whole plugin
    // entry point, and so the fades and the pan live beside the markup they animate.
    public partial class AttractModeView : UserControl, IAttractModePresenter
    {
        public AttractModeView()
        {
            InitializeComponent();
        }

        // Read per call rather than cached in the constructor. This control is built when the
        // plugin loads, so a cached copy could predate the settings being read and would then
        // disagree with the slideshow's copy, which is taken per run. The settings themselves
        // are cached for the process lifetime either way, so this does not make them live.
        private static AttractModeTimings Timings => AttractModeTimings.Current;

        // DataContext is inherited from the hosting view, so this is the same view model the
        // rest of the theme is bound to.
        private MainWindowViewModel ViewModel => DataContext as MainWindowViewModel;

        public void TurnOff()
        {
            OnUiThread(() =>
            {
                double exitFade = Timings.ExitFade.TotalMilliseconds;

                // The pan runs for 17 seconds by default and was never stopped on the way out,
                // so it carried on animating a hidden element after the user had left.
                StopPan();

                FadeFrameworkElementOpacity(Image_AttractModeBackgroundImage, 0, exitFade);
                Image_AttractModeBackgroundImage.Source = null;

                FadeFrameworkElementOpacity(Image_AttractModeClearLogo, 0, exitFade);
                Image_AttractModeClearLogo.Source = null;

                FadeFrameworkElementOpacity(Grid_AttractMode, 0, exitFade);

                MainWindowViewModel viewModel = ViewModel;
                if (viewModel != null)
                {
                    viewModel.IsDisplayingAttractMode = false;
                }
            });
        }

        public void FadeToBlack()
        {
            OnUiThread(() =>
            {
                // reset the background image - it should already be faded out but make sure
                Image_AttractModeBackgroundImage.Opacity = 0;
                Image_AttractModeBackgroundImage.Source = null;
                StopPan();

                Image_AttractModeClearLogo.Opacity = 0;
                Image_AttractModeClearLogo.Source = null;

                // make the grid transparent so we can fade it in
                Grid_AttractMode.Opacity = 0;

                // flag attract mode
                MainWindowViewModel viewModel = ViewModel;
                if (viewModel != null)
                {
                    viewModel.IsDisplayingAttractMode = true;
                }

                // fade the grid in if it isn't already
                FadeFrameworkElementOpacity(Grid_AttractMode, 1, Timings.FadeIn.TotalMilliseconds);
            });
        }

        public void ShowBackground(ImageSource image, bool slideLeft)
        {
            OnUiThread(() =>
            {
                AttractModeTimings timings = Timings;

                // reset the background image - it should already be faded out but make sure
                Image_AttractModeBackgroundImage.Opacity = 0;
                Image_AttractModeBackgroundImage.Source = null;
                StopPan();

                Image_AttractModeClearLogo.Opacity = 0;
                Image_AttractModeClearLogo.Source = null;

                // already decoded and frozen off the UI thread by the slideshow
                Image_AttractModeBackgroundImage.Source = image;
                FadeFrameworkElementOpacity(Image_AttractModeBackgroundImage, 1, timings.BackgroundFadeIn.TotalMilliseconds);

                // The image is deliberately wider than the screen; the pan slides that
                // overhang across. Measure against this control's own width, NOT the monitor
                // resolution: Image.Width is in device-independent units while Screen.Bounds
                // is in physical pixels, and on a display with DPI scaling those differ.
                double displayWidth = ActualWidth;

                // To slide left - set canvas left edge to 0 and shift image from 0 to (displayWidth - ImageWidth) (i.e. from 0 to -25)
                // To slide right - set canvas left edge to (displayWidth - imageWidth) and shift image from 0 to imageWidth - displayWidth (i.e. from 0 to 25)
                double attractCanvasLeftCoordinate, // where to start the canvas
                        shiftCanvasFrom,            // where to shift from
                        shiftCanvasTo;              // where to shift to

                if (slideLeft)
                {
                    attractCanvasLeftCoordinate = 0;
                    shiftCanvasFrom = 0;
                    shiftCanvasTo = displayWidth - Image_AttractModeBackgroundImage.Width;
                }
                else
                {
                    attractCanvasLeftCoordinate = displayWidth - Image_AttractModeBackgroundImage.Width;
                    shiftCanvasFrom = 0;
                    shiftCanvasTo = Image_AttractModeBackgroundImage.Width - displayWidth;
                }

                // shift the canvas
                Canvas.SetLeft(Canvas_AttractModeInnerCanvas, attractCanvasLeftCoordinate);
                ShiftFrameworkElement(Canvas_AttractModeInnerCanvas, shiftCanvasFrom, shiftCanvasTo, timings.Pan.TotalMilliseconds);
            });
        }

        public void ShowLogo(ImageSource logo)
        {
            OnUiThread(() =>
            {
                // A game with no resolved logo simply shows no logo - better than a
                // placeholder box-front.
                if (logo == null)
                {
                    return;
                }

                Image_AttractModeClearLogo.Source = logo;
                FadeFrameworkElementOpacity(Image_AttractModeClearLogo, 1, Timings.LogoFadeIn.TotalMilliseconds);
            });
        }

        public void FadeOutBackgroundAndLogo()
        {
            OnUiThread(() =>
            {
                AttractModeTimings timings = Timings;

                // fade out this image
                FadeFrameworkElementOpacity(Image_AttractModeBackgroundImage, 0, timings.BackgroundFadeOut.TotalMilliseconds);
                FadeFrameworkElementOpacity(Image_AttractModeClearLogo, 0, timings.LogoFadeOut.TotalMilliseconds);
            });
        }

        // The slideshow runs off the UI thread, so every presenter call has to marshal. This
        // used to be a blocking Dispatcher.Invoke from a timer thread: if the UI thread was
        // stalled - a host dialog, a game launching - the caller parked with it. Running
        // inline when already on the UI thread keeps ordering identical to before; going
        // through InvokeAsync otherwise means the slideshow never waits on the UI.
        private void OnUiThread(Action action)
        {
            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.InvokeAsync(action);
            }
        }

        // Kept private here rather than shared with MainWindowView: it is a few lines of
        // view-local animation plumbing, and a shared helper would couple the two views back
        // together for no benefit.
        private void FadeFrameworkElementOpacity(FrameworkElement element, double newOpacityValue, double durationInMilliseconds, EventHandler completedEventHandler = null)
        {
            if (element.Opacity != newOpacityValue)
            {
                DoubleAnimation dimElement = new DoubleAnimation(element.Opacity, newOpacityValue, TimeSpan.FromMilliseconds(durationInMilliseconds));

                if (completedEventHandler != null)
                {
                    dimElement.Completed += completedEventHandler;
                }

                element.BeginAnimation(OpacityProperty, dimElement);
            }
        }

        // Stops the pan where it is rather than removing the animation outright, which would
        // snap the image back to its start position part way through the fade out. The pan
        // animates the inner canvas - the reset code used to clear the RenderTransform on the
        // background image instead, which is a different element, so it never had any effect.
        private void StopPan()
        {
            if (Canvas_AttractModeInnerCanvas.RenderTransform is TranslateTransform panTransform)
            {
                double currentX = panTransform.X;
                panTransform.BeginAnimation(TranslateTransform.XProperty, null);
                panTransform.X = currentX;
            }
        }

        private void ShiftFrameworkElement(FrameworkElement element, double fromValue, double toValue, double durationInMilliseconds)
        {
            TranslateTransform translateTransform = new TranslateTransform();
            element.RenderTransform = translateTransform;

            DoubleAnimation moveElement = new DoubleAnimation(fromValue, toValue, TimeSpan.FromMilliseconds(durationInMilliseconds));

            translateTransform.BeginAnimation(TranslateTransform.XProperty, moveElement);
        }
    }
}
