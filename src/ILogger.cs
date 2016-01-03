using System;

namespace WakaTime
{
    public interface ILogger
    {
        void Debug(string message);

        void Info(string message);

        void Warning(string message);

        void Error(string message);

        void Error(Exception error);
    }
}
