using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Windows.Forms;

namespace Eclipse.Service
{
    class BigBoxSettingsHelper
    {
        // gets the index of the monitor from the big box settings file and returns it's height
        // defaults to 1080 if anything goes wrong
        // this height is used for prescaling images to the right size
        public static int GetMonitorHeight()
        {
            int defaultHeight = 1440;
            int monitorHeight;
            try
            {
                monitorHeight = Screen.FromHandle(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle).Bounds.Height;
            }
            catch (Exception ex)
            {
                Helpers.LogException(ex, "GetMonitorHeight");
                Helpers.Log($"Monitor height not found - defaulting to {defaultHeight}");
                monitorHeight = defaultHeight;
            }

            return monitorHeight;
        }

        public static string[] GetFrontImageFolders()
        {
            string[] imageFrontFolders;
            try
            {
                // get the index from the big box xml file
                var launchBoxSettingsDocument = XDocument.Load(Helpers.LaunchBoxSettingsFile);
                var setting = from xmlElement in launchBoxSettingsDocument.Root.Descendants("Settings")
                              select xmlElement.Element("FrontImageTypePriorities").Value;
                string value = setting.FirstOrDefault();

                string[] splitter = new string[] { "," };
                imageFrontFolders = value.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception ex)
            {
                Helpers.LogException(ex, "GetFrontImageFolders");

                // default folders 
                imageFrontFolders = new string[]
                {
                    "GOG Poster",
                    "Steam Poster",
                    "Epic Games Poster",
                    "Box - Front",
                    "Box - Front - Reconstructed",
                    "Advertisement Flyer - Front",
                    "Origin Poster",
                    "Uplay Thumbnail",
                    "Fanart - Box - Front",
                    "Steam Banner"
                };
            }

            return imageFrontFolders;
        }
    }
}
