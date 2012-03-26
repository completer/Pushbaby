using System.IO;

namespace Pushbaby.Server
{
    public interface IFileSystem
    {
        void CreateDirectory(string path);
    }

    public class FileSystem : IFileSystem
    {
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
    }
}
