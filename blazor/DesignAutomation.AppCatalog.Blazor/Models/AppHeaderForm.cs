using System.ComponentModel.DataAnnotations;
using DesignAutomation.AppCatalog.Blazor.Models.Api;

namespace DesignAutomation.AppCatalog.Blazor.Models;

// Mutable form model for create/edit; maps to/from the immutable AppHeader record.
public class AppHeaderForm
{
    public Guid ID { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string AppType { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public bool Production { get; set; }

    public string ProductionBranch { get; set; } = string.Empty;

    public string WebSite { get; set; } = string.Empty;

    public string ReviewURL { get; set; } = string.Empty;

    public AvailabilityLevel Availability { get; set; } = AvailabilityLevel.Undecided;

    // newline-separated in the form; split into lists on save
    public string TeamText { get; set; } = string.Empty;

    public string ReposText { get; set; } = string.Empty;

    public static AppHeaderForm FromAppHeader(AppHeader a) => new()
    {
        ID = a.ID,
        FullName = a.FullName,
        AppType = a.AppType,
        Description = a.Description,
        Production = a.Production,
        ProductionBranch = a.ProductionBranch,
        WebSite = a.WebSite,
        ReviewURL = a.ReviewURL,
        Availability = a.Availability,
        TeamText = string.Join(Environment.NewLine, a.Team),
        ReposText = string.Join(Environment.NewLine, a.Repos)
    };

    public AppHeader ToAppHeader() => new()
    {
        ID = ID,
        FullName = FullName,
        AppType = AppType,
        Description = Description,
        Production = Production,
        ProductionBranch = ProductionBranch,
        WebSite = WebSite,
        ReviewURL = ReviewURL,
        Availability = Availability,
        Team = SplitLines(TeamText),
        Repos = SplitLines(ReposText)
    };

    private static List<string> SplitLines(string text) => text
        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
}
