﻿using System;
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

    static class Logger
    {
        internal static void Debug(string message)
        {
            if (!WakaTimePackage.Debug)
                return;

            Log(LogLevel.Debug, message);
        }

        internal static void Error(string message, Exception ex = null)
        {
            var exceptionMessage = string.Format("{0}: {1}", message, ex);

            Log(LogLevel.HandledException, exceptionMessage);
        }

        internal static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        internal static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        private static string Log(LogLevel level, string message)
        {
            return string.Format("[Wakatime {0} {1}] {2}{3}", 
                Enum.GetName(level.GetType(), level), DateTime.Now.ToString("hh:mm:ss tt", CultureInfo.InvariantCulture), message, Environment.NewLine);
        }
    }
}
