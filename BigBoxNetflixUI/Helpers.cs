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
        public static void Log(string logMessage)
        {
            using (StreamWriter w = File.AppendText("BigBoxNetflixUI.txt"))
            {
                w.Write("\r\nLog Entry : ");
                w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                w.WriteLine("  :");
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
