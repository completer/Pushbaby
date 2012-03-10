using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Pushak.Shared
{
    public static class CryptoUtility
    {
        public static string GenerateKey()
        {
            var bytes = new byte[16];
            new RNGCryptoServiceProvider().GetBytes(bytes);

            return bytes.Aggregate(new StringBuilder(),
                (sb, b) => sb.Append(b.ToString("X2")))
                .ToString();
        }
    }
}
