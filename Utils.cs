﻿using SimpleLogger;
using System;
using System.Configuration;
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
        }

        #region Helper methods
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

            int cnt = servers.Length;
            if (cnt == 0)
            {
                Exit(-1, "Server list is empty.");
            }
            else
            {
                Settings.servers = servers;
                SimpleLog.Info("Server list initialized. " + cnt.ToString() + " servers found.");
            }

            if (Settings.debugMode)
            {
                Console.WriteLine("\r\n{0} servers found.", cnt);
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

            if (!String.IsNullOrEmpty(errorMessage))
            {
                SimpleLog.Error(errorMessage);
            }

            SimpleLog.Info("Health check finished.");
            Environment.Exit(exitCode);
        }

    }
}
