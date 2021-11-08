using GameServer.Core.Database;
using GameServer.Core.Logger;
using GameServer.Core.Settings;

namespace GameServer.Logger
{
    public class PerformanceLogger : IPerformanceLogger
    {
        private readonly LoggingSettings loggingSettings;
        private readonly ILoggerDataProvider loggerProvider;

        public PerformanceLogger(LoggingSettings loggingSettings, ILoggerDataProvider loggerProvider)
        {
            this.loggingSettings = loggingSettings;
            this.loggerProvider = loggerProvider;
        }

        public void Dispose()
        {
            loggerProvider.Dispose();
        }
    }
}