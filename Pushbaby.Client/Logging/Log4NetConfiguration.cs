using System;
using System.IO;
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace Pushbaby.Client.Logging
{
    public static class Log4NetConfiguration
    {
        public static void Configure()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();
            hierarchy.Root.RemoveAllAppenders();

            // see http://logging.apache.org/log4net/release/sdk/log4net.Layout.PatternLayout.html

            var layout = new PatternLayout
            {
                ConversionPattern = "%date{dd-MMM-yyyy HH:mm:ss}  %-5level  %message%n%exception%n"
            };

            var fileAppender = new RollingFileAppender
            {
                Layout = layout,
                AppendToFile = true,
                File = "log.txt",
                LockingModel = new FileAppender.MinimalLock(),
            };

            var consoleAppender = new ConsoleAppender
            {
                Layout = layout,
            };

            layout.ActivateOptions();

            fileAppender.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(fileAppender);

            consoleAppender.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(consoleAppender);
        }
    }
}
