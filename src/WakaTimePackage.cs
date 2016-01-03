using System;
using System.IO;
using System.Threading.Tasks;
using WakaTime.Forms;

namespace WakaTime
{
    public abstract class WakaTimePackage : IWakaTimePackage
    {
        #region Fields

        protected string lastFile;
        protected string lastSolutionName;

        protected DateTime lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        private static readonly object ThreadLock = new object();

        protected EditorInfo editorInfo;

        #endregion

        #region Startup/Cleanup

        public void Initialize()
        {
            try
            {
                editorInfo = GetEditorInfo();

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

        public abstract ILogger GetLogger();

        public abstract void BindEditorEvents();

        public abstract EditorInfo GetEditorInfo();

        public abstract string GetActiveSolutionPath();

        #endregion

        #region Event Handlers

        public void OnWindowOrDocumentActivated()
        {
            try
            {
                var solutionName = GetActiveSolutionPath();
                if (string.IsNullOrWhiteSpace(solutionName))
                    return;

                var document = GetActiveSolutionPath();
                if (document != null)
                    HandleActivity(document, false);
            }
            catch (Exception ex)
            {
                Logger.Error("HandleWindowOrDocumentActivated : " + ex.Message);
            }
        }

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
                lock (ThreadLock)
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

        protected static void PromptApiKey()
        {
            using (var form = new ApiKeyForm())
            {
                form.Show();
            }
        }

        protected static void SettingsPopup()
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

        #endregion
    }
}

