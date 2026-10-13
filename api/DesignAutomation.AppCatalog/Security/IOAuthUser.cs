namespace DesignAutomation.AppCatalog.Security;

/// <summary></summary>
public interface IOAuthUser
{
	/// <summary>ID from the OAuth system</summary>
	public string ID { get; set; }

	/// <summary></summary>
	public string Name { get; }

	/// <summary></summary>
	public string Email { get; set; }

	/// <summary></summary>
	public string Picture { get; set; }
}
