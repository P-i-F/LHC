using System;
using SimpleLogger;
using System.Data;
using System.Xml;
using System.Text;
using System.IO;
using System.Globalization;

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



            DataSet scripts = new DataSet("scripts");
            DataTable scriptHeader = scripts.Tables.Add("scriptHeader");
            DataTable scriptContent = scripts.Tables.Add("scriptContent");

            scriptHeader.Columns.AddRange(new DataColumn[] {
                new DataColumn("code", typeof(string)),
                new DataColumn("description", typeof(string))
            });

            scriptContent.Columns.AddRange(new DataColumn[] {
                new DataColumn("scriptCode", typeof(string)),
                new DataColumn("minVersion", typeof(decimal)),
                new DataColumn("maxVersion", typeof(decimal)),
                new DataColumn("tsql", typeof(string))
            });

            foreach (XmlNode scriptNode in Utils.Settings.programData.GetElementsByTagName("script"))
            {
                DataRow rowH = scriptHeader.Rows.Add(
                    scriptNode.Attributes["code"].Value,
                    scriptNode.Attributes["description"].Value);

                Console.WriteLine("\r\nCode: {0}\r\nDescription: {1}", scriptNode.Attributes["code"].Value, scriptNode.Attributes["description"].Value);

                XmlNodeList codes = scriptNode.SelectNodes("tsql");

                foreach (XmlNode codeNode in codes)
                {
                    DataRow rowD = scriptContent.Rows.Add(
                        scriptNode.Attributes["code"].Value,
                        Convert.ToDecimal(codeNode.Attributes["minVersion"].Value, new CultureInfo("en-US")),
                        Convert.ToDecimal(codeNode.Attributes["maxVersion"].Value, new CultureInfo("en-US")),
                        codeNode.InnerText);
                }
            }

            scriptHeader.ChildRelations.Add("FK_script_code", scriptHeader.Columns["code"], scriptContent.Columns["scriptCode"]);
            var childRows = scriptHeader.Rows[0].GetChildRows("FK_script_code");

            //// dev only
            TableToCSV(scriptHeader, "C:\\drgs\\LHC\\bin\\Debug\\scriptHdr.txt");
            TableToCSV(scriptContent, "C:\\drgs\\LHC\\bin\\Debug\\scriptSql.txt");

            decimal version = 10.5m;
            string scriptCode;
            string filterExpression;
            DataRow[] scriptSql;
            foreach (DataRow scrHdr in scriptHeader.Rows)
            {
                scriptCode = scrHdr["code"].ToString();
                filterExpression = "scriptCode = '" + scriptCode + "' AND " + version.ToString().Replace(",",".") + " >= minVersion AND " + version.ToString().Replace(",", ".") + " <= maxVersion";
                scriptSql = scriptContent.Select(filterExpression);

                if (scriptSql.Length == 0)
                {
                    Console.WriteLine("\r\nScript not identified for code " + scriptCode);
                }
                if (scriptSql.Length > 1)
                {
                    Console.WriteLine("\r\nMultiple scripts identified for code " + scriptCode);
                }
                if (scriptSql.Length == 1)
                {
                    Console.WriteLine("\r\nScript identified for code " + scriptCode + "\r\n" + scriptSql[0]["tsql"].ToString());
                }
             }





            // allow user to read the info in debug mode before proceeding further
            if (Utils.Settings.debugMode)
            {
                Console.WriteLine("\r\nPress any key to continue.");
                Console.ReadKey();
            }


            //// generate XML structure for scripts and load them in a datatable


            //Utils.EncryptFile("C:\\drgs\\LHC_res\\srv_plain.xml", "C:\\drgs\\LHC_res\\srv.dat");
            //Console.WriteLine();
            //Console.WriteLine("File encrypted");

            //Utils.DecryptFile("C:\\drgs\\LHC_res\\srv.dat", "C:\\drgs\\LHC_res\\srv_plain1.xml");
            //errorMessage = "Data file decryption failed.";
            //SimpleLog.Error(errorMessage);
            //Utils.Exit(-1, errorMessage);

            Utils.Exit();
        }

        public static void TableToCSV(DataTable table, string strFilePath)
        {
            var result = new StringBuilder();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                result.Append(table.Columns[i].ColumnName);
                result.Append(i == table.Columns.Count - 1 ? "\n" : ",");
            }

            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    result.Append(row[i].ToString());
                    result.Append(i == table.Columns.Count - 1 ? "\n" : ",");
                }
            }

            //return result.ToString();
            File.WriteAllText(strFilePath, result.ToString());
        }

    }
}
