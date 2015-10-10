﻿using System;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using WakaTime.Forms;
using Task = System.Threading.Tasks.Task;
using System.Net;
using System.Text.RegularExpressions;

namespace WakaTime
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.GuidWakaTimePkgString)]
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    public sealed class WakaTimePackage : Package
    {
        #region Fields
        private static string _version = string.Empty;
        private static string _editorVersion = string.Empty;
        private static WakaTimeConfigFile _wakaTimeConfigFile;
        private static SettingsForm _settingsForm;

        private static DTE2 _objDte;
        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;
        private SolutionEvents _solutionEvents;

        // Settings
        public static bool Debug;
        public static string ApiKey;
        public static string Proxy;

        static readonly PythonCliParameters PythonCliParameters = new PythonCliParameters();
        private static string _lastFile;
        private static string _solutionName = string.Empty;
        DateTime _lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        #endregion

        #region Startup/Cleanup
        protected override void Initialize()
        {
            base.Initialize();
            Task.Run(() => {
                InitializeAsync();
            });
        }

        public void InitializeAsync()
        {
            _version = string.Format("{0}.{1}.{2}", CoreAssembly.Version.Major, CoreAssembly.Version.Minor, CoreAssembly.Version.Build);

            try
            {
                Logger.Info(string.Format("Initializing WakaTime v{0}", _version));

                // VisualStudio Object
                _objDte = (DTE2)GetService(typeof(DTE));
                _docEvents = _objDte.Events.DocumentEvents;
                _windowEvents = _objDte.Events.WindowEvents;
                _solutionEvents = _objDte.Events.SolutionEvents;
                _editorVersion = _objDte.Version;

                // Settings Form
                _settingsForm = new SettingsForm();
                _settingsForm.ConfigSaved += SettingsFormOnConfigSaved;

                // Load config file
                _wakaTimeConfigFile = new WakaTimeConfigFile();
                GetSettings();

                // Make sure python is installed
                if (!PythonManager.IsPythonInstalled())
                {
                    var dialogResult = MessageBox.Show(@"Let's download and install Python now?",
                        @"WakaTime requires Python", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dialogResult == DialogResult.Yes)
                    {
                        var url = PythonManager.PythonDownloadUrl;
                        Downloader.DownloadAndInstallPython(url, WakaTimeConstants.UserConfigDir);
                    }
                    else
                        MessageBox.Show(
                            @"Please install Python (https://www.python.org/downloads/) and restart Visual Studio to enable the WakaTime plugin.",
                            @"WakaTime", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                if (!DoesCliExist() || !IsCliLatestVersion())
                {
                    try
                    {
                        Directory.Delete(string.Format("{0}\\wakatime-master", WakaTimeConstants.UserConfigDir), true);
                    }
                    catch { /* ignored */ }

                    Downloader.DownloadCli(WakaTimeConstants.CliUrl, WakaTimeConstants.UserConfigDir);
                }

                if (string.IsNullOrEmpty(ApiKey))
                    PromptApiKey();

                // Add our command handlers for menu (commands must exist in the .vsct file)
                var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (mcs != null)
                {
                    // Create the command for the menu item.
                    var menuCommandId = new CommandID(GuidList.GuidWakaTimeCmdSet, (int)PkgCmdIdList.UpdateWakaTimeSettings);
                    var menuItem = new MenuCommand(MenuItemCallback, menuCommandId);
                    mcs.AddCommand(menuItem);
                }

                // setup event handlers
                _docEvents.DocumentOpened += DocEventsOnDocumentOpened;
                _docEvents.DocumentSaved += DocEventsOnDocumentSaved;
                _windowEvents.WindowActivated += WindowEventsOnWindowActivated;
                _solutionEvents.Opened += SolutionEventsOnOpened;

                Logger.Info(string.Format("Finished initializing WakaTime v{0}", _version));
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing Wakatime", ex);
            }
        }

        #endregion

        #region Event Handlers
        private void DocEventsOnDocumentOpened(Document document)
        {
            try
            {
                HandleActivity(document.FullName, false);
            }
            catch (Exception ex)
            {
                Logger.Error("DocEventsOnDocumentOpened", ex);
            }
        }

        private void DocEventsOnDocumentSaved(Document document)
        {
            try
            {
                HandleActivity(document.FullName, true);
            }
            catch (Exception ex)
            {
                Logger.Error("DocEventsOnDocumentSaved", ex);
            }
        }

        private void WindowEventsOnWindowActivated(Window gotFocus, Window lostFocus)
        {
            try
            {
                var document = _objDte.ActiveWindow.Document;
                if (document != null)
                    HandleActivity(document.FullName, false);
            }
            catch (Exception ex)
            {
                Logger.Error("WindowEventsOnWindowActivated", ex);
            }
        }

        private void SolutionEventsOnOpened()
        {
            try
            {
                _solutionName = _objDte.Solution.FullName;
            }
            catch (Exception ex)
            {
                Logger.Error("SolutionEventsOnOpened", ex);
            }
        }
        #endregion

        #region Methods

        private static void SettingsFormOnConfigSaved(object sender, EventArgs eventArgs)
        {
            _wakaTimeConfigFile.Read();
            GetSettings();
        }

        private static void GetSettings()
        {
            ApiKey = _wakaTimeConfigFile.ApiKey;
            Debug = _wakaTimeConfigFile.Debug;
            Proxy = _wakaTimeConfigFile.Proxy;
        }

        private void HandleActivity(string currentFile, bool isWrite)
        {
            if (currentFile == null)
                return;

            if (!isWrite && _lastFile != null && !EnoughTimePassed() && currentFile.Equals(_lastFile))
                return;

            Task.Run(() =>
            {
                SendHeartbeat(currentFile, isWrite);
            });


            _lastFile = currentFile;
            _lastHeartbeat = DateTime.UtcNow;
        }

        private bool EnoughTimePassed()
        {
            return _lastHeartbeat < DateTime.UtcNow.AddMinutes(-1);
        }

        public static void SendHeartbeat(string fileName, bool isWrite)
        {
            PythonCliParameters.Key = ApiKey;
            PythonCliParameters.File = fileName;
            PythonCliParameters.Plugin = string.Format("{0}/{1} {2}/{3}", WakaTimeConstants.EditorName, _editorVersion, WakaTimeConstants.PluginName, _version);
            PythonCliParameters.IsWrite = isWrite;
            PythonCliParameters.Project = GetProjectName();

            var pythonBinary = PythonManager.GetPython();
            if (pythonBinary != null)
            {
                var process = new RunProcess(pythonBinary, PythonCliParameters.ToArray());
                if (Debug)
                {
                    Logger.Debug(string.Format("[\"{0}\", \"{1}\"]", pythonBinary, string.Join("\", \"", PythonCliParameters.ToArray(true))));
                    process.Run();
                    Logger.Debug(string.Format("CLI STDOUT: {0}", process.Output));
                    Logger.Debug(string.Format("CLI STDERR: {0}", process.Error));
                }
                else
                    process.RunInBackground();

                if(!process.Success)
                    Logger.Error(string.Format("Could not send heartbeat: {0}", process.Error));
            }
            else
                Logger.Error("Could not send heartbeat because python is not installed");
        }

        static bool DoesCliExist()
        {
            return File.Exists(PythonCliParameters.Cli);
        }

        static bool IsCliLatestVersion()
        {
            var process = new RunProcess(PythonManager.GetPython(), PythonCliParameters.Cli, "--version");
            process.Run();

            if (process.Success)
            {
                var currentVersion = process.Error.Trim();
                Logger.Info(string.Format("Current wakatime-cli version is {0}", currentVersion));

                Logger.Info("Checking for updates to wakatime-cli...");
                var latestVersion = WakaTimeConstants.LatestWakaTimeCliVersion();

                if (currentVersion.Equals(latestVersion))
                {
                    Logger.Info("wakatime-cli is up to date.");
                    return true;
                }
                else
                {
                    Logger.Info(string.Format("Found an updated wakatime-cli v{0}", latestVersion));
                }

            }
            return false;
        }

        private static void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                SettingsPopup();
            }
            catch (Exception ex)
            {
                Logger.Error("MenuItemCallback", ex);
            }
        }

        private static void PromptApiKey()
        {
            var form = new ApiKeyForm();
            form.ShowDialog();
        }

        private static void SettingsPopup()
        {
            _settingsForm.ShowDialog();
        }

        private static string GetProjectName()
        {
            return !string.IsNullOrEmpty(_solutionName)
                ? Path.GetFileNameWithoutExtension(_solutionName)
                : (_objDte.Solution != null && !string.IsNullOrEmpty(_objDte.Solution.FullName))
                    ? Path.GetFileNameWithoutExtension(_objDte.Solution.FullName)
                    : string.Empty;
        }

        /// <summary>
        /// Get a Proxy object by parsing the proxy string in the .wakatime.cfg file
        /// </summary>
        /// <returns>Proxy object set in .wakatime.cfg</returns>
        public static WebProxy GetProxy()
        {
            WebProxy proxy = null;

            try
            {
                string proxyStr = WakaTimePackage.Proxy;

                // Regex that matches proxy address with authentication
                Regex regProxyWithAuth = new Regex(@"\s*(https?:\/\/)?([^\s:]+):([^\s:]+)@([^\s:]+):(\d+)\s*");
                Match match = regProxyWithAuth.Match(proxyStr);

                if (match.Success)
                {
                    var username = match.Groups[2].Value;
                    var password = match.Groups[3].Value;
                    var address = match.Groups[4].Value;
                    var port = match.Groups[5].Value;

                    NetworkCredential credentials = new NetworkCredential(username, password);
                    proxy = new WebProxy(String.Join(":", address, port), true, null, credentials);

                    Logger.Debug("A proxy with authentication will be used.");
                    return proxy;
                }

                // Regex that matches proxy address and port(no authentication)
                Regex regProxy = new Regex(@"\s*(https?:\/\/)?([^\s@]+):(\d+)\s*");
                match = regProxy.Match(proxyStr);

                if (match.Success)
                {
                    var address = match.Groups[2].Value;
                    var port = Int32.Parse(match.Groups[3].Value);

                    proxy = new WebProxy(address, port);

                    Logger.Debug("A proxy will be used.");
                    return proxy;
                }

                Logger.Debug("No proxy will be used. It's either not set or badly formatted.");
            }
            catch (Exception ex)
            {
                Logger.Error("Exception while parsing the proxy string from WakaTime config file. No proxy will be used.", ex);
            }

            return proxy;
        }
        #endregion

        static class CoreAssembly
        {
            static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }
    }
}
