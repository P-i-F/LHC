using System;
using SimpleLogger;

namespace LHC
{
    class Program
    {

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

            // get program configurations from the config file
            Utils.InitProgramConfigurations();

            // check if LHC.dat data file exists in the curent directory
            Utils.InitDataFile();

            //// dev only - for easier testing of config changes
            Utils.EncryptFile("C:\\drgs\\LHC\\LHC.xml", "C:\\drgs\\LHC\\bin\\Debug\\LHC.dat");

            // decrypt file
            Utils.InitProgramData(Utils.DecryptFileToString(Utils.Settings.dataFile));

            // load server list from decrypted program data
            Utils.InitServerList();
            

            //// dev only - check server list
            foreach (string server in Utils.Settings.servers) { Console.WriteLine(server); }


            // allow user to read the info in debug mode before proceeding further
            if (Utils.Settings.debugMode)
            {
                Console.WriteLine("\r\nPress any key to continue.");
                Console.ReadKey();
            }



            //Utils.EncryptFile("C:\\drgs\\LHC_res\\srv_plain.xml", "C:\\drgs\\LHC_res\\srv.dat");
            //Console.WriteLine();
            //Console.WriteLine("File encrypted");

            //Utils.DecryptFile("C:\\drgs\\LHC_res\\srv.dat", "C:\\drgs\\LHC_res\\srv_plain1.xml");
            //errorMessage = "Data file decryption failed.";
            //SimpleLog.Error(errorMessage);
            //Utils.Exit(-1, errorMessage);

            Utils.Exit();
        }



    }
}
