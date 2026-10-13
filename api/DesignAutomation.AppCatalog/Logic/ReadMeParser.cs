using System.Text.RegularExpressions;
using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Model;

namespace DesignAutomation.AppCatalog.Logic;

public class ReadMeParser
{
    /// <summary>Tokens that start a list</summary>
    static readonly char[] ListTokens = ['-', '*', '+'];

    static readonly char[] Whitespace = ['\t','\n',' '];

    const string RequiredKey = "metadata";
    const string ProdBranchKey = "production branch";
    const string IsProdKey = "is production";
    const string ProjMgmtKey = "project management";
    const string WebsiteKey = "website";
    const string DescriptionKey = "description";
    const string DocumentsKey = "documents";
    const string TeamKey = "team";
    const string TechStackKey = "tech stack";
    const string ChildReposKey = "other repos";
    const string UptimeKey = "availability";
    const string HasAPIKey = "tier 1 integration";

    /// <summary>Constructor</summary>
    /// <param name="body">Takes the README text to parse</param>
    public ReadMeParser(string body)
    {
        this.Body = body;
    }

    #region [ Properties ]

    /// <summary></summary>
    private static readonly Regex NewBlock = new Regex(@"^(#{1,6})\s+([-\w&— ]*)$", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    private static readonly Regex JustLink = new Regex(@"^https?:\/\/[^ ]+$", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    /// <summary></summary>
    private static readonly Regex MDLink = new Regex(@"^\[(.*)\]\((https?:\/\/[^ ]*)\)(.*)$", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    /// <summary></summary>
    private static readonly Regex Email = new Regex(@"^\|\s*([\w\-\.]+@[\w-]+\.+com)\s*\|\s*([\w-\(\) ]+)\s*\|$", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    /// <summary></summary>
    private static readonly Regex Comment = new Regex(@"<!--([\s\S]*?)-->", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    /// <summary>The original input body after it is sanatized of comments</summary>
    public string Body {get; private set; }

    /// <summary></summary>
    public ReadmeData Data {get; private set; } = new();

    /// <summary></summary>
    private Dictionary<string, ReadmeBlock> Sections = [];

    /// <summary></summary>
    private ReadmeBlock? CurrentBlock = null;

    /// <summary></summary>
    protected char[] AllTokens = Whitespace.Concat(ListTokens).ToArray();

    #endregion [ Properties ]

    public ReadmeData Process()
    {
        this.ParseSections();
        this.Data.Name = this.Sections.First().Value.Header.Trim();
        this.ParseDescription();
        this.ParseRequiredInfo();
        this.ParseAliases();
        this.ParseDocuments();
        this.ParseTechStack();
        this.ParseTeam();
        this.ParseChildRepo();
        return this.Data;
    }

    /// <summary>Split all the README sections into a list</summary>
    /// <param name="body"></param>
    protected void ParseSections()
    {
        // split on new lines
        var lines = this.Body.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        IEnumerator<string> enu = lines.GetEnumerator();

        // iterate each line
        while ( enu.MoveNext() )
        {
            // ignore comments until the end of comments
            if ( enu.Current.StartsWith("<!--") )
            {
                while (!enu.Current.EndsWith("-->") )
                    enu.MoveNext();

                continue;
            }

            // ignore code blocks until the end
            if ( enu.Current.StartsWith("```") )
            {
                while (!enu.Current.StartsWith("```") )
                    enu.MoveNext();

                continue;
            }

            // ignore HRs
            if ( enu.Current.StartsWith("---") || enu.Current.StartsWith("___") || enu.Current.StartsWith("***") )
                continue;

            // ignore quotes
            if ( enu.Current.StartsWith(">") )
                continue;

            // test for section start
            if ( !this.IsSectionStart(enu) )
            {
                this.CurrentBlock?.Text.Add(enu.Current.Trim());
            }
        }
    }

    /// <summary>Is this the start of a new section</summary>
    protected bool IsSectionStart(IEnumerator<string> enu)
    {
        var match = NewBlock.Match(enu.Current);

        if ( match.Success )
        {
            // finish the previous block
            if ( this.CurrentBlock != null )
                this.Sections.Add(this.CurrentBlock.Header.ToLower(), this.CurrentBlock);

            // start a new block
            this.CurrentBlock =  new() { Level = match.Groups[1].Value.Length, Header = match.Groups[2].Value.Trim() };
            return true;
        }

        return false;
    }

    /// <summary>Parse just the description</summary>
    protected void ParseDescription()
    {
        this.Data.Description = String.Join(" ", this.GetSection(DescriptionKey).Text);
    }

    /// <summary>Parse just the aliases</summary>
    protected void ParseAliases()
    {
        this.Data.Aliases = this.ParseList(this.Sections.First().Value.Text);
    }

    /// <summary>Parse just the documents</summary>
    protected void ParseDocuments()
    {
        var text = this.ParseList(this.GetSection(DocumentsKey).Text);

        if (text == null || text.Count < 1)
            return;

        this.Data.Documents = [];

        foreach (string line in text)
        {
            var match = MDLink.Match(line);
            if (!match.Success)
                continue;
            this.Data.Documents.Add( new() { Label = match.Groups[1].Value.Trim(['[',']']), URL = match.Groups[2].Value.Trim(['(',')']) } );
        }
    }

    /// <summary>Parse metadata</summary>
    protected void ParseRequiredInfo()
    {
        var text = this.GetSection(RequiredKey).Text;

        var list = this.ParseList(text);

        var dict = new Dictionary<string,string>();

        foreach (string line in list)
        {
            var start = line.IndexOf(':');
            var key = line.Substring(0, start).Trim(AllTokens);
            var value = line.Substring(start+1).Trim(AllTokens);
            dict.Add(key.ToLower(),value);
        }

        // this is a bad way to do this for now but just to get it going
        this.Data.IsProduction = dict.ContainsKey(IsProdKey) ? dict[IsProdKey].GetBooleanValue() : false;
        this.Data.WebSite = dict.ContainsKey(WebsiteKey) ? this.ParseLink(dict[WebsiteKey]) : new();
        this.Data.ProjectManagement =  dict.ContainsKey(ProjMgmtKey) ? this.ParseLink(dict[ProjMgmtKey]) : new();
        this.Data.ProductionBranch = dict.ContainsKey(ProdBranchKey) ? dict[ProdBranchKey] : String.Empty;
        this.Data.Uptime = dict.ContainsKey(UptimeKey) ? dict[UptimeKey].GetPercentValue() : 0;

        if (dict.ContainsKey(HasAPIKey))
        {
            var values = dict[HasAPIKey].Split(['|', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            values.ForEach( v => {
                if ( Enum.TryParse<Tier1AppType>(v, true, out Tier1AppType res ) && res != Tier1AppType.None )
                    this.Data.Integrations |= res;
            });
        }
    }

    /// <summary>Parse the tech stack</summary>
    protected void ParseTechStack()
    {
        this.Data.TechStack = this.ParseList(this.GetSection(TechStackKey).Text);
    }

    /// <summary>Parse child repos</summary>
    protected void ParseChildRepo()
    {
        this.Data.ChildRepos = [];
        var list = this.ParseList(this.GetSection(ChildReposKey).Text);
        foreach(string link in list)
        {
            if ( JustLink.IsMatch(link) )
                this.Data.ChildRepos.Add( new() { Label = link.GetLastAfter('/'), URL = link } );
        }
    }

    /// <summary>Parses a simple link or a Markdown Link with Label</summary>
    protected ReadmeLink ParseLink(string text)
    {
        if ( String.IsNullOrWhiteSpace(text))
            return new();

        text = text.Trim();

        if ( JustLink.IsMatch(text) )
            return new() { Label = text, URL = text };

        var match = MDLink.Match(text);
        return new() { Label = match.Groups[1].Value.Trim(['[',']']), URL = match.Groups[2].Value.Trim(['(',')']) };
    }

    /// <summary>Parses out a readme list</summary>
    protected List<string> ParseList(List<string> text)
    {
        if (text.Count < 1)
            return [];

        return text.Where( i => ListTokens.Contains( i.TrimStart().First() )).Select(l => l.Trim(AllTokens)).ToList();
    }

    /// <summary>Parses out a team from a table</summary>
    protected void ParseTeam()
    {
        var text = this.GetSection(TeamKey).Text;

        if (text == null || text.Count < 1)
            return;

        this.Data.Team = [];

        foreach (string line in text)
        {
            var match = Email.Match(line);
            if (!match.Success || match.Groups.Count < 2)
                continue;
            this.Data.Team.Add(new() { Name = match.Groups[1].Value.Trim(), Role = match.Groups[2].Value.Trim() } );
        }
    }

    /// <summary>Get the first line of a section by title</summary>
    protected string GetSectionLine(string section)
    {
        var lines = this.GetSection(section).Text;
        return lines.Count > 0 ? lines[0].Trim() : String.Empty;
    }
    
    /// <summary>Find a section by title</summary>
    protected ReadmeBlock GetSection(string section)
    {
        if ( String.IsNullOrWhiteSpace(section) || this.Sections.Count < 1)
            return new();

        var sec = section.Trim().ToLower();

        return this.Sections.ContainsKey(sec) ? this.Sections[sec] : new();
    }


}