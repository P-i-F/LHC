using System;
using System.Collections.Generic;
using System.Text;
using SimpleLogger;

namespace LHC
{
    class Program
    {
        static void Main(string[] args)
        {
            SimpleLog.SetLogFile(logDir: ".", prefix: "LHC_", writeText: false);

            Console.WriteLine("Log is written to {0}.", SimpleLog.FileName);

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
