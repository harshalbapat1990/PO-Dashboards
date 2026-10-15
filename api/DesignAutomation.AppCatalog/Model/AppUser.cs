using DesignAutomation.AppCatalog.Security;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>Minimal account record used by local Product Health authentication.</summary>
public sealed class AppUser
{
    public Guid ID { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Modified { get; set; } = DateTimeOffset.UtcNow;
    public UserRoleLevel Role { get; set; } = UserRoleLevel.User;
}
