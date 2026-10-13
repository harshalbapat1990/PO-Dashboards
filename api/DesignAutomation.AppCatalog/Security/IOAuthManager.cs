namespace DesignAutomation.AppCatalog.Security;

/// <summary>Interface for any Authorization Manager</summary>
public interface IOAuthManager
{
    string Login(string redirect, OAuthState state);

    string Logout(string redirect);

    Task<Auth0Token?> GetToken(string code, string redirect);

    Task<IOAuthUser> GetUserInfo(string token);

    Task<KeyChain> GetKeys();
}
