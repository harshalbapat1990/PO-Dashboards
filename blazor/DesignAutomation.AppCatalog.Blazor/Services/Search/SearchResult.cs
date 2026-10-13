namespace DesignAutomation.AppCatalog.Blazor.Services.Search;

public enum SearchResultType
{
    Application,
    Repository,
    PoC,
    User
}

public record SearchResult(
    SearchResultType Type,
    string Title,
    string? Subtitle,
    string Url);
