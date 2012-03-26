using System.Configuration;
using System.IO;
using System.Reflection;
using Pushbaby.Shared;

namespace Pushbaby.Tests.Pushbaby.Server
{
    public static class settings_loader
    {
        public static Configuration load(string configFileName)
        {
            // test config files are in embedded resources for convenience
            string resource = "Pushbaby.Tests.Pushbaby.Server.settings." + configFileName;

            // need to save to a file so can use ConfigurationManager.OpenMappedExeConfiguration
            string path = Path.Combine(Path.GetTempPath(), configFileName);

            using (var input = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            using (var output = File.Create(path))
            {
                StreamUtility.Copy(input, output);
            }

            var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = path };
            var config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

            return config;
        }
    }
}
