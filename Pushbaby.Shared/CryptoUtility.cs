using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Pushbaby.Shared
{
    public static class CryptoUtility
    {
        public static string GenerateSessionKey()
        {
            var bytes = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(bytes);

            return HexUtility.ToString(bytes);
        }

        public static SymmetricAlgorithm GetAlgorithm(string sharedSecret, string session)
        {
            // http://stackoverflow.com/questions/202011/encrypt-decrypt-string-in-net

            if (string.IsNullOrEmpty(sharedSecret))
                throw new ArgumentNullException("sharedSecret");

            if (string.IsNullOrEmpty(sharedSecret))
                throw new ArgumentNullException("session");

            var salt = Encoding.ASCII.GetBytes("o6806642kbM7c5");

            // generate the key from the shared secret + session and the salt
            var key = new Rfc2898DeriveBytes(sharedSecret + session, salt);

            var a = new AesManaged();
            a.Key = key.GetBytes(a.KeySize / 8);
            a.IV = key.GetBytes(a.BlockSize / 8);

            return a;
        }

        public static string EncryptString(this SymmetricAlgorithm algorithm, string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                throw new ArgumentNullException("plaintext");

            var encryptor = algorithm.CreateEncryptor();

            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);

            using (var writer = new StreamWriter(cs))
            {
                writer.Write(plaintext);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string DecryptString(this SymmetricAlgorithm algorithm, string cipher)
        {
            if (string.IsNullOrEmpty(cipher))
                throw new ArgumentNullException("cipher");

            var decryptor = algorithm.CreateDecryptor();

            var bytes = Convert.FromBase64String(cipher);
            var ms = new MemoryStream(bytes);
            var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);

            using (var reader = new StreamReader(cs))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
