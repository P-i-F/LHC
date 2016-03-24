using System;
using SimpleLogger;
using System.Configuration;

namespace LHC
{
    class Program
    {
        public static bool debugMode = false;
        public static string customerCode = null;
        public static string customerName = null;

        // available parameters (position based)
        // [0] debug mode - 0/missing(default):no|1:yes
        static void Main(string[] args)
        {
            // initialize log file
            SimpleLog.SetLogFile(logDir: ".", prefix: "LHC_", writeText: false);
            SimpleLog.Info("Health check started.");

            // parse input parameters
            if (args.Length > 0)
            {
                if (args[0] != null) {
                    debugMode = (args[0] == "1");
                }
            }

            // check if we are in debug mode
            string debugStatus = (debugMode ? "ON" : "OFF");
            Console.WriteLine("Debug mode {0}", debugStatus);
            SimpleLog.Info("Debug mode is " + debugStatus);
            //Console.WriteLine("Log is written to {0}.", SimpleLog.FileName);

            string customerCode = ConfigurationManager.AppSettings["CustomerCode"];
            string customerName = ConfigurationManager.AppSettings["CustomerName"];

            if (String.IsNullOrEmpty(customerCode) || String.IsNullOrEmpty(customerName))
            {
                SimpleLog.Error("Customer code and/or name not available.");
                Exit(-1);
            }
            else {
                SimpleLog.Info("Customer is " + customerCode + " - " + customerName);
            }
            
            Console.WriteLine();
            Console.WriteLine("Customer code: {0}", customerCode);
            Console.WriteLine("Customer name: {0}", customerName);

            // Show log file in browser
            //SimpleLog.ShowLogFile();

            // Prevent window from closing
            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        static void Exit(int exitCode = 1) {
            Environment.Exit(exitCode);
        }
    }
}
