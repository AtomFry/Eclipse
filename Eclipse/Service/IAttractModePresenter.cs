namespace Eclipse.Service
{
    // What attract mode needs the screen to do, and nothing else.
    //
    // The service and the state used to hold a MainWindowView - the whole 700-line plugin
    // entry point, with every named element in the theme reachable from either of them.
    // They now depend on this instead, so the surface they can touch is these five calls.
    //
    // The signatures deliberately mirror the original methods: this stage moves code without
    // changing behaviour. Loading images off the UI thread changes ShowBackground/ShowLogo to
    // take a decoded ImageSource, and that belongs in its own stage.
    public interface IAttractModePresenter
    {
        /// <summary>Clear any previous game and fade the screen saver in over black.</summary>
        void FadeToBlack();

        /// <summary>Pick the next game, fade its background in and start the pan.</summary>
        void FadeInAndSlideBackground(bool slideLeft);

        /// <summary>Fade in the current game's clear logo.</summary>
        void FadeInLogo();

        /// <summary>Fade the current game's background and logo out.</summary>
        void FadeOutBackgroundAndLogo();

        /// <summary>Leave attract mode and hide everything it displayed.</summary>
        void TurnOff();
    }
}
