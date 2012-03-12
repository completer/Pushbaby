using System;
using System.Linq;
using System.Text;

namespace Pushbaby.Shared
{
    public static class HexUtility
    {
        public static string ToString(byte[] hash)
        {
            return hash.Aggregate(new StringBuilder(),
                (sb, b) => sb.Append(b.ToString("x2")))
                .ToString();
        }

        public static byte[] ToBytes(string s)
        {
            int numberChars = s.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(s.Substring(i, 2), 16);
            return bytes;            
        }
    }
}
