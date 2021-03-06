﻿using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Win32;

#if NET35 || NET40
using Ionic.Zip;
#else
using System.IO.Compression;
#endif

namespace WakaTime
{
    static class PythonManager
    {
        private const string CurrentPythonVersion = "3.5.1";
        private static string PythonBinaryLocation { get; set; }

        internal static void Initialize()
        {
            // Make sure python is installed
            if (!IsPythonInstalled())
            {
                DownloadPython();
            }
            else
            {
                OnInitialized();
            }
        }

        internal static string GetPython()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    if (PythonBinaryLocation == null)
                        PythonBinaryLocation = GetEmbeddedPath();

                    if (PythonBinaryLocation == null)
                        PythonBinaryLocation = GetPathFromMicrosoftRegistry();

                    goto default;

                default:
                    return PythonBinaryLocation ?? (PythonBinaryLocation = GetPathFromFixedPath());
            }
        }

        static bool IsPythonInstalled()
        {
            return GetPython() != null;
        }

        static string GetPathFromMicrosoftRegistry()
        {
#if !NETSTANDARD
            try
            {
                var regex = new Regex(@"""([^""]*)\\([^""\\]+(?:\.[^"".\\]+))""");
                var pythonKey = Registry.ClassesRoot.OpenSubKey(@"Python.File\shell\open\command");
                if (pythonKey == null)
                {
                    Logger.Debug("Couldn't find python's path through Microsoft Registry. Please try repairing your Python installation.");
                    return null;
                }
                var python = pythonKey.GetValue(null).ToString();
                var match = regex.Match(python);

                if (!match.Success) return null;

                var directory = match.Groups[1].Value;
                var fullPath = Path.Combine(directory, "pythonw");
                var process = new RunProcess(fullPath, "--version");

                process.Run();

                if (!process.Success)
                    return null;

                Logger.Debug(string.Format("Python found from Microsoft Registry: {0}", fullPath));

                return fullPath;
            }
            catch (Exception ex)
            {
                Logger.Error("GetPathFromMicrosoftRegistry:", ex);
                return null;
            }
#else
            return null;
#endif
        }

        static string GetPathFromFixedPath()
        {
            string[] locations = {
                "pythonw",
                "python3",
                "python",
                "\\Python37\\pythonw",
                "\\Python36\\pythonw",
                "\\Python35\\pythonw",
                "\\Python34\\pythonw",
                "\\Python33\\pythonw",
                "\\Python32\\pythonw",
                "\\Python31\\pythonw",
                "\\Python30\\pythonw",
                "\\Python27\\pythonw",
                "\\Python26\\pythonw",
                "\\python37\\pythonw",
                "\\python36\\pythonw",
                "\\python35\\pythonw",
                "\\python34\\pythonw",
                "\\python33\\pythonw",
                "\\python32\\pythonw",
                "\\python31\\pythonw",
                "\\python30\\pythonw",
                "\\python27\\pythonw",
                "\\python26\\pythonw",
                "\\Python37\\python",
                "\\Python36\\python",
                "\\Python35\\python",
                "\\Python34\\python",
                "\\Python33\\python",
                "\\Python32\\python",
                "\\Python31\\python",
                "\\Python30\\python",
                "\\Python27\\python",
                "\\Python26\\python",
                "\\python37\\python",
                "\\python36\\python",
                "\\python35\\python",
                "\\python34\\python",
                "\\python33\\python",
                "\\python32\\python",
                "\\python31\\python",
                "\\python30\\python",
                "\\python27\\python",
                "\\python26\\python",
            };

            foreach (var location in locations)
            {
                try
                {
                    var process = new RunProcess(location, "--version");
                    process.Run();

                    if (!process.Success) continue;
                }
                catch { /*ignored*/ }

                Logger.Debug(string.Format("Python found by Fixed Path: {0}", location));

                return location;
            }

            return null;
        }

        static string GetAppDataDirectory()
        {
            var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(roamingFolder, "WakaTime");

            // Create folder if it does not exist
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            return appFolder;
        }

        static string GetEmbeddedPath()
        {
            var appDataPath = GetAppDataDirectory();
            var path = Path.Combine(appDataPath, "python");
            path = Path.Combine(path, "pythonw");

            try
            {
                var process = new RunProcess(path, "--version");

                process.Run();

                if (!process.Success)
                    return null;

                Logger.Debug(string.Format("Python found from embedded location: {0}", path));

                return path;
            }
            catch (Exception ex)
            {
                Logger.Error("GetEmbeddedPath:", ex);
                return null;
            }
        }

        static string PythonDownloadUrl
        {
            get
            {
                var arch = ProcessorArchitectureHelper.Is64BitOperatingSystem ? "amd64" : "win32";
                return string.Format("https://www.python.org/ftp/python/{0}/python-{0}-embed-{1}.zip", CurrentPythonVersion, arch);
            }
        }

        static public void DownloadPython()
        {
            Logger.Debug("Downloading python...");

            var tempDir = Path.GetTempPath();
            var localFile = Path.Combine(tempDir, "python.zip");
            var client = new WebClient
            {
                Proxy = WakaTimeConfigFile.GetProxy() // Check for proxy setting
            };

            // Download embeddable python
            DownloadProgress.Show(PythonDownloadUrl);
            client.DownloadProgressChanged += (s, e) => { DownloadProgress.Report(e); };
            client.DownloadFileCompleted += (s, e) =>
            {
                try
                {
                    DownloadProgress.Complete(e);
                    Logger.Debug("Finished downloading python.");

                    // Extract wakatime cli zip file
                    var appDataPath = GetAppDataDirectory();

                    // Extract wakatime cli zip file
#if NET35 || NET40
                    using (var zipFile = new ZipFile(localFile))
                    {
                        zipFile.ExtractAll(Path.Combine(appDataPath, "python"), ExtractExistingFileAction.OverwriteSilently);
                    }
#else
                    ZipFile.ExtractToDirectory(localFile, Path.Combine(appDataPath, "python"));
#endif
                    Logger.Debug(string.Format("Finished extracting python: {0}", Path.Combine(appDataPath, "python")));

                    // Delete downloaded file
                    File.Delete(localFile);
                }
                finally
                {
                    OnInitialized();
                }
            };

            client.DownloadFileAsync(new Uri(PythonDownloadUrl), localFile);
        }

        private static void OnInitialized()
        {
            var handler = Initialized;
            if (handler != null)
                handler.Invoke(null, EventArgs.Empty);
        }

        public static event EventHandler<EventArgs> Initialized;
    }
}
