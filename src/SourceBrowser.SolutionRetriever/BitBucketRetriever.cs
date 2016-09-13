using System;
using System.IO;
using LibGit2Sharp;

namespace SourceBrowser.SolutionRetriever
{
    public class BitBucketRetriever : IRepositoryRetriever
    {
        private string _url;
        private Guid guid = Guid.NewGuid();
        string _absoluteRepositoryPath;
        public string UserName { get; set; }
        public string RepoName { get; set; }

        public BitBucketRetriever(string url)
        {
            if (!(url.StartsWith("https://") || url.StartsWith("http://")))
                url = "https://" + url;
            _url = url;
            var splitUrl = url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (splitUrl.Length < 3)
                return;

            UserName = splitUrl[splitUrl.Length - 3];
            RepoName = splitUrl[splitUrl.Length - 1];
        }

        public bool IsValidUrl()
        {
            if (String.IsNullOrWhiteSpace(UserName))
                return false;
            if (String.IsNullOrWhiteSpace(RepoName))
                return false;

            return true;
        }

        public string ProvideParsedReadme()
        {
            string readmeLocation = Path.Combine(_absoluteRepositoryPath, "README.md");
            if (!File.Exists(readmeLocation))
            {
                return String.Empty;
            }
            var md = new MarkdownDeep.Markdown();
            return md.Transform(File.ReadAllText(readmeLocation));
        }

        public string RetrieveProject()
        {
            string baseRepositoryPath = Path.Combine(System.Web.Hosting.HostingEnvironment.MapPath("~/"), "GithubStaging");

            _absoluteRepositoryPath = Path.Combine(baseRepositoryPath, UserName, RepoName);

            // libgit2 requires the target directory to be empty
            if (Directory.Exists(_absoluteRepositoryPath))
            {
                DeleteReadOnlyDirectory(_absoluteRepositoryPath);
            }
            Directory.CreateDirectory(_absoluteRepositoryPath);

            //TODO: Bitbucket API to get this
            var cloneUrl = _url
                .Replace("projects", "scm")
                .Replace("/repos", "")
                .TrimEnd('/')
                + ".git";

            Repository.Clone(cloneUrl, _absoluteRepositoryPath);
            return _absoluteRepositoryPath;
        }

        /// <summary>
        /// Recursively deletes a directory as well as any subdirectories and files. If the files are read-only, they are flagged as normal and then deleted.
        /// From http://stackoverflow.com/questions/25549589/programatically-delete-local-repository-with-libgit2sharp
        /// </summary>
        /// <param name="directory">The name of the directory to remove.</param>
        public static void DeleteReadOnlyDirectory(string directory)
        {
            foreach (var subdirectory in Directory.EnumerateDirectories(directory))
            {
                DeleteReadOnlyDirectory(subdirectory);
            }
            foreach (var fileName in Directory.EnumerateFiles(directory))
            {
                var fileInfo = new FileInfo(fileName);
                fileInfo.Attributes = FileAttributes.Normal;
                fileInfo.Delete();
            }
            Directory.Delete(directory);
        }
    }
}
