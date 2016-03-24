using System;
using SimpleLogger;
using System.Configuration;
using System.Runtime.InteropServices;

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
                    // check if we are in debug mode
                    debugMode = (args[0] == "1");
                }
            }

            // prepare for debug mode
            string debugStatus = (debugMode ? "ON" : "OFF");
            SimpleLog.Info("Debug mode is " + debugStatus);

            if (debugMode) {
                ShowConsoleWindow();
                Console.WriteLine("Debug mode {0}", debugStatus);
            }

            // global configurations
            string customerCode = ConfigurationManager.AppSettings["CustomerCode"];
            string customerName = ConfigurationManager.AppSettings["CustomerName"];

            if (String.IsNullOrEmpty(customerCode) || String.IsNullOrEmpty(customerName))
            {
                string errorMessage = "Customer code and/or name not available.";
                SimpleLog.Error(errorMessage);
                Exit(-1, errorMessage);
            }
            else {
                SimpleLog.Info("Customer is " + customerCode + " - " + customerName);
            }

            if (debugMode)
            {
                Console.WriteLine();
                Console.WriteLine("Customer code: {0}", customerCode);
                Console.WriteLine("Customer name: {0}", customerName);
            }

            // allow user to read the info in debug mode before proceeding furter
            if (debugMode)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
            }

            //Console.WriteLine();
            //Console.WriteLine("This is the rest of it.");

            Exit();
        }

        static void Exit(int exitCode = 1, string errorMessage = "") {

            if (debugMode) {

                Console.WriteLine();
                if (exitCode == -1)
                {
                    Console.WriteLine(errorMessage);
                    //SimpleLog.ShowLogFile();
                }
                Console.WriteLine();
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }

            SimpleLog.Info("Health check finished.");
            Environment.Exit(exitCode);
        }

        #region Show/Hide console window
        public static void ShowConsoleWindow()
        {
            var handle = GetConsoleWindow();

            if (handle == IntPtr.Zero)
            {
                AllocConsole();
            }
            else
            {
                ShowWindow(handle, SW_SHOW);
            }
        }

        public static void HideConsoleWindow()
        {
            var handle = GetConsoleWindow();

            ShowWindow(handle, SW_HIDE);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        #endregion
    }
}
