using System.Configuration;
using System.Linq;
using FluentAssertions;
using Pushbaby.Server;

namespace Pushbaby.Tests.Pushbaby.Server
{
    class when_loading_basic_valid_config_file : context
    {
        Settings settings;

        protected override void when()
        {
            this.settings = settings_loader.load("basic_valid.config").GetSection("pushbaby") as Settings;
        }

        [then]
        public void should_load_config_section()
        {
            this.settings.Should().NotBeNull();
            this.settings.EndpointSettingsCollection.Cast<EndpointSettings>().Should().HaveCount(1);
        }
    }

    class when_validating_valid_config_file : context
    {
        Settings settings;

        protected override void given()
        {
            this.settings = settings_loader.load("basic_valid.config").GetSection("pushbaby") as Settings;
        }

        protected override void when()
        {
            this.settings.Validate();
        }

        [then]
        public void should_not_throw_config_error()
        {
            this.Exception.Should().BeNull();
        }
    }

    class when_validating_config_file_with_shared_secret_too_short : context
    {
        Settings settings;

        protected override void given()
        {
            this.settings = settings_loader.load("shared_secret_too_short.config").GetSection("pushbaby") as Settings;
        }

        [throws]
        protected override void when()
        {
            this.settings.Validate();
        }

        [then]
        public void should_throw_useful_config_error()
        {
            this.Exception.Should().BeOfType<ConfigurationErrorsException>();
            this.Exception.Message.Should().Contain("at least 16 characters");
        }
    }

    class when_validating_config_file_with_uri_with_no_trailing_slash : context
    {
        Settings settings;

        protected override void given()
        {
            this.settings = settings_loader.load("uri_with_no_trailing_slash.config").GetSection("pushbaby") as Settings;
        }

        [throws]
        protected override void when()
        {
            this.settings.Validate();
        }

        [then]
        public void should_throw_useful_config_error()
        {
            this.Exception.Should().BeOfType<ConfigurationErrorsException>();
            this.Exception.Message.Should().Contain("must end with '/'");
        }
    }
}
