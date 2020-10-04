using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Windows.Forms;

namespace Eclipse.Service
{
    class BigBoxSettingsReader
    {
        // gets the index of the monitor from the big box settings file and returns it's height
        // defaults to 1080 if anything goes wrong
        // this height is used for prescaling images to the right size
        public static int GetMonitorHeight()
        {
            int monitorHeight = 0;
            int defaultHeight = 1440;
            try
            {
                // get the index from the big box xml file
                var bigBoxSettingsXmlDocument = XDocument.Load(Helpers.BigBoxSettingsFile);
                var setting = from xmlElement in bigBoxSettingsXmlDocument.Root.Descendants("BigBoxSettings")
                              select xmlElement.Element("PrimaryMonitorIndex").Value;
                string value = setting.FirstOrDefault();

                // try to parse the value - default to 0 if anything goes wrong
                int screenIndex = 0;
                if (!int.TryParse(value, out screenIndex))
                {
                    screenIndex = 0;
                }

                // if we are somehow out of bounds, default to index 0
                if (Screen.AllScreens.Length < screenIndex + 1)
                {
                    screenIndex = 0;
                }

                var screen = Screen.AllScreens[screenIndex];
                monitorHeight = screen.Bounds.Height;
            }
            catch (Exception ex)
            {
                Helpers.LogException(ex, "GetMonitorHeight");
            }

            // if anything went wrong, just use 1440 as a default
            if (monitorHeight == 0)
            {
                Helpers.Log($"Monitor height not found - defaulting to {defaultHeight}");
                monitorHeight = defaultHeight;
            }
            return monitorHeight;
        }
    }
}
