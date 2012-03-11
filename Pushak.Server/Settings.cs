using System;
using System.Configuration;

namespace Pushak.Server
{
    public class Settings
    {
        public string SharedSecret
        {
            get { return ConfigurationManager.AppSettings["SharedSecret"]; }
        }

        public void Validate()
        {
            if (this.SharedSecret.Length < 16)
                throw new ApplicationException("You must specify a SharedSecret app setting of at least 16 characters.");
        }
    }
}
