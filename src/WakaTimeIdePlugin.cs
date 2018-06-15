using System;
using System.IO;
using System.Net;
using System.Timers;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

#if NET45
using System.Web.Script.Serialization;
#else
using Newtonsoft.Json;
#endif

namespace WakaTime
{
    public abstract class WakaTimeIdePlugin<T> : IWakaTimeIdePlugin, IDisposable
    {
#region Fields

        protected string lastFile;
        protected string lastSolutionName;

        protected DateTime lastHeartbeat;
        protected readonly object threadLock = new object();

        protected static EditorInfo editorInfo;
        protected T editorObj;

        const int heartbeatFrequency = 2; // minutes
        protected static ConcurrentQueue<Heartbeat> heartbeatQueue;
        protected static Timer timer;

#endregion

#region Startup/Cleanup

        protected WakaTimeIdePlugin(T editor)
        {
            editorObj = editor;
            lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
            heartbeatQueue = new ConcurrentQueue<Heartbeat>();
            timer = new Timer();

            Initialize();
        }

        ~WakaTimeIdePlugin()
        {
            Dispose(false);
        }

        private void Initialize()
        {
            try
            {
                Logger.Initialize(GetLogger());
            }
            catch
            {
                Logger.Initialize(GetConsoleLogger()); // fallback on console logger
            }

            try
            {
                editorInfo = GetEditorInfo();

                Logger.Info(string.Format("Initializing WakaTime v{0}", editorInfo.PluginVersion));

                WakaTimeCli.Initialized += (s, e) =>
                {
                    if (string.IsNullOrEmpty(WakaTimeConfigFile.ApiKey))
                        PromptApiKey();

                    BindEditorEvents();

                    // Setup timer to process queued heartbeats every 8 seconds
                    timer.Interval = 1000 * 8;
                    timer.Elapsed += ProcessHeartbeats;
                    timer.Start();

                    Logger.Info(string.Format("Finished initializing WakaTime v{0}", editorInfo.PluginVersion));
                };

                DownloadProgress.Initialize(() => { return GetReporter(); });

                PythonManager.Initialized += (s, e) =>
                {
                    WakaTimeCli.Initialize();
                };

                CheckPrerequisites();
            }
            catch (WebException ex)
            {
                Logger.Error("Are you behind a proxy? Try setting a proxy in WakaTime Settings with format https://user:pass@host:port. Exception Traceback:", ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing Wakatime", ex);
            }
        }

        public void CheckPrerequisites()
        {
            PythonManager.Initialize();
        }

        private static ILogService GetConsoleLogger()
        {
            return new ConsoleLogger();
        }

        class ConsoleLogger : ILogService
        {
            public void Log(string message)
            {
                Console.WriteLine(message);
            }
        }

        public abstract ILogService GetLogger();

        public abstract void BindEditorEvents();

        public abstract EditorInfo GetEditorInfo();

        public abstract string GetActiveSolutionPath();

#endregion

#region Event Handlers

        public void OnDocumentOpened(string documentName)
        {
            try
            {
                var solutionName = GetActiveSolutionPath();
                // if (string.IsNullOrWhiteSpace(solutionName))
                //    return;

                HandleActivity(documentName, false);
            }
            catch (Exception ex)
            {
                Logger.Error("HandleDocumentOpened : " + ex.Message);
            }
        }

        public void OnDocumentChanged(string documentName)
        {
            try
            {
                var solutionName = GetActiveSolutionPath();
                // if (string.IsNullOrWhiteSpace(solutionName))
                //    return;

                HandleActivity(documentName, true);
            }
            catch (Exception ex)
            {
                Logger.Error("HandleDocumentChanged : " + ex.Message);
            }
        }

        public void OnSolutionOpened(string solutionPath)
        {
            try
            {
#if NET35
                if (solutionPath == null || string.IsNullOrEmpty(solutionPath.Trim()))
#else
                if (string.IsNullOrWhiteSpace(solutionPath))
#endif
                {
                    return;
                }

                lastSolutionName = solutionPath;
            }
            catch (Exception ex)
            {
                Logger.Error("HandleSolutionOpened : " + ex.Message);
            }
        }

#endregion

#region Methods

        protected void HandleActivity(string currentFile, bool isWrite)
        {
            try
            {
                if (currentFile == null)
                    return;

                if (!isWrite && lastFile != null && !IsEnoughTimePassed() && currentFile.Equals(lastFile))
                    return;

                lastFile = currentFile;
                lastHeartbeat = DateTime.UtcNow;

                AppendHeartbeat(currentFile, isWrite, DateTime.UtcNow, GetProjectName());
            }
            catch (Exception ex)
            {
                Logger.Error("Error appending heartbeat", ex);
            }
        }

        private static void AppendHeartbeat(string fileName, bool isWrite, DateTime time, string project = null)
        {
#if NET35
            Task.Factory.StartNew(() =>
#else
            Task.Run(() =>
#endif
            {
                try
                {
                    var heartbeat = new Heartbeat
                    {
                        FileName = fileName,
                        DateTime = time,
                        IsWrite = isWrite,
                        Project = project
                    };
                    heartbeatQueue.Enqueue(heartbeat);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error appending heartbeat", ex);
                }
            });
        }

        private static void ProcessHeartbeats(object sender, ElapsedEventArgs e)
        {
#if NET35
            Task.Factory.StartNew(() =>
#else
            Task.Run(() =>
#endif
            {
                try
                {
                    ProcessHeartbeats();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error processing heartbeats", ex);
                }
            });
        }

        private static void ProcessHeartbeats()
        {
            var pythonBinary = PythonManager.GetPython();
#if NET35
            if (pythonBinary == null || string.IsNullOrEmpty(pythonBinary.Trim()))
#else
            if (string.IsNullOrWhiteSpace(pythonBinary))
#endif
            {
                Logger.Error("Could not send heartbeat because python is not installed");
                return;
            }

            // get first heartbeat from queue
            Heartbeat heartbeat;
            bool gotOne = heartbeatQueue.TryDequeue(out heartbeat);
            if (!gotOne)
                return;

            // remove all extra heartbeats from queue
            var extraHeartbeats = new List<Heartbeat>();
            Heartbeat hbOut;
            while (heartbeatQueue.TryDequeue(out hbOut))
            {
                extraHeartbeats.Add(hbOut.Clone());
            }

            bool hasExtraHeartbeats = extraHeartbeats.Any();
            var cliParams = new PythonCliParameters
            {
                Key = WakaTimeConfigFile.ApiKey,
                Plugin = string.Format("{0}/{1} {2}/{3}", editorInfo.Name, editorInfo.Version, editorInfo.PluginKey, editorInfo.PluginVersion),
                File = heartbeat.FileName,
                Time = heartbeat.Timestamp,
                IsWrite = heartbeat.IsWrite,
                HasExtraHeartbeats = hasExtraHeartbeats,
            };

            string extraHeartbeatsJSON = null;
            if (hasExtraHeartbeats)
            {
#if NET45
                var serializer = new JavaScriptSerializer();
                serializer.RegisterConverters(new JavaScriptConverter[] { new DataContractJavaScriptConverter(true) });
                extraHeartbeatsJSON = serializer.Serialize(extraHeartbeats);
#else
                extraHeartbeatsJSON = JsonConvert.SerializeObject(extraHeartbeats, Formatting.None);
#endif
            }

            var process = new RunProcess(pythonBinary, cliParams.ToArray());
            if (WakaTimeConfigFile.Debug)
            {
                Logger.Debug(string.Format("[\"{0}\", \"{1}\"]", pythonBinary, string.Join("\", \"", cliParams.ToArray(true))));
                process.Run(extraHeartbeatsJSON);
                if (process.Output != null && process.Output != "")
                    Logger.Debug(process.Output);
                if (process.Error != null && process.Error != "")
                    Logger.Debug(process.Error);
            }
            else
            {
                process.RunInBackground(extraHeartbeatsJSON);
            }

            if (!process.Success)
            {
                Logger.Error("Could not send heartbeat.");
                if (process.Output != null && process.Output != "")
                    Logger.Error(process.Output);
                if (process.Error != null && process.Error != "")
                    Logger.Error(process.Error);
            }
        }

        protected bool IsEnoughTimePassed()
        {
            return lastHeartbeat < DateTime.UtcNow.AddMinutes(-1 * heartbeatFrequency);
        }

        public abstract void PromptApiKey();

        public abstract void SettingsPopup();

        public virtual string GetProjectName()
        {
            if (!string.IsNullOrEmpty(lastSolutionName))
                return Path.GetFileNameWithoutExtension(lastSolutionName);

            var solutionPath = GetActiveSolutionPath();
            return (string.IsNullOrEmpty(solutionPath))
                ? (lastSolutionName = Path.GetFileNameWithoutExtension(lastSolutionName))
                : null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract void Dispose(bool disposing);

        public abstract IDownloadProgressReporter GetReporter();

#endregion
    }
}

