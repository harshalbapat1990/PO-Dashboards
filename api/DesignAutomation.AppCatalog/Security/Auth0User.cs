using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Security;

/// <summary>An Auth0 User</summary>
public struct Auth0User : IOAuthUser
{
	/// <summary>The primary token</summary>
	[JsonProperty("sub")]
	public string ID { get; set; }

	/// <summary>IAM ID</summary>
	[JsonProperty("p1.userId")]
	public Guid AltID { get; set; }

	[JsonProperty("nickname")]
	public string NickName { get; set; }

	[JsonProperty("name")]
	public string Name => $"{this.FamilyName}, {this.GivenName}";

	[JsonProperty("given_name")]
	public string GivenName { get; set; }

	[JsonProperty("family_name")]
	public string FamilyName { get; set; }

	[JsonProperty("preferred_username")]
	public string Username { get; set; }

	[JsonProperty("email")]
	public string Email { get; set; }

	[JsonProperty("email_verified")]
	public bool Verified { get; set; }

	[JsonProperty("updated_at")]
	public long UpdatedAt { get; set; }

	[JsonProperty("picture")]
	public string Picture { get; set; }
}