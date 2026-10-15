using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;

namespace DesignAutomation.AppCatalog.Data;

/// <summary>Minimal local identity persistence required by Product Health auth.</summary>
public interface IUsersRepository
{
    AppUser? GetByEmail(string email);
    Task<Guid> Insert(AppUser user);
    Task<bool> UpdateRole(Guid userId, UserRoleLevel role);
}
