using System.Windows.Media;

namespace Eclipse.Service
{
    // What attract mode needs the screen to do, and nothing else.
    //
    // The service and the state used to hold a MainWindowView - the whole 700-line plugin
    // entry point, with every named element in the theme reachable from either of them.
    // They now depend on this instead, so the surface they can touch is these five calls.
    //
    // The images arrive already decoded and frozen. The presenter used to pick the game and
    // build a BitmapImage itself, which put a full-size synchronous decode on the UI thread
    // once per slide and left the view knowing how games are chosen. Both now belong to
    // AttractModeSlideshow.
    public interface IAttractModePresenter
    {
        /// <summary>Clear any previous game and fade the screen saver in over black.</summary>
        void FadeToBlack();

        /// <summary>Show a game's background image and start the pan across it.</summary>
        /// <param name="image">A frozen image, or null if even the default background failed to load.</param>
        /// <param name="slideLeft">Which way the pan travels; alternates between slides.</param>
        void ShowBackground(ImageSource image, bool slideLeft);

        /// <summary>Fade the current game's clear logo in.</summary>
        /// <param name="logo">A frozen image, or null for a game with no logo - which shows no logo at all.</param>
        void ShowLogo(ImageSource logo);

        /// <summary>Fade the current game's background and logo out.</summary>
        void FadeOutBackgroundAndLogo();

        /// <summary>Leave attract mode and hide everything it displayed.</summary>
        void TurnOff();
    }
}
