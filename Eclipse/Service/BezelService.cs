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