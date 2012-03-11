using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Pushak.Shared
{
    public static class CryptoUtility
    {
        public static string GenerateSessionKey()
        {
            var bytes = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(bytes);

            return HexUtility.ToString(bytes);
        }

        public static ICryptoTransform GetEncryptor(string sharedSecret, string session)
        {
            // make the shared secret a valid length for algorithm
            string key = HashUtility.ComputeStringHash(sharedSecret);

            var des = new AesCryptoServiceProvider
                {
                    Key = HexUtility.ToBytes(key),
                    IV = HexUtility.ToBytes(session)
                };

            return des.CreateEncryptor();
        }

        public static string EncryptString(this ICryptoTransform transformer, string s)
        {
            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, transformer, CryptoStreamMode.Write);

            using (var writer = new StreamWriter(cs))
            {
                writer.Write(s);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string DecryptString(this ICryptoTransform transformer, string s)
        {
            var bytes = Convert.FromBase64String(s);
            var ms = new MemoryStream(bytes);
            var cs = new CryptoStream(ms, transformer, CryptoStreamMode.Read);

            using (var reader = new StreamReader(cs))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
