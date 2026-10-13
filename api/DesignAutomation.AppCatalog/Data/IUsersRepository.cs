using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;

namespace DesignAutomation.AppCatalog.Data;

/// <summary></summary>
public interface IUsersRepository
{
    /// <summary></summary>
    string DBInfo();

    /// <summary></summary>
    AppUser? Get(Guid id);

    /// <summary></summary>
    AppUser? GetByEmail(string email);

    /// <summary></summary>
    Task<List<AppUser>> List();

    /// <summary></summary>
    Task<Guid> Insert(AppUser acct);

    /// <summary></summary>
    Task<bool> UpdateLogin(Guid userID);

    /// <summary>updates JUST the Ping ID only fow now. Could be other values later</summary>
    Task<bool> Update(Guid userID, Guid pingID);

    /// <summary></summary>
    Task<bool> UpdateRole(Guid userID, UserRoleLevel role);

    /// <summary></summary>
    Task<bool> Delete(Guid id);
}