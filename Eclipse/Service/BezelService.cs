using Eclipse.Helpers;
using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.IO;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace Eclipse.Service
{
    public class BezelService
    {
        static BezelService()
        {
        }

        private BezelService()
        {
            string bezelFolder = DirectoryInfoHelper.Instance.BezelFolder;

            // default bezel path 
            // ..\LaunchBox\Plugins\Eclipse\Media\Bezels\DEFAULT\Horizontal.png
            // ..\LaunchBox\Plugins\Eclipse\Media\Bezels\DEFAULT\Vertical.png
            Tuple<BezelType, BezelOrientation, string> defaultHorizontalBezelKey = new Tuple<BezelType, BezelOrientation, string>(BezelType.Default, BezelOrientation.Horizontal, string.Empty);
            Tuple<BezelType, BezelOrientation, string> defaultVerticalBezelKey = new Tuple<BezelType, BezelOrientation, string>(BezelType.Default, BezelOrientation.Vertical, string.Empty);

            string defaultHorizontalBezelPath = Path.Combine(bezelFolder, "Default", "Horizontal.png");
            string defaultVerticalBezelPath = Path.Combine(bezelFolder, "Default", "Vertical.png");

            if (File.Exists(defaultHorizontalBezelPath))
            {
                bezelDictionary.Add(defaultHorizontalBezelKey, new Uri(defaultHorizontalBezelPath));
            }

            if (File.Exists(defaultVerticalBezelPath))
            {
                bezelDictionary.Add(defaultVerticalBezelKey, new Uri(defaultVerticalBezelPath));
            }

            // platform bezel path 
            // ..\LaunchBox\Plugins\Eclipse\Media\Bezels\{PLATFORM}\Horizontal.png
            // ..\LaunchBox\Plugins\Eclipse\Media\Bezels\{PLATFORM}\Vertical.png
            List<IPlatform> platforms = new List<IPlatform>(PluginHelper.DataManager.GetAllPlatforms());
            foreach (IPlatform platform in platforms)
            {
                Tuple<BezelType, BezelOrientation, string> platformHorizontalBezelKey = new Tuple<BezelType, BezelOrientation, string>(BezelType.PlatformDefault, BezelOrientation.Horizontal, platform.Name);
                Tuple<BezelType, BezelOrientation, string> platformVerticalBezelKey = new Tuple<BezelType, BezelOrientation, string>(BezelType.PlatformDefault, BezelOrientation.Vertical, platform.Name);

                string platformHorizontalBezelPath = Path.Combine(DirectoryInfoHelper.Instance.BezelFolder, platform.Name, "Horizontal.png");
                string platformVerticalBezelPath = Path.Combine(DirectoryInfoHelper.Instance.BezelFolder, platform.Name, "Vertical.png");

                if (File.Exists(platformHorizontalBezelPath))
                {
                    bezelDictionary.Add(platformHorizontalBezelKey, new Uri(platformHorizontalBezelPath));
                }

                if (File.Exists(platformVerticalBezelPath))
                {
                    bezelDictionary.Add(platformVerticalBezelKey, new Uri(platformVerticalBezelPath));
                }
            }
        }

        /// <summary>
        /// The bezel to frame a video with, or null for none - the whole five-level chain
        /// behind one call.
        ///
        /// Levels 1-3 are resolved during hydration and arrive here as
        /// <paramref name="gameBezel"/>, because they depend only on the game. Levels 4 and 5
        /// cannot be: they depend on the video's dimensions, which are not known until the
        /// player raises MediaOpened (RULE-MEDIA-021). That is a real constraint and this does
        /// not try to remove it - what it removes is the view having to know the rule.
        /// </summary>
        /// <param name="gameBezel">What levels 1-3 found for this game, or null.</param>
        /// <param name="platformName">The game's platform, for the level 4 lookup.</param>
        /// <param name="videoWidth">The video's natural width, or 0 if not known yet.</param>
        /// <param name="videoHeight">The video's natural height, or 0 if not known yet.</param>
        public Uri ResolveBezel(Uri gameBezel, string platformName, int videoWidth, int videoHeight)
        {
            BezelSelection selection = BezelRules.Choose(gameBezel != null, videoWidth, videoHeight);

            switch (selection.Source)
            {
                case BezelSource.GameSpecific:
                    return gameBezel;

                case BezelSource.Default:
                    // GetDefaultBezel is levels 4 and 5 together - the platform's bezel, then
                    // the global one if the platform has none.
                    return GetDefaultBezel(BezelType.PlatformDefault, selection.Orientation, platformName);

                default:
                    return null;
            }
        }

        public Uri GetDefaultBezel(BezelType bezelType, BezelOrientation bezelOrientation, string platformName)
        {
            // get platform default bezel
            Uri bezelUri = GetBezel(bezelType, bezelOrientation, platformName);
            if (bezelUri != null)
            {
                return bezelUri;
            }

            // get default bezel
            if (bezelType != BezelType.Default)
            {
                bezelUri = GetBezel(BezelType.Default, bezelOrientation, string.Empty);
            }

            return bezelUri;
        }

        private Uri GetBezel(BezelType bezelType, BezelOrientation bezelOrientation, string platformName)
        {
            Uri bezelUri;

            Tuple<BezelType, BezelOrientation, string> bezelKey = new Tuple<BezelType, BezelOrientation, string>(bezelType, bezelOrientation, platformName);
            bezelDictionary.TryGetValue(bezelKey, out bezelUri);

            return bezelUri;
        }


        private Dictionary<Tuple<BezelType, BezelOrientation, string>, Uri> bezelDictionary = new Dictionary<Tuple<BezelType, BezelOrientation, string>, Uri>();

        public static BezelService Instance { get; } = new BezelService();
    }
}