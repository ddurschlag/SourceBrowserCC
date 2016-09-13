namespace SourceBrowser.Site.Controllers
{
    using System.IO;
    using System.Web.Mvc;
    using SourceBrowser.SolutionRetriever;
    using SourceBrowser.Generator.Transformers;
    using SourceBrowser.Site.Repositories;
    using System;
    using System.Linq;
    using SourceBrowser.Tracing;

    public class UploadController : Controller
    {
        // GET: Upload
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Submit(string repositoryUrl)
        {
            using (ViewBag.ResultMessages = new Collector())
            {

                // If someone navigates to submit directly, just send 'em back to index
                if (string.IsNullOrWhiteSpace(repositoryUrl))
                {
                    return View("Index");
                }

                IRepositoryRetriever retriever = new GitHubRetriever(repositoryUrl);
                if (!retriever.IsValidUrl())
                {
                    retriever = new BitBucketRetriever(repositoryUrl);
                    if (!retriever.IsValidUrl())
                    {
                        ViewBag.Error = "Make sure that the provided path points to a valid GitHub or BitBucket repository.";
                        return View("Index");
                    }
                }

                // Check if this repo already exists
                if (!BrowserRepository.TryLockRepository(retriever.UserName, retriever.RepoName))
                {
                    // Repo exists. Redirect the user to that repository.
                    return Redirect("/Browse/" + retriever.UserName + "/" + retriever.RepoName);
                }
                // We have locked the repository and marked it as processing.
                // Whenever we return or exit on an exception, we need to unlock this repository
                bool processingSuccessful = false;
                try
                {
                    string repoRootPath = string.Empty;
                    try
                    {
                        repoRootPath = retriever.RetrieveProject();
                    }
                    catch (Exception ex)
                    {
                        Collector.Register(new Message { Type = Message.Level.Error, Text = "There was an error downloading this repository: " + ex.Message });
                        return View("Index");
                    }

                    // Generate the source browser files for this solution
                    var solutionPaths = GetSolutionPaths(repoRootPath);
                    if (solutionPaths.Length == 0)
                    {
                        ViewBag.Error = "No C# solution was found. Ensure that a valid .sln file exists within your repository.";
                        return View("Index");
                    }

                    var organizationPath = System.Web.Hosting.HostingEnvironment.MapPath("~/") + "SB_Files\\" + retriever.UserName;
                    var repoPath = Path.Combine(organizationPath, retriever.RepoName);

                    // TODO: Use parallel for.
                    // TODO: Process all solutions.
                    // For now, we're assuming the shallowest and shortest .sln file is the one we're interested in
                    foreach (var solutionPath in solutionPaths.OrderBy(n => n.Length).Take(1))
                    {
                        try
                        {
                            SourceBrowser.Generator.Model.WorkspaceModel workspaceModel;

                            using (new TimingMessage("ProcessSolution"))
                                workspaceModel = UploadRepository.ProcessSolution(solutionPath, repoRootPath);

                            //One pass to lookup all declarations
                            var typeTransformer = new TokenLookupTransformer();
                            using (new TimingMessage("TokenLookupVisit"))
                                typeTransformer.Visit(workspaceModel);
                            var tokenLookup = typeTransformer.TokenLookup;

                            //Another pass to generate HTMLs
                            var htmlTransformer = new HtmlTransformer(tokenLookup, repoPath);
                            using (new TimingMessage("HtmlVisit"))
                                htmlTransformer.Visit(workspaceModel);

                            var searchTransformer = new SearchIndexTransformer(retriever.UserName, retriever.RepoName);
                            using (new TimingMessage("SearchVisit"))
                                searchTransformer.Visit(workspaceModel);

                            // Generate HTML of the tree view
                            var treeViewTransformer = new TreeViewTransformer(repoPath, retriever.UserName, retriever.RepoName);
                            using (new TimingMessage("TreeViewVisit"))
                                treeViewTransformer.Visit(workspaceModel);
                        }
                        catch (Exception ex)
                        {
                            // TODO: Log this
                            Collector.Register(new Message { Type = Message.Level.Error, Text = ex.Message });
                            ViewBag.Error = "There was an error processing solution " + Path.GetFileName(solutionPath);
                            return View("Index");
                        }
                    }

                    try
                    {
                        UploadRepository.SaveReadme(repoPath, retriever.ProvideParsedReadme());
                    }
                    catch (Exception ex)
                    {
                        Collector.Register(new Message { Type = Message.Level.Warning, Text = "Could not load readme: " + ex.Message });
                    }

                    processingSuccessful = true;

                    ViewBag.RepoUrl = "/Browse/" + retriever.UserName + "/" + retriever.RepoName;
                    return View("Index");
                }
                finally
                {
                    if (processingSuccessful)
                    {
                        BrowserRepository.UnlockRepository(retriever.UserName, retriever.RepoName);
                    }
                    else
                    {
                        BrowserRepository.RemoveRepository(retriever.UserName, retriever.RepoName);
                    }
                }
            }
        }

        /// <summary>
        /// Simply searches for the solution files and returns their paths.
        /// </summary>
        /// <param name="rootDirectory">
        /// The root Directory.
        /// </param>
        /// <returns>
        /// The solution paths.
        /// </returns>
        private string[] GetSolutionPaths(string rootDirectory)
        {
            return Directory.GetFiles(rootDirectory, "*.sln", SearchOption.AllDirectories);
        }
    }
}