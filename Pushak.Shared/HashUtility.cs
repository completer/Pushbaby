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
        public static string ComputeHash(string payload)
        {
            using (var stream = File.OpenRead(payload))
            {
                var hash = new MD5CryptoServiceProvider().ComputeHash(stream);

                return hash.Aggregate(new StringBuilder(32),
                    (sb, b) => sb.Append(b.ToString("X2")))
                    .ToString();
            }
        }
    }
}
