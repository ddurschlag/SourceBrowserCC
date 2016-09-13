namespace SourceBrowser.SolutionRetriever
{
    public interface IRepositoryRetriever
    {
        string RepoName { get; set; }
        string UserName { get; set; }

        bool IsValidUrl();
        string ProvideParsedReadme();
        string RetrieveProject();
    }
}