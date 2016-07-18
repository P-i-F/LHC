using SimpleLogger;
using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace LHC
{
    class Utils
    {
        // global app settings
        public static class Settings
        {
            public static bool debugMode = false;
            public static string customerCode = null;
            public static string customerName = null;
            public static string dataFile = null;
            public static XmlDocument programData = null;
            public static string[] servers;
            public static DataSet scripts;
        }

        #region Helper methods
        public static void InitDataDirectory()
        {
            string currentDirectory = Environment.CurrentDirectory.TrimEnd(Path.DirectorySeparatorChar) + "\\";
            string currentDate = DateTime.Today.ToString("yyyyMMdd");
            string dataPath = currentDirectory + "data\\" + currentDate + "\\";

            try
            {
                Directory.CreateDirectory(dataPath);
            }
            catch
            {
                // log is not yet present, console is not yet available, just exit
                Environment.Exit(-1);
            }
        }

        public static void InitLogFile()
        {
            string currentDirectory = Environment.CurrentDirectory.TrimEnd(Path.DirectorySeparatorChar) + "\\";
            string currentDate = DateTime.Today.ToString("yyyyMMdd");
            string logPath = currentDirectory + "data\\" + currentDate + "\\";

            SimpleLog.SetLogFile(logDir: logPath, prefix: "LHC_", writeText: false);
            SimpleLog.Info("Health check started.");
        }

        public static void InitProgramConfigurations()
        {

            // customer info
            Settings.customerCode = ConfigurationManager.AppSettings["CustomerCode"];
            Settings.customerName = ConfigurationManager.AppSettings["CustomerName"];

            if (string.IsNullOrEmpty(Settings.customerCode) || string.IsNullOrEmpty(Settings.customerName))
            {
                Exit(-1, "Customer code and/or name not available.");
            }
            else
            {
                SimpleLog.Info("Customer is " + Settings.customerCode + " - " + Settings.customerName);
            }

            if (Settings.debugMode)
            {
                Console.WriteLine("\r\nCustomer code: {0}", Settings.customerCode);
                Console.WriteLine("Customer name: {0}", Settings.customerName);
            }
        }

        public static void InitDataFile()
        {
 
            // check if data file exists
            string currentDirectory = Environment.CurrentDirectory.TrimEnd(Path.DirectorySeparatorChar) + "\\";
            string dataFile = currentDirectory + "LHC.dat";

            if (!File.Exists(dataFile))
            {
                Exit(-1, "Data file not found! [" + dataFile + "]");
            }
            else
            {
                Settings.dataFile = dataFile;
                SimpleLog.Info("Data file identified [" + Settings.dataFile + "]");
            }

            if (Settings.debugMode)
            {
                Console.WriteLine("\r\nData file: {0}", Settings.dataFile);
            }
        }

        public static void InitProgramData(string data)
        {
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(data);
            }
            catch (Exception ex)
            {
                Exit(-1, "Error loading XML configuration: " + ex.Message + ((ex.InnerException.Message != null) ? "\r\n" + ex.InnerException.Message : ""));
            }

            Settings.programData = xmlDoc;
            SimpleLog.Info("Data file decrypted and XML configuration loaded successfully.");

            //if (Settings.debugMode)
            //{
            //    Console.WriteLine("\r\nData file decrypted and XML configuration loaded successfully.");
            //}
        }

        public static void InitServerList()
        {
            string[] servers = null;

            try
            {
                XmlNode serversNode = Settings.programData.SelectSingleNode("/sql/instances");
                string serversRaw = serversNode.InnerText;

                // servers are stored one per line, split them
                servers = Regex.Split(serversRaw, "\r\n");

                // trim leading and trailing whitespace characters
                servers = servers.Select(x => x.Trim()).ToArray();

                // eliminate empty rows
                servers = servers.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            }
            catch (Exception ex)
            {
                Exit(-1, "Error initializing server list: " + ex.Message + ((ex.InnerException.Message != null) ? "\r\n" + ex.InnerException.Message : ""));
            }

            int cntServers = servers.Length;
            if (cntServers == 0)
            {
                Exit(-1, "Server list is empty.");
            }
            else
            {
                Settings.servers = servers;
                SimpleLog.Info("Server list initialized. " + cntServers.ToString() + " servers found.");
            }

            if (Settings.debugMode)
            {
                Console.WriteLine("\r\n{0} servers found.", cntServers);
            }
        }

        public static void InitScripts()
        {
            DataSet scripts = new DataSet("scripts");
            DataTable scriptHeader = scripts.Tables.Add("scriptHeader");
            DataTable scriptContent = scripts.Tables.Add("scriptContent");

            scriptHeader.Columns.AddRange(new DataColumn[] {
                new DataColumn("id", typeof(string)),
                new DataColumn("description", typeof(string))
            });

            scriptContent.Columns.AddRange(new DataColumn[] {
                new DataColumn("scriptId", typeof(string)),
                new DataColumn("minVersion", typeof(decimal)),
                new DataColumn("maxVersion", typeof(decimal)),
                new DataColumn("tsql", typeof(string))
            });

            int cntScripts = 0;
            int cntVersions = 0;
            
            if (Settings.debugMode)
            {
                Console.WriteLine("\r\nIdentifying scripts:");
            }

            foreach (XmlNode scriptNode in Settings.programData.GetElementsByTagName("script"))
            {
                cntScripts++;
                DataRow rowH = scriptHeader.Rows.Add(
                    scriptNode.Attributes["id"].Value,
                    scriptNode.Attributes["description"].Value);

                XmlNodeList scriptVersions = scriptNode.SelectNodes("tsql");

                foreach (XmlNode scriptVersion in scriptVersions)
                {
                    cntVersions++;
                    DataRow rowD = scriptContent.Rows.Add(
                        scriptNode.Attributes["id"].Value,
                        Convert.ToDecimal(scriptVersion.Attributes["minVersion"].Value, new CultureInfo("en-US")),
                        Convert.ToDecimal(scriptVersion.Attributes["maxVersion"].Value, new CultureInfo("en-US")),
                        scriptVersion.InnerText);
                }

                if (Settings.debugMode)
                {
                    Console.WriteLine("{0} - {1}", scriptNode.Attributes["id"].Value, scriptNode.Attributes["description"].Value);
                }
                 
            }

            //scriptHeader.ChildRelations.Add("FK_script_id", scriptHeader.Columns["id"], scriptContent.Columns["scriptId"]);
            //var childRows = scriptHeader.Rows[0].GetChildRows("FK_script_id");

            if (cntScripts == 0 || cntVersions == 0)
            {
                Exit(-1, "Script or version count is empty.");
            }
            else
            {
                Settings.scripts = scripts;
                SimpleLog.Info("Script list initialized. Identified " + cntScripts.ToString() + " scripts and " + cntVersions.ToString() + " versions.");
            }

            if (Settings.debugMode)
            {
                Console.WriteLine("\r\nIdentified {0} scripts and {1} versions.", cntScripts, cntVersions);
            }
        }
        #endregion

        #region Cryptography
        ///<summary>
        /// Encrypts a file using Rijndael algorithm.
        ///</summary>
        public static void EncryptFile(string inputFile, string outputFile)
        {

            try
            {
                string k = GetMD5(Settings.customerCode).ToUpper();
                byte[] key = Encoding.Unicode.GetBytes(k);

                string cryptFile = outputFile;
                FileStream fsCrypt = new FileStream(cryptFile, FileMode.Create);

                RijndaelManaged RMCrypto = new RijndaelManaged();

                Rfc2898DeriveBytes derivedKey = new Rfc2898DeriveBytes(k, key);
                RMCrypto.Key = derivedKey.GetBytes(RMCrypto.KeySize / 8);
                RMCrypto.IV = derivedKey.GetBytes(RMCrypto.BlockSize / 8);

                CryptoStream cs = new CryptoStream(fsCrypt, RMCrypto.CreateEncryptor(), CryptoStreamMode.Write);
                FileStream fsIn = new FileStream(inputFile, FileMode.Open);

                int data;
                while ((data = fsIn.ReadByte()) != -1)
                    cs.WriteByte((byte)data);


                fsIn.Close();
                cs.Close();
                fsCrypt.Close();
            }
            catch
            {
                Console.WriteLine("Encryption failed!");
            }
        }

        ///<summary>
        /// Decrypts a file using Rijndael algorithm.
        ///</summary>
        public static bool DecryptFile(string inputFile, string outputFile)
        {
            bool result = true;
            try
            {
                string k = GetMD5(Settings.customerCode).ToUpper();
                byte[] key = Encoding.Unicode.GetBytes(k);

                FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);
                RijndaelManaged RMCrypto = new RijndaelManaged();

                Rfc2898DeriveBytes derivedKey = new Rfc2898DeriveBytes(k, key);
                RMCrypto.Key = derivedKey.GetBytes(RMCrypto.KeySize / 8);
                RMCrypto.IV = derivedKey.GetBytes(RMCrypto.BlockSize / 8);

                CryptoStream cs = new CryptoStream(fsCrypt, RMCrypto.CreateDecryptor(), CryptoStreamMode.Read);
                FileStream fsOut = new FileStream(outputFile, FileMode.Create);

                int data;
                while ((data = cs.ReadByte()) != -1)
                    fsOut.WriteByte((byte)data);

                fsOut.Close();
                cs.Close();
                fsCrypt.Close();
            }
            catch
            {
                result = false;
            }

            return result;
        }

        ///<summary>
        /// Decrypts a file using Rijndael algorithm and stores the decrypted data into a string
        ///</summary>
        public static string DecryptFileToString(string inputFile)
        { 
            string k = GetMD5(Settings.customerCode).ToUpper();
            byte[] key = Encoding.Unicode.GetBytes(k);

            FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);

            RijndaelManaged RMCrypto = new RijndaelManaged();

            Rfc2898DeriveBytes derivedKey = new Rfc2898DeriveBytes(k, key);
            RMCrypto.Key = derivedKey.GetBytes(RMCrypto.KeySize / 8);
            RMCrypto.IV = derivedKey.GetBytes(RMCrypto.BlockSize / 8);

            CryptoStream cs = new CryptoStream(fsCrypt, RMCrypto.CreateDecryptor(), CryptoStreamMode.Read);
            MemoryStream ms = new MemoryStream();

            int data;
            while ((data = cs.ReadByte()) != -1)
                ms.WriteByte((byte)data);

            cs.Close();
            fsCrypt.Close();

            string result;
            ms.Position = 0;
            using (var streamReader = new StreamReader(ms))
            {
                result = streamReader.ReadToEnd();
            }

            return result;

        }

        public static string GetMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
        #endregion Cryptography

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

        #region DB utility methods

        static string GetConnectionString(string serverName)
        {
            if (string.IsNullOrEmpty(serverName))
            {
                Exit(-1, "No server name specified for creating a connection string.");
            }

            SqlConnectionStringBuilder csBuilder = new SqlConnectionStringBuilder();
            csBuilder["Data Source"] = serverName;
            csBuilder["Integrated Security"] = true;
            csBuilder["Initial Catalog"] = "master";
            //csBuilder["Connect Timeout"] = 60; // default 15 seconds

            return csBuilder.ConnectionString;
        }

        /// <summary>
        /// Executes the reader.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="dbParameters">The db parameters.</param>
        /// <returns>A IDataReader object. </returns>
        public static IDataReader ExecuteReader(string serverName, string commandText, CommandType commandType, params DbParameter[] dbParameters)
        {
            SqlConnection connection = null;
            SqlCommand command = null;
            IDataReader reader;
            try
            {
                connection = new SqlConnection(GetConnectionString(serverName));
                command = new SqlCommand(commandText, connection);

                if (dbParameters != null && dbParameters.Length > 0)
                {
                    foreach (DbParameter param in dbParameters)
                    {
                        if (param != null)
                            command.Parameters.Add(param);
                    }
                }
                command.CommandType = commandType;
                command.CommandTimeout = 600; // 10 minutes
                connection.Open();
                reader = command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (SqlException ex)
            {
                throw new DataException(string.Empty, ex);
            }
            catch (Exception ex)
            {
                throw new DataException(string.Empty, ex);
            }
            return reader;
        }

        /// <summary>
        /// Executes the reader.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="dbParameters">The db parameters.</param>
        /// <returns>A IDataReader object. </returns>
        public static IDataReader ExecuteReader(out SqlException sqlEx, string serverName, string commandText, CommandType commandType, params DbParameter[] dbParameters)
        {
            sqlEx = null;
            SqlConnection connection = null;
            SqlCommand command = null;
            IDataReader reader = null;
            try
            {
                connection = new SqlConnection(GetConnectionString(serverName));
                command = new SqlCommand(commandText, connection);

                if (dbParameters != null && dbParameters.Length > 0)
                {
                    foreach (DbParameter param in dbParameters)
                    {
                        if (param != null)
                            command.Parameters.Add(param);
                    }
                }
                command.CommandType = commandType;
                command.CommandTimeout = 600; // 10 minutes
                connection.Open();
                reader = command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (SqlException ex)
            {
                sqlEx = ex;
            }
            catch (Exception ex)
            {
                throw new DataException(string.Empty, ex);
            }
            return reader;
        }

        /// <summary>
        /// Executes the non query.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="dbParameters">The db parameters.</param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string serverName, string commandText, CommandType commandType, params DbParameter[] dbParameters)
        {

            int rowAffected = 0;

            try
            {
                using (SqlConnection connection = new SqlConnection(GetConnectionString(serverName)))
                {
                    SqlCommand command = new SqlCommand(commandText, connection);

                    command.CommandType = commandType;
                    command.CommandTimeout = 600; // 10 minutes

                    if (dbParameters != null && dbParameters.Length > 0)
                    {
                        command.Parameters.AddRange(dbParameters);
                    }
                    connection.Open();
                    rowAffected = command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new DataException(ex.Message, ex);
            }

            return rowAffected;

        }

        /// <summary>
        /// Executes the non query.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="dbParameters">The db parameters.</param>
        /// <returns>The number of rows affected.</returns>
        public static int ExecuteNonQuery(string serverName, string commandText, CommandType commandType, DbParameter[] dbParameters, bool isTransactionScope)
        {
            SqlConnection connection = null;
            SqlCommand command = null;
            int rowAffected = 0;

            try
            {
                connection = new SqlConnection(GetConnectionString(serverName));
                command = new SqlCommand(commandText, connection);

                if (dbParameters != null && dbParameters.Length > 0)
                {
                    command.Parameters.AddRange(dbParameters);
                }

                command.CommandType = commandType;
                command.CommandTimeout = 600; // 10 minutes

                connection.Open();
                rowAffected = command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                throw new DataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new DataException(ex.Message, ex);
            }
            finally
            {
                if (connection != null
                  && connection.State == ConnectionState.Open
                  && !isTransactionScope)
                {
                    connection.Close();
                    connection.Dispose();
                }

                if (command != null)
                    command.Dispose();
            }

            return rowAffected;
        }

        /// <summary>
        /// Executes the scalar.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="dbParameters">The db parameters.</param>
        /// <returns>The first column of the first row in the result set, or a null reference (Nothing in Visual Basic) if the result set is empty.</returns>
        public static object ExecuteScalar(string serverName, string commandText, CommandType commandType, DbParameter[] dbParameters)
        {
            SqlConnection connection = null;
            SqlCommand command = null;
            object result;

            try
            {
                connection = new SqlConnection(GetConnectionString(serverName));
                command = new SqlCommand(commandText, connection);
                if (dbParameters != null)
                    command.Parameters.AddRange(dbParameters);
                command.CommandType = commandType;
                command.CommandTimeout = 600; // 10 minutes

                connection.Open();
                result = command.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                throw new DataException(ex.Message, ex);
            }
            finally
            {
                if (connection != null
                  && connection.State == ConnectionState.Open)
                {
                    connection.Close();
                    connection.Dispose();
                }

                if (command != null)
                    command.Dispose();
            }

            return result;
        }

        public static object ExecuteScalar(string serverName, string commandText, CommandType commandType)
        {
            SqlConnection connection = null;
            SqlCommand command = null;
            object result;

            try
            {
                connection = new SqlConnection(GetConnectionString(serverName));
                command = new SqlCommand(commandText, connection);
                //if (dbParameters != null)
                //  command.Parameters.AddRange(dbParameters);
                command.CommandType = commandType;
                command.CommandTimeout = 600; // 10 minutes

                connection.Open();
                result = command.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                throw new DataException(ex.Message, ex);
            }
            finally
            {
                if (connection != null
                  && connection.State == ConnectionState.Open)
                {
                    connection.Close();
                    connection.Dispose();
                }

                if (command != null)
                    command.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Executes the data adapter.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="dbParameters">The db parameters.</param>
        /// <returns>A DataSet to fill with records and, if necessary, schema.</returns>
        public static DataSet ExecuteDataAdapter(string serverName, string commandText, CommandType commandType, DbParameter[] dbParameters)
        {
            SqlConnection connection = null;
            SqlCommand command = null;
            DbDataAdapter adapter = null;
            DataSet ds = new DataSet();

            try
            {
                connection = new SqlConnection(GetConnectionString(serverName));
                command = new SqlCommand(commandText, connection);

                if (dbParameters != null)
                {
                    command.Parameters.AddRange(dbParameters);
                }
                command.CommandType = commandType;
                command.CommandTimeout = 600; // 10 minutes

                adapter = new SqlDataAdapter(command);

                adapter.Fill(ds);
            }
            catch (SqlException ex)
            {
                throw new DataException("Error retrieving data from the database", ex);
            }
            finally
            {
                if (connection != null
                  && connection.State == ConnectionState.Open)
                {
                    connection.Close();
                    connection.Dispose();
                }

                if (command != null)
                    command.Dispose();

                if (adapter != null)
                    adapter.Dispose();
            }

            return ds;
        }

        public static void FillDataSet(string serverName, string procedureName, DataSet ds, string tableName, DbParameter[] dbParameters)
        {
            SqlCommand cmd = new SqlCommand(procedureName);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 0; // timeout nedefinit

            FillDataSet(serverName, cmd, ds, tableName, dbParameters);
        }

        /// <summary>
        /// Fills the dataset
        /// </summary>
        public static void FillDataSet(string serverName, SqlCommand selectCommand, DataSet ds, string tableName, params DbParameter[] dbParameters)
        {
            try
            {
                selectCommand.Connection = new SqlConnection(GetConnectionString(serverName));

                if (dbParameters != null)
                {
                    selectCommand.Parameters.AddRange(dbParameters);
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter())
                {

                    adapter.SelectCommand = selectCommand;
                    adapter.FillError += new FillErrorEventHandler(adapter_FillError);

                    adapter.Fill(ds, tableName);

                }
            }
            catch (DBConcurrencyException dbEx)
            {
                throw new Exception("Concurrency error in FillDataSet", dbEx);
            }
            catch (SqlException sqlEx)
            {
                throw new DataException("Error retrieving data", sqlEx);
            }
        }

        static void adapter_FillError(object sender, FillErrorEventArgs e)
        {

        }
        #endregion

        public static void Exit(int exitCode = 1, string errorMessage = "")
        {

            if (Utils.Settings.debugMode)
            {

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

            if (!string.IsNullOrEmpty(errorMessage))
            {
                SimpleLog.Error(errorMessage);
            }

            SimpleLog.Info("Health check finished.");
            Environment.Exit(exitCode);
        }

    }
}
