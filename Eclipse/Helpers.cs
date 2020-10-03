using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigBoxNetflixUI
{
    public static class Helpers
    {
        public static string ApplicationPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        public static string MediaFolder = $"{ApplicationPath}\\Plugins\\BigBoxNetflixUI\\Media";
        public static string LaunchboxImagesPath = $"{ApplicationPath}\\Images";
        public static string PluginImagesPath = $"{MediaFolder}\\Images";
        public static string ResourceFolder = "pack://application:,,,/BigBoxNetflixUI;component/resources";



        // this is hacky as shit...defining some integers we can use in sizing the background image/video
        public static int BackgroundRowSpanFeature = 16;
        public static int BackgroundRowSpanNormal = 10;
        public static int BackgroundColumnStartFeature = 0;
        public static int BackgroundColumnStartNormal = 12;
        public static int BackgroundColumnSpanFeature = 32;
        public static int BackgroundColumnSpanNormal = 20;

        public static void Log(string logMessage)
        {
            using (StreamWriter w = File.AppendText("BigBoxNetflixUI.txt"))
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
