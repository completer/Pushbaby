using System.IO;

namespace Pushak.Shared
{
    public static class StreamUtility
    {
        public static void Copy(Stream input, Stream output)
        {
            var buffer = new byte[1024];
            int bytesRead;

            do
            {
                bytesRead = input.Read(buffer, 0, 1024);
                output.Write(buffer, 0, bytesRead);
            }
            while (bytesRead > 0);
        }
    }
}
