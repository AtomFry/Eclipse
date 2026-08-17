using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.Helpers
{
    class LogHelper
    {
        public static readonly string LogFile = ResolveLogFilePath();

        // The log lives beside the plugin, in <LaunchBox>\Plugins\Eclipse. This used to be a
        // bare relative path, which resolved against whatever working directory the host
        // process happened to start from - it landed in the LaunchBox root by luck rather
        // than by design.
        private static string ResolveLogFilePath()
        {
            try
            {
                string folder = DirectoryInfoHelper.Instance.EclipseFolder;

                // Logging can happen before the startup folder creation runs, so make sure
                // the folder exists rather than throwing from inside an error handler.
                Directory.CreateDirectory(folder);

                return Path.Combine(folder, "Eclipse.txt");
            }
            catch
            {
                // If the plugin folder cannot be resolved or created there is nowhere to
                // report that, so fall back to the previous behaviour rather than losing
                // logging altogether.
                return "Eclipse.txt";
            }
        }

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
            if (ex != null)
            {
                Log($"An exception occurred while attempting to {context}");
                Log($"Exception message: {ex?.Message ?? "null"}");
                Log($"Exception stack: {ex?.StackTrace ?? "null"}");

                if (ex.InnerException != null)
                {
                    LogException(ex.InnerException, "Inner Exception");
                }
            }
        }

    }
}
