using System;
using System.Configuration;

namespace Pushbaby.Client
{
    public class Settings
    {
        public string SharedSecret
        {
            get { return ConfigurationManager.AppSettings["sharedSecret"]; }
        }

        public int PollIntervalInSeconds
        {
            get { return Convert.ToInt32(ConfigurationManager.AppSettings["pollIntervalInSeconds"] ?? "5"); }
        }

        public void Validate()
        {
            if (this.SharedSecret.Length < 16)
                throw new ApplicationException("You must specify a SharedSecret app setting of at least 16 characters.");
        }
    }
}
