using System;
using System.Globalization;

namespace WakaTime
{
    public enum LogLevel
    {
        Debug = 1,
        Info,
        Warning,
        HandledException
    };

    public static class Logger
    {
        private static ILogService _logger;

        public static void Initialize(ILogService logger)
        {
            if (logger == null)
                throw new ArgumentNullException();

            _logger = logger;
        }

        public static void Debug(string message)
        {
            if (!WakaTimeConfigFile.Debug)
                return;

            Log(LogLevel.Debug, message);
        }

        public static void Error(string message, Exception ex = null)
        {
            Log(LogLevel.HandledException, string.Format("{0}: {1}", message, ex));
        }

        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        private static void Log(LogLevel level, string message)
        {
            message = string.Format("[Wakatime {0} {1}] {2}{3}",
                    Enum.GetName(level.GetType(), level),
                    DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture),
                    message,
                    Environment.NewLine);

            _logger.Log(message);
        }
    }
}
