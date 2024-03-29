﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse.Helpers
{
    class LogHelper
    {
        public static string LogFile = $"Eclipse.txt";

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
