using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>Stage 0 setup payload that creates the first organization/portfolio/product hierarchy.</summary>
public sealed record FoundationProductRequest
{
    [JsonProperty("organizationName")] public string OrganizationName { get; init; } = string.Empty;
    [JsonProperty("portfolioName")] public string PortfolioName { get; init; } = string.Empty;
    [JsonProperty("productName")] public string ProductName { get; init; } = string.Empty;
    [JsonProperty("description")] public string Description { get; init; } = string.Empty;
    [JsonProperty("productType")] public string ProductType { get; init; } = string.Empty;
    [JsonProperty("productOwner")] public string ProductOwner { get; init; } = string.Empty;
    [JsonProperty("productManager")] public string ProductManager { get; init; } = string.Empty;
    [JsonProperty("globalProductLead")] public string GlobalProductLead { get; init; } = string.Empty;
    [JsonProperty("engineeringManager")] public string EngineeringManager { get; init; } = string.Empty;
    [JsonProperty("scrumMaster")] public string ScrumMaster { get; init; } = string.Empty;
    [JsonProperty("securityLead")] public string SecurityLead { get; init; } = string.Empty;
    [JsonProperty("status")] public string Status { get; init; } = "Active";
    [JsonProperty("startDate")] public DateOnly? StartDate { get; init; }
    [JsonProperty("targetDate")] public DateOnly? TargetDate { get; init; }
    [JsonProperty("teams")] public List<FoundationTeamRequest> Teams { get; init; } = [];
    [JsonProperty("githubRepositories")] public List<string> GitHubRepositories { get; init; } = [];
    [JsonProperty("azureProjects")] public List<FoundationAzureProjectRequest> AzureProjects { get; init; } = [];
    [JsonProperty("syncFrequencyMinutes")] public int SyncFrequencyMinutes { get; init; } = 60;
    [JsonProperty("githubOrganization")] public string GitHubOrganization { get; init; } = string.Empty;
    [JsonProperty("azureOrganization")] public string AzureOrganization { get; init; } = string.Empty;
    [JsonProperty("githubSecretReference")] public string GitHubSecretReference { get; init; } = string.Empty;
    [JsonProperty("azureSecretReference")] public string AzureSecretReference { get; init; } = string.Empty;
}

public sealed record FoundationTeamRequest
{
    [JsonProperty("name")] public string Name { get; init; } = string.Empty;
    [JsonProperty("members")] public List<string> Members { get; init; } = [];
}

public sealed record FoundationAzureProjectRequest
{
    [JsonProperty("id")] public string Id { get; init; } = string.Empty;
    [JsonProperty("name")] public string Name { get; init; } = string.Empty;
}

public sealed record FoundationProductSummary
{
    [JsonProperty("organizationId")] public Guid OrganizationId { get; init; }
    [JsonProperty("organizationName")] public string OrganizationName { get; init; } = string.Empty;
    [JsonProperty("portfolioId")] public Guid PortfolioId { get; init; }
    [JsonProperty("portfolioName")] public string PortfolioName { get; init; } = string.Empty;
    [JsonProperty("productId")] public Guid ProductId { get; init; }
    [JsonProperty("productName")] public string ProductName { get; init; } = string.Empty;
    [JsonProperty("description")] public string Description { get; init; } = string.Empty;
    [JsonProperty("productType")] public string ProductType { get; init; } = string.Empty;
    [JsonProperty("productOwner")] public string ProductOwner { get; init; } = string.Empty;
    [JsonProperty("productManager")] public string ProductManager { get; init; } = string.Empty;
    [JsonProperty("globalProductLead")] public string GlobalProductLead { get; init; } = string.Empty;
    [JsonProperty("engineeringManager")] public string EngineeringManager { get; init; } = string.Empty;
    [JsonProperty("scrumMaster")] public string ScrumMaster { get; init; } = string.Empty;
    [JsonProperty("securityLead")] public string SecurityLead { get; init; } = string.Empty;
    [JsonProperty("status")] public string Status { get; init; } = "Active";
    [JsonProperty("startDate")] public DateOnly? StartDate { get; init; }
    [JsonProperty("targetDate")] public DateOnly? TargetDate { get; init; }
    [JsonProperty("githubRepositories")] public List<string> GitHubRepositories { get; init; } = [];
    [JsonProperty("azureProjects")] public List<FoundationAzureProjectRequest> AzureProjects { get; init; } = [];
    [JsonProperty("azureOrganization")] public string AzureOrganization { get; init; } = string.Empty;
    [JsonProperty("teams")] public List<FoundationTeamRequest> Teams { get; init; } = [];
}

public sealed record FoundationUserRequest([property: JsonProperty("email")] string Email,
    [property: JsonProperty("role")] string Role = "User");
public sealed record FoundationAccessRequest([property: JsonProperty("userId")] string UserId,
    [property: JsonProperty("role")] string Role = "Viewer");
public sealed record FoundationUserSummary(
    [property: JsonProperty("id")] Guid Id,
    [property: JsonProperty("email")] string Email,
    [property: JsonProperty("role")] string Role,
    [property: JsonProperty("created")] DateTimeOffset Created,
    [property: JsonProperty("modified")] DateTimeOffset Modified);
