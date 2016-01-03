namespace WakaTime
{
    public interface IWakaTimeIdePlugin
    {
        void BindEditorEvents();

        ILogService GetLogger();

        EditorInfo GetEditorInfo();

        string GetActiveSolutionPath();

        void OnDocumentOpened(string documentName);

        void OnDocumentChanged(string documentName);

        void OnSolutionOpened(string solutionName);
    }
}
