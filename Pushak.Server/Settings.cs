using System;
using System.Configuration;
using System.IO;

namespace Pushak.Server
{
    public class Settings
    {
        public string SharedSecret
        {
            get { return ConfigurationManager.AppSettings["SharedSecret"]; }
        }

        public string DeploymentDirectory
        {
            get { return ConfigurationManager.AppSettings["DeploymentDirectory"]; }
        }

        public string ExecutableFile
        {
            get { return ConfigurationManager.AppSettings["ExecutableFile"]; }
        }

        public void Validate()
        {
            if (String.IsNullOrWhiteSpace(this.SharedSecret) || this.SharedSecret.Length < 16)
                throw new ApplicationException("You must specify a SharedSecret app setting of at least 16 characters.");

            if (String.IsNullOrWhiteSpace(this.DeploymentDirectory))
                throw new ApplicationException("You must specify a valid DeploymentDirectory app setting.");

            if (String.IsNullOrWhiteSpace(this.ExecutableFile) || !File.Exists(this.ExecutableFile))
                throw new ApplicationException("You must specify a valid ExecutableFile app setting.");
        }
    }
}
