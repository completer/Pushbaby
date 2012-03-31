using System.Collections.Generic;
using System.IO;

namespace Pushbaby.Server
{
    public interface IFileSystem
    {
        void CreateDirectory(string path);
        IEnumerable<string> GetDirectories(string path, string pattern);
    }

    public class FileSystem : IFileSystem
    {
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public IEnumerable<string> GetDirectories(string path, string pattern)
        {
            return Directory.GetDirectories(path, pattern);
        }
    }
}
