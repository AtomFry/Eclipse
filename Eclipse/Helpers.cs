using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse
{
    public static class Helpers
    {
        public static string ApplicationPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        public static string LogFile = $"Eclipse.txt";
        public static string MediaFolder = $"{ApplicationPath}\\Plugins\\Eclipse\\Media";
        public static string LaunchboxImagesPath = $"{ApplicationPath}\\Images";
        public static string PluginImagesPath = $"{MediaFolder}\\Images";
        public static string ResourceFolder = "pack://application:,,,/Eclipse;component/resources";
        public static string BigBoxSettingsFile = $"{ApplicationPath}\\Data\\BigBoxSettings.xml";
        public static string LaunchBoxSettingsFile = $"{ApplicationPath}\\Data\\Settings.xml";
        public static string PlatformImagesPath = $"{ApplicationPath}\\Images\\Platforms";
        public static string ClearLogoFolder = $"Clear Logo";


        // this is hacky as shit...the integers define the row/column span to use for background image/videos, dependent on whether we are in featured game or regular results mode
        public static int BackgroundRowSpanFeature = 16;
        public static int BackgroundRowSpanNormal = 10;
        public static int BackgroundColumnStartFeature = 0;
        public static int BackgroundColumnStartNormal = 12;
        public static int BackgroundColumnSpanFeature = 32;
        public static int BackgroundColumnSpanNormal = 20;

        public static void Log(string logMessage)
        {
            using (StreamWriter w = File.AppendText(LogFile))
            {
                w.Write("\r\nLog Entry : ");
                w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                w.WriteLine($"  :{logMessage}");
                w.WriteLine("-------------------------------");
            }
        }

        public static void LogException(Exception ex, string context)
        {
            Helpers.Log($"An exception occurred while attempting to {context}");
            Helpers.Log($"Exception message: {ex?.Message ?? "null"}");
            Helpers.Log($"Exception stack: {ex?.StackTrace ?? "null"}");
        }
    }
}
