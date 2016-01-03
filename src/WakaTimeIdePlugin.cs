using System;
using System.IO;
using System.Threading.Tasks;
using WakaTime.Forms;

namespace WakaTime
{
    public abstract class WakaTimeIdePlugin<T> : IWakaTimeIdePlugin, IDisposable
    {
        #region Fields

        protected string lastFile;
        protected string lastSolutionName;

        protected DateTime lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        protected readonly object threadLock = new object();

        protected EditorInfo editorInfo;
        protected T editorObj;

        #endregion

        #region Startup/Cleanup

        protected WakaTimeIdePlugin(T editor)
        {
            editorObj = editor;
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
                editorInfo = GetEditorInfo();

                Logger.Initialize(GetLogger());
                Logger.Info("Initializing WakaTime v" + editorInfo.PluginVersion);

                PythonManager.Initialize();
                WakaTimeCli.Initialize();

                if (string.IsNullOrEmpty(WakaTimeConfigFile.ApiKey))
                    PromptApiKey();

                BindEditorEvents();

                Logger.Info("Finished initializing WakaTime v" + editorInfo.PluginVersion);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
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
                if (string.IsNullOrWhiteSpace(solutionName))
                    return;

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
                if (string.IsNullOrWhiteSpace(solutionName))
                    return;

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
                if (string.IsNullOrWhiteSpace(solutionPath))
                    return;

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
            if (currentFile == null)
                return;
            
            if (!isWrite && lastFile != null && !IsEnoughTimePassed() && currentFile.Equals(lastFile))
                return;

            var args = new PythonCliParameters
            {
                File = currentFile,
                Plugin = string.Format("{0}/{1} {2}/{3}", editorInfo.Name, editorInfo.Version, editorInfo.PluginName, editorInfo.PluginVersion),
                IsWrite = isWrite,
                Project = GetProjectName()
            };

            Task.Run(() =>
            {
                lock (threadLock)
                {
                    WakaTimeCli.SendHeartbeat(args);
                }
            });

            lastFile = currentFile;
            lastHeartbeat = DateTime.UtcNow;
        }

        protected bool IsEnoughTimePassed()
        {
            return lastHeartbeat < DateTime.UtcNow.AddMinutes(-1);
        }

        public void PromptApiKey()
        {
            using (var form = new ApiKeyForm())
            {
                form.Show();
            }
        }

        public void SettingsPopup()
        {
            using (var form = new SettingsForm())
            {
                form.Show();
            }
        }

        protected string GetProjectName()
        {
            if (!string.IsNullOrEmpty(lastSolutionName))
                return Path.GetFileNameWithoutExtension(lastSolutionName);

            var solutionPath = GetActiveSolutionPath();
            return (string.IsNullOrWhiteSpace(solutionPath))
                ? (lastSolutionName = Path.GetFileNameWithoutExtension(lastSolutionName))
                : null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract void Dispose(bool disposing);

        #endregion
    }
}

