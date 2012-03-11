using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Pushak.Shared
{
    public static class HashUtility
    {
        public static string ComputeFileHash(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var hash = new MD5CryptoServiceProvider().ComputeHash(stream);
                return HexUtility.ToString(hash);
            }
        }

        public static string ComputeStringHash(string s)
        {
            var bytes = Encoding.Unicode.GetBytes(s);
            var hash = new MD5CryptoServiceProvider().ComputeHash(bytes);
            return HexUtility.ToString(hash);
        }
    }
}
