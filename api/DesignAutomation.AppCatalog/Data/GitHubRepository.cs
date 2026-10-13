using Microsoft.Extensions.Logging;
using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Logic;
using Octokit;
using DesignAutomation.AppCatalog.Config;

namespace DesignAutomation.AppCatalog.Data;

/// <summary></summary>
public interface IGitHubRepository
{
    Task<List<ReadmeDTO>> ListReposByTopic(string topic);

    Task<List<ReadmeData>> ParseReposByTopic(string topic);

    Task<ReadmeData> ParseRepo(long id);
}

/// <summary></summary>
public class GitHubRepository : IGitHubRepository
{
    public GitHubRepository(GithubAPIConfiguration config, ILogger<GitHubRepository> logger)
    {
        if ( !config.IsValid() )
            throw new ArgumentNullException();

        this.Logger = logger;
        this.Client = new GitHubClient(new ProductHeaderValue(config.ClientName)) { Credentials = new Credentials(config.GitHubToken)};
    }

    /// <summary></summary>
    public GitHubClient Client {get; init;}

    /// <summary>Logger to use</summary>
    public ILogger<GitHubRepository> Logger {get; set;}

    /// <summary></summary>
    /// <param name="topic"></param>
    /// <returns></returns>
    public async Task<List<ReadmeDTO>> ListReposByTopic(string topic)
    {
        List<ReadmeDTO> results = new();

        if (String.IsNullOrWhiteSpace(topic))
            return [];

        var search = new SearchRepositoriesRequest("org:People-Places-Solutions") { Topic = topic };
        var repos = await this.Client.Search.SearchRepo(search);

        foreach ( var repo in repos.Items )
        {
            results.Add( new ReadmeDTO {
                ID = repo.Id,
                RepoName = repo.Name,
                URL = repo.HtmlUrl,
                Timestamp = repo.UpdatedAt
            });
        }

        return results;
    }

    /// <summary></summary>
    /// <param name="topic"></param>
    /// <returns></returns>
    public async Task<List<ReadmeData>> ParseReposByTopic(string topic)
    {
        // dict of the repo name with the readme data
        var results = new Dictionary<string,ReadmeData>();

        if (String.IsNullOrWhiteSpace(topic))
            return [];

        var search = new SearchRepositoriesRequest("org:People-Places-Solutions") { Topic = topic };
        var repos = await this.Client.Search.SearchRepo(search);

        // iterate the search results
        //if (repo.Id != 827465157)
        foreach ( var repo in repos.Items )
        {
            var readme = await this.ParseRepo(repo);
            results.Add(repo.Name, readme);
        }

        // select out child repos for removal
        List<string> toRemove = [];
        results.Values.ToList().ForEach( r => toRemove.AddRange(r.ChildRepos.Select(c => c.Label) ) );
        toRemove.Distinct().ToList().ForEach( key => { if (results.ContainsKey(key)) results.Remove(key); } );

        // return just the readmes
        return results.Select(r => r.Value).OrderBy(i => i.Name).ToList();
    }

    /// <summary>Need to move this to a data manager</summary>
    public async Task<ReadmeData> ParseRepo(long id)
    {
        if (id < 1)
            return new();

        var repo = await this.Client.Repository.Get(id);
        var readme = await this.ParseRepo(repo);

        // get commits in the past week
        var proBranches = (await this.Client.Repository.Branch.GetAll(repo.Id)).Where( b => b.Protected );

        // foreach protected branch get its commit info
        foreach (var branch in proBranches)
        {
            var commit = await this.Client.Repository.Commit.Get( repo.Id, branch.Commit.Sha );
            readme.Branches.Add(new() { Name = branch.Name, Timestamp = commit.Commit.Committer.Date } );
        }

        return readme;
    }

    /// <summary>Does the actual repo readme parsing</summary>
    /// <param name="client"></param>
    /// <param name="repo"></param>
    /// <returns></returns>
    private async Task<ReadmeData> ParseRepo(Repository repo)
    {
        var readme = await this.Client.Repository.Content.GetReadme(repo.Id);

        try
        {
            var parser = new ReadMeParser(readme.Content);
            var data = parser.Process();
            data.ID = repo.Id;
            data.RepoName = repo.Name;
            data.URL = repo.HtmlUrl;
            data.Timestamp = repo.UpdatedAt;
            //data.IsPrivate = repo.Private;
            data.ParseError = false;
            data.ContentLength = readme.Content.Length;
            return data;
        }
        catch(System.Exception exp)
        {
            this.Logger.LogError(exp, "Failed to parse README for repo {name}", repo.FullName);
            return new ReadmeData { 
                ID = repo.Id,
                Name = repo.Name,
                RepoName = repo.Name,
                URL = repo.HtmlUrl,
                ParseError = true,
                ContentLength = readme.Content.Length
            };
        }
    }

}

// var search = new SearchRepositoriesRequest("org:People-Places-Solutions")
// {
//     In = new[] { InQualifier.Readme, InQualifier.Description },
// };
// search.Topic = this.Config.Topic;
// search.CustomProperties = new Dictionary<string, string>() { {"ou", "gte" } };
// DateTimeOffset since = DateTimeOffset.UtcNow - new TimeSpan(7, 0, 0, 0);
// var commits = await client.Repository.Commit.GetAll(repo.Id, new CommitRequest() { Since = since } );