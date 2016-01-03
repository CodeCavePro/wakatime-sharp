using System;

namespace WakaTime
{
    public interface IWakaTimePackage
    {
        void Initialize();

        void BindEditorEvents();

        ILogger GetLogger();

        EditorInfo GetEditorInfo();

        string GetActiveSolutionPath();

        void OnWindowOrDocumentActivated();

        void OnDocumentOpened(string documentName);

        void OnDocumentChanged(string documentName);

        void OnSolutionOpened(string solutionName);
    }
}
