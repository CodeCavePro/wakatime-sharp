﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WakaTime
{
    static class PythonManager
    {
        private const string CurrentPythonVersion = "3.5.0";
        private static string PythonBinaryLocation { get; set; }        

        internal static bool IsPythonInstalled()
        {
            return GetPython() != null;
        }

        internal static string GetPython()
        {
            if (PythonBinaryLocation == null)
                PythonBinaryLocation = GetEmbeddedPath();

            if (PythonBinaryLocation == null)
                PythonBinaryLocation = GetPathFromMicrosoftRegistry();

            if (PythonBinaryLocation == null)
                PythonBinaryLocation = GetPathFromFixedPath();

            return PythonBinaryLocation;
        }

        static string GetPathFromMicrosoftRegistry()
        {
            try
            {
                var regex = new Regex(@"""([^""]*)\\([^""\\]+(?:\.[^"".\\]+))""");
                var pythonKey = Registry.ClassesRoot.OpenSubKey(@"Python.File\shell\open\command");
                if (pythonKey == null)
                {
                    Logger.Debug("Couldn't find python's path through Microsft Registry. Please try repairing your Python installation.");
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
        }

        static string GetPathFromFixedPath()
        {
            string[] locations = {
                "pythonw",
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

        internal static string GetEmbeddedPath()
        {
            var path = Path.Combine(WakaTimeConstants.UserConfigDir, "python", "pythonw");
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

        internal static string PythonDownloadUrl
        {
            get
            {
                var arch = "win32";
                if (ProcessorArchitectureHelper.Is64BitOperatingSystem)
                    arch = "amd64";
                return string.Format("https://www.python.org/ftp/python/{0}/python-{0}-embed-{1}.zip", CurrentPythonVersion, arch);
            }
        }
    }
}
