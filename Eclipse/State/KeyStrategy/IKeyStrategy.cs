using Eclipse.Models;
using Eclipse.Service;

namespace Eclipse.State.KeyStrategy
{
    public interface IKeyStrategy
    {
        void DoKeyFunction(EclipseStateContext eclipseStateContext, EclipseState eclipseState);
        bool IsValidForState(EclipseState eclipseState);
    }

    public class KeyStrategyCache
    {
        private IKeyStrategy pageUpStrategy;
        public IKeyStrategy PageUpStrategy
        {
            get
            {
                if (pageUpStrategy == null)
                {
                    pageUpStrategy = GetStrategyForFunction(EclipseSettingsDataProvider.Instance.EclipseSettings.PageUpFunction);
                }
                return pageUpStrategy;
            }
        }

        private IKeyStrategy pageDownStrategy;
        public IKeyStrategy PageDownStrategy
        {
            get
            {
                if (pageDownStrategy == null)
                {
                    pageDownStrategy = GetStrategyForFunction(EclipseSettingsDataProvider.Instance.EclipseSettings.PageDownFunction);
                }
                return pageDownStrategy;
            }
        }

        private IKeyStrategy GetStrategyForFunction(PageFunction pageFunction)
        {
            IKeyStrategy keyStrategy = null;

            switch (pageFunction)
            {
                case PageFunction.PageDown:
                    keyStrategy = new KeyStrategyPageDown();
                    break;

                case PageFunction.PageUp:
                    keyStrategy = new KeyStrategyPageUp();
                    break;

                case PageFunction.RandomGame:
                    keyStrategy = new KeyStrategyRandomGame();
                    break;

                case PageFunction.VoiceSearch:
                    keyStrategy = new KeyStrategyVoiceSearch();
                    break;

                case PageFunction.FlipBox:
                    keyStrategy = new KeyStrategyFlipBox();
                    break;

                case PageFunction.ZoomBox:
                    keyStrategy = new KeyStrategyZoomBox();
                    break;

                case PageFunction.VolumeUp:
                    keyStrategy = new KeyStrategyVolumeUp();
                    break;

                case PageFunction.VolumeDown:
                    keyStrategy = new KeyStrategyVolumeDown();
                    break;

                case PageFunction.DisplayDetails:
                    keyStrategy = new KeyStrategyDisplayDetails();
                    break;

                case PageFunction.PlayGame:
                    keyStrategy = new KeyStrategyPlayGame();
                    break;
            }
            return keyStrategy;
        }


        #region singleton implementation 
        public static KeyStrategyCache Instance => instance;

        private static readonly KeyStrategyCache instance = new KeyStrategyCache();

        static KeyStrategyCache()
        {
        }

        private KeyStrategyCache()
        {
        }
        #endregion

    }
}
