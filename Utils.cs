using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace LHC
{
    class Utils
    {

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

        #region Cryptography
        ///<summary>
        /// Encrypts a file using Rijndael algorithm.
        ///</summary>
        public static void EncryptFile(string inputFile, string outputFile)
        {

            try
            {
                string password = @"A18771CF7E181CDF9DAD769D494BAFEF"; // Your Key Here
                //UnicodeEncoding UE = new UnicodeEncoding();
                byte[] key = Encoding.Unicode.GetBytes(password);

                string cryptFile = outputFile;
                FileStream fsCrypt = new FileStream(cryptFile, FileMode.Create);

                RijndaelManaged RMCrypto = new RijndaelManaged();

                Rfc2898DeriveBytes derivedKey = new Rfc2898DeriveBytes(password, key);
                RMCrypto.Key = derivedKey.GetBytes(RMCrypto.KeySize / 8);
                RMCrypto.IV = derivedKey.GetBytes(RMCrypto.BlockSize / 8);
                //ICryptoTransform encryptor = RMCrypto.CreateEncryptor();

                CryptoStream cs = new CryptoStream(fsCrypt,
                    //RMCrypto.CreateEncryptor(key, key),
                    RMCrypto.CreateEncryptor(),
                    CryptoStreamMode.Write);

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
        public static void DecryptFile(string inputFile, string outputFile)
        {

            {
                string password = @"A18771CF7E181CDF9DAD769D494BAFEF"; // Your Key Here
                //UnicodeEncoding UE = new UnicodeEncoding();
                byte[] key = Encoding.Unicode.GetBytes(password);

                FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);

                RijndaelManaged RMCrypto = new RijndaelManaged();

                Rfc2898DeriveBytes derivedKey = new Rfc2898DeriveBytes(password, key);
                RMCrypto.Key = derivedKey.GetBytes(RMCrypto.KeySize / 8);
                RMCrypto.IV = derivedKey.GetBytes(RMCrypto.BlockSize / 8);
                //ICryptoTransform decryptor = RMCrypto.CreateDecryptor();

                CryptoStream cs = new CryptoStream(fsCrypt,
                    //RMCrypto.CreateDecryptor(key, key),
                    RMCrypto.CreateDecryptor(),
                    CryptoStreamMode.Read);

                FileStream fsOut = new FileStream(outputFile, FileMode.Create);

                int data;
                while ((data = cs.ReadByte()) != -1)
                    fsOut.WriteByte((byte)data);

                fsOut.Close();
                cs.Close();
                fsCrypt.Close();

            }
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
    }
}
