using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;

namespace DesignAutomation.AppCatalog.Data;

public interface IFoundationAccessRepository
{
    Task<List<FoundationUserSummary>> ListUsers();
    Task<Guid> CreateUser(string email, UserRoleLevel role);
    Task<bool> SetUserRole(Guid id, UserRoleLevel role);
    Task<bool> SetPortfolioAccess(Guid portfolioId, Guid userId, UserRoleLevel role);
    Task<bool> SetProductAccess(Guid productId, Guid userId, UserRoleLevel role);
    Task<bool> CanReadProduct(Guid productId, Guid userId);
    Task<bool> CanManageProduct(Guid productId, Guid userId);
}
