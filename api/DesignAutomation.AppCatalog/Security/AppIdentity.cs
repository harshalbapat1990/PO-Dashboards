using System.Security.Claims;
using System.Security.Principal;
using DesignAutomation.AppCatalog.Extensions;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Security;

/// <summary></summary>
public class AnonymousPrincipal : IPrincipal
{
    public IIdentity Identity => new AnonymousIdentity();

    public bool IsInRole( string role ) => false;
}

/// <summary>A completely anon Identity with no claims</summary>
public class AnonymousIdentity : IIdentity
{
    public string AuthenticationType => String.Empty;

    public bool IsAuthenticated => false;

    public string Name => "Anonymous";
}

/// <summary>A lowest level basic identity</summary>
public class BasicIdentity : IIdentity
{
    public string AuthenticationType => "Basic";

    public bool IsAuthenticated => true;

    public string Name => "Basic";
}

/// <summary>A User Identity that simply wraps a ClaimsIdentity receieved from the client</summary>
public class AppIdentity
{
    /// <summary>Create an anonymous identity</summary>
    public AppIdentity()
    {
        this.Identity = new ClaimsIdentity(new AnonymousIdentity(), [new Claim(ClaimTypes.Anonymous, "anonymous")]);
    }

    public AppIdentity(ClaimsIdentity id)
    {
        this.Identity = id;
    }

    /// <summary>The original Identity</summary>
    [JsonIgnore]
    protected ClaimsIdentity Identity { get; private set; }

    /// <summary></summary>
    [JsonProperty("valid")]
    public bool IsAuthenticated => this.Identity.IsAuthenticated;

    /// <summary></summary>
    [JsonProperty("anon")]
    public bool IsAnonymous => this.Identity.HasClaim(c => c.Type == ClaimTypes.Anonymous);

    /// <summary></summary>
    [JsonProperty("userID")]
    public string UserID => this.Identity.Claims.FirstOrDefault(c => c.Type == "userid")?.Value
        ?? this.Identity.Claims.FirstOrDefault(c => c.Type == "id")?.Value
        ?? String.Empty;

    /// <summary></summary>
    [JsonProperty("name")]
    public string Name => this.Identity.Name ?? String.Empty;

    /// <summary></summary>
    [JsonProperty("email")]
    public string? Email => this.Identity.Claims.Where(c => c.Type == ClaimTypes.Email).FirstOrDefault()?.Value ?? String.Empty;

    /// <summary></summary>
    [JsonProperty("role")]
    public UserRoleLevel Role => this.Identity.Claims.Where(c => c.Type == ClaimTypes.Role).FirstOrDefault()?.Value.GetNullableEnumValue<UserRoleLevel>(true) ?? UserRoleLevel.Guest;
}
