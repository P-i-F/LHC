using System;
using System.Collections.Generic;
using System.Text;
using SimpleLogger;

namespace LHC
{
    class Program
    {
        public static bool debugMode = false;

        // available parameters (position based)
        // [0] debug mode 0(default):no|1:yes
        static void Main(string[] args)
        {
            SimpleLog.SetLogFile(logDir: ".", prefix: "LHC_", writeText: false);

            if (args.Length > 0)
            {
                if (args[0] != null) {
                    debugMode = (args[0] == "1");
                }
            }

            string debugStatus = (debugMode ? "ON" : "OFF");
            Console.WriteLine("Debug mode {0}", debugStatus);
            SimpleLog.Info("Debug mode is " + debugStatus);
            //Console.WriteLine("Log is written to {0}.", SimpleLog.FileName);

            SimpleLog.Info("Test logging started.");
            SimpleLog.Warning("This is a warning.");
            SimpleLog.Error("This is an error.");
            
            // Show log file in browser
            SimpleLog.ShowLogFile();

            // Prevent window from closing
            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
