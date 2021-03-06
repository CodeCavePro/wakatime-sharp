﻿using System;
using System.ComponentModel;
using System.Net;

namespace WakaTime
{
    public static class DownloadProgress
    {
        private static IDownloadProgressReporter _progressReporter;
        private static Func<IDownloadProgressReporter> _initReporter;

        internal static void Initialize(Func<IDownloadProgressReporter> initReporter)
        {
            _initReporter = initReporter;
        }

        public static void Show(string message = "")
        {
            _progressReporter = _initReporter?.Invoke();
            _progressReporter.Show(message);
        }

        public static void Report(DownloadProgressChangedEventArgs e)
        {
            if (_progressReporter == null)
                return;

            _progressReporter.Report(e);
        }

        public static void Complete(AsyncCompletedEventArgs e)
        {
            if (_progressReporter == null)
                return;

            try
            {
                _progressReporter.Close(e);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
    }

    public interface IDownloadProgressReporter : IProgress<DownloadProgressChangedEventArgs>, IDisposable
    {
        void Show(string message = "");

        void Close(AsyncCompletedEventArgs e);
    }
}
