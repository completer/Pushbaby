using System;
using System.Configuration;
using System.Linq;

namespace Pushbaby.Server
{
    public class Settings : ConfigurationSection
    {
        [ConfigurationProperty("endpoints")]
        public EndpointSettingsCollection EndpointSettingsCollection
        {
            get { return (EndpointSettingsCollection) base["endpoints"]; }
            set { base["endpoints"] = value; }
        }

        public void Validate()
        {
            foreach (var settings in this.EndpointSettingsCollection.Cast<EndpointSettings>())
            {
                if (String.IsNullOrWhiteSpace(settings.Uri))
                    settings.ThrowValidationException("You must specify a 'uri' setting. See the documentation");

                if (!settings.Uri.EndsWith("/"))
                    settings.ThrowValidationException("Uri must end with '/'.");

                if (String.IsNullOrWhiteSpace(settings.SharedSecret) || settings.SharedSecret.Length < 16)
                    settings.ThrowValidationException("You must specify a 'sharedSecret' setting of at least 16 characters.");

                if (String.IsNullOrWhiteSpace(settings.DeploymentDirectory))
                    settings.ThrowValidationException("You must specify a 'deploymentDirectory' setting.");
            }
        }
    }

    public class EndpointSettings : ConfigurationElement
    {
        [ConfigurationProperty("uri", IsKey = true, IsRequired = true)]
        public string Uri
        {
            get { return (string) base["uri"]; }
            set { base["uri"] = value; }
        }

        [ConfigurationProperty("sharedSecret", IsRequired = true)]
        public string SharedSecret
        {
            get { return (string) base["sharedSecret"]; }
            set { base["sharedSecret"] = value; }
        }

        [ConfigurationProperty("deploymentDirectory", IsRequired = true)]
        public string DeploymentDirectory
        {
            get { return (string) base["deploymentDirectory"]; }
            set { base["deploymentDirectory"] = value; }
        }

        [ConfigurationProperty("executableFile")]
        public string ExecutableFile
        {
            get { return (string) base["executableFile"]; }
            set { base["executableFile"] = value; }
        }

        [ConfigurationProperty("snakeLength", IsRequired = false, DefaultValue = 5)]
        public int SnakeLength
        {
            get { return (int) base["snakeLength"]; }
            set { base["snakeLength"] = value; }
        }

        internal void ThrowValidationException(string message)
        {
            throw new ConfigurationErrorsException(String.Format("Error validating endpoint '{0}'. ", this.Uri) + message);
        }
    }

    [ConfigurationCollection(typeof(EndpointSettings))]
    public class EndpointSettingsCollection : ConfigurationElementCollection
    {
        const string PropertyName = "endpoint";

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMapAlternate; }
        }

        protected override string ElementName
        {
            get { return PropertyName; }
        }

        protected override bool IsElementName(string elementName)
        {
            return elementName.Equals(PropertyName, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool IsReadOnly()
        {
            return false;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new EndpointSettings();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((EndpointSettings)(element)).Uri;
        }

        public EndpointSettings this[int i]
        {
            get { return (EndpointSettings) BaseGet(i); }
        }
    }
}
