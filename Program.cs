using System;
using SimpleLogger;
using System.Configuration;

namespace LHC
{
    class Program
    {
        //public static bool debugMode = false;
        //public static string customerCode = null;
        //public static string customerName = null;


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
                    Utils.Settings.debugMode = (args[0] == "1");
                }
            }

            // prepare for debug mode
            string debugStatus = (Utils.Settings.debugMode ? "ON" : "OFF");
            SimpleLog.Info("Debug mode is " + debugStatus);

            if (Utils.Settings.debugMode) {
                Utils.ShowConsoleWindow();
                Console.WriteLine("Debug mode {0}", debugStatus);
            }

            // customer info
            Utils.Settings.customerCode = ConfigurationManager.AppSettings["CustomerCode"];
            Utils.Settings.customerName = ConfigurationManager.AppSettings["CustomerName"];

            if (String.IsNullOrEmpty(Utils.Settings.customerCode) || String.IsNullOrEmpty(Utils.Settings.customerName))
            {
                string errorMessage = "Customer code and/or name not available.";
                SimpleLog.Error(errorMessage);
                Exit(-1, errorMessage);
            }
            else {
                SimpleLog.Info("Customer is " + Utils.Settings.customerCode + " - " + Utils.Settings.customerName);
            }

            if (Utils.Settings.debugMode)
            {
                Console.WriteLine();
                Console.WriteLine("Customer code: {0}", Utils.Settings.customerCode);
                Console.WriteLine("Customer name: {0}", Utils.Settings.customerName);
            }

            // check if data file exists


            // allow user to read the info in debug mode before proceeding furter
            if (Utils.Settings.debugMode)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
            }



            //string md5cc = Utils.GetMD5(Utils.Settings.customerCode);
            //Console.WriteLine();
            //Console.WriteLine("Hash is: {0}", md5cc);



            //Utils.EncryptFile("C:\\drgs\\LHC_res\\srv_plain.xml", "C:\\drgs\\LHC_res\\srv.dat");
            //Console.WriteLine();
            //Console.WriteLine("File encrypted");

            //Utils.DecryptFile("C:\\drgs\\LHC_res\\srv.dat", "C:\\drgs\\LHC_res\\srv_plain1.xml");
            string errorMessage = "Data file decryption failed.";
            SimpleLog.Error(errorMessage);
            Exit(-1, errorMessage);

            Utils.DecryptFileToString("C:\\drgs\\LHC_res\\srv.dat");
            Console.WriteLine("File decrypted");

            Exit();
        }

        static void Exit(int exitCode = 1, string errorMessage = "") {

            if (Utils.Settings.debugMode) {

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

    }
}
