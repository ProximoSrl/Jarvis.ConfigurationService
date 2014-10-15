using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.ConfigurationService.Host.Support
{
    public static class EncryptionUtils
    {

        public class EncryptionKey
        {
            public Byte[] Key { get; set; }

            public Byte[] IV { get; set; }

        }

        public static EncryptionKey GenerateKey()
        {
            using (var rm = new RijndaelManaged())
            {
                rm.GenerateKey();
                rm.GenerateIV();
                EncryptionKey key = new EncryptionKey();
                key.Key = rm.Key;
                key.IV = rm.IV;
                return key;
            }
        }

        public static String Decrypt(Byte[] key, Byte[] iv, String data)
        {
            using (MemoryStream ms = new MemoryStream())
            using (RijndaelManaged crypto = new RijndaelManaged())
            {

                Byte[] rawData = HexEncoding.GetBytes(data);
                ICryptoTransform ct = crypto.CreateDecryptor(key, iv);
                using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Write))
                {
                    cs.Write(rawData, 0, rawData.Length);
                    cs.Close();
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }

        }

        public static Byte[] Decrypt(Byte[] key, Byte[] iv, Byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            using (RijndaelManaged crypto = new RijndaelManaged())
            {
                ICryptoTransform ct = crypto.CreateDecryptor(key, iv);
                using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.Close();
                }

                return ms.ToArray();
            }

        }

        public static String Encrypt(Byte[] key, Byte[] iv, String data)
        {
            using (MemoryStream ms = new MemoryStream())
            using (RijndaelManaged crypto = new RijndaelManaged())
            {
                ICryptoTransform ct = crypto.CreateEncryptor(key, iv);
                Byte[] rawData = Encoding.UTF8.GetBytes(data);
                using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Write))
                {
                    cs.Write(rawData, 0, rawData.Length);
                    cs.Close();
                };
                return BitConverter.ToString(ms.ToArray()).Replace("-", "");
            }

        }

        public static Byte[] Encrypt(Byte[] key, Byte[] iv, byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            using (RijndaelManaged crypto = new RijndaelManaged())
            {
                ICryptoTransform ct = crypto.CreateEncryptor(key, iv);
                using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.Close();
                };
                return ms.ToArray();
            }

        }

        public static EncryptionUtils.EncryptionKey GetDefaultEncryptionKey(out String errorMessage)
        {
            EncryptionUtils.EncryptionKey retValue = null;
            errorMessage = null;
            var keyFileName = Path.Combine(
                   FileSystem.Instance.GetBaseDirectory(),
                   "encryption.key"
              );
            var keyFileContent = FileSystem.Instance.GetFileContent(keyFileName);
            if (String.IsNullOrEmpty(keyFileContent))
            {
                errorMessage = "Missing key file 'encryption.key' in configuration storage root path: " + keyFileName;
                return retValue;
            }

            EncryptionUtils.EncryptionKey key;
            try
            {
                retValue = JsonConvert.DeserializeObject<EncryptionUtils.EncryptionKey>(keyFileContent);
            }
            catch (Exception)
            {
                errorMessage = "Malformed encryption key file: " + keyFileName;
            }
            return retValue;
        }
    }
}
