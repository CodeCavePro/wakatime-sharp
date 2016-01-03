using System;
using System.Globalization;

namespace WakaTime
{
    internal enum LogLevel
    {
        Debug = 1,
        Info,
        Warning,
        HandledException
    };

    internal static class Logger
    {
        private static ILogger _logger;

        internal static void Initialize(ILogger logger)
        {
            _logger = logger;
        }

        internal static void Debug(string message)
        {
            if (!WakaTimeConfigFile.Debug)
                return;

            Log(LogLevel.Debug, message);
        }

        internal static void Error(string message, Exception ex = null)
        {
            Log(LogLevel.HandledException, string.Format("{0}: {1}", message, ex));
        }

        internal static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        internal static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        private static void Log(LogLevel level, string message)
        {
            {
                message = string.Format("[Wakatime {0} {1}] {2}{3}",
                    Enum.GetName(level.GetType(), level),
                    DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture),
                    message,
                    Environment.NewLine);

                switch (level)
                {
                    case LogLevel.Debug:
                        _logger.Debug(message);
                        break;
                    case LogLevel.Info:
                        _logger.Info(message);
                        break;
                    case LogLevel.Warning:
                        _logger.Warning(message);
                        break;
                    case LogLevel.HandledException:
                        _logger.Error(message);
                        break;
                }
            }
        }
    }
}
