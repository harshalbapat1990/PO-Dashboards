using System.Security.Claims;
using System.Text;
using DesignAutomation.AppCatalog.Data;
using DesignAutomation.AppCatalog.Extensions;

namespace DesignAutomation.AppCatalog.Security;

/// <summary>The Security Manager Interface which defines security methods on each API method.</summary>
public interface ISecurityManager
{
    AppIdentity Authenticate(string token, string id);
}

/// <summary></summary>
public class AnonynousSecurityManager : ISecurityManager
{
    /// <summary></summary>
    public AppIdentity Authenticate(string token, string curClient)
    {
        return new AppIdentity();
    }
}

public class BasicSecurityManager : ISecurityManager
{
    public BasicSecurityManager( IUsersRepository repo, byte[] key )
    {
        if ( key == null || key.Length != 32 )
            throw new ArgumentException( "The key must be exactly 32 bytes for now." );

        this.UsersRepo = repo;
        this.Secret = key;
    }

    #region [ Properties ]

    /// <summary>Verifies the requesting client has the same signature as the client used to log in</summary>
    public bool DisableClientCheck { get; set; } = true;

    /// <summary></summary>
    protected IUsersRepository UsersRepo { get; set; }

    /// <summary>32 byte key used in signing the token</summary>
    private byte[] Secret { get; set; } = [];

    #endregion [ Properties ]

    /// <summary>Generates an Identity based on the Bearer token</summary>
    /// /// <param name="curClient">The current client request signature to use in the claims check</param>
    public AppIdentity Authenticate(string token, string curClient)
    {
        // no token at all returns anon user
        if ( String.IsNullOrWhiteSpace( token ) )
        	return new AppIdentity();

        // validate the token is signed properly
        var auth = new JWTManager(this.Secret);
        ClaimsPrincipal claims = auth.ValidateToken( token );

		// valdate the client claim and current client fingerprint are the same
        if ( !this.DisableClientCheck )
        {
            string sigClaim = claims.Claims.FirstOrDefault(c => c.Type == "cid")?.Value ?? String.Empty;
            if ( String.IsNullOrWhiteSpace(sigClaim) || !curClient.Equals(sigClaim, StringComparison.OrdinalIgnoreCase) )
                return new AppIdentity();  
        }

        // test thee timestamp

		// update the token in the database

        return ( claims?.Identity != null && claims.Identity.IsAuthenticated )
            ? new AppIdentity( claims.Identities.First() ) : new AppIdentity();
    }
}


// public interface ISecurityManager
// {
//     /// <summary>Holds the user Identity Info</summary>
//     AppIdentity User { get; }

//     /// <summary>Validate the user from a token and set it to the User property</summary>
//     AppIdentity ValidateUser(string token);

//     /// <summary>Determine if the user can access this method</summary>
//     bool IsAuthorized(UserRoleLevel minLevel = UserRoleLevel.Guest);
// }

// /// <summary>An anon Security Manager can be used in dev to avoid dealing with any logins</summary>
// public class AnonynousSecurityManager : ISecurityManager
// {
//     /// <summary>Creates an anonymous user</summary>
//     public AppIdentity User { get => new AppIdentity(); protected set { } }

//     /// <summary>Always returns true</summary>
//     public bool IsAuthorized(UserRoleLevel minLevel = UserRoleLevel.Guest) => true;

//     /// <summary></summary>
//     public AppIdentity ValidateUser(string token) => this.User;
// }

// /// <summary>The standard JWT Security Manager to use for real security</summary>
// public class JWTSecurityManager : ISecurityManager
// {
//     protected JWTManager JWT { get; set; }

//     public JWTSecurityManager(JWTManager jwt)
//     {
//         this.JWT = jwt;
//     }

//     public AppIdentity User { get; protected set; } = new AppIdentity();

//     /// <summary></summary>
//     /// <param name="token"></param>
//     /// <returns></returns>
//     public AppIdentity ValidateUser(string token)
//     {
//         this.User = this.JWT.ValidateToken(token);
//         return this.User;
//     }

//     /// <summary></summary>
//     /// <returns></returns>
//     public bool IsAuthorized(UserRoleLevel minLevel = UserRoleLevel.Guest)
//     {
//         if (this.User.IsAnonymous || !this.User.IsAuthenticated)
//             return false;

//         if (String.IsNullOrWhiteSpace(this.User.Email) || this.User.Role < minLevel)
//             return false;

//         return this.User.Email.IsValidJacobsEmail();
//     }
// }

// /// <summary>Uses a simple password to pass auth</summary>
// public class BasicSecurityManager : ISecurityManager
// {
//     protected string Passcode { get; set; } = String.Empty;

//     public BasicSecurityManager(string passcode)
//     {
//         if (String.IsNullOrWhiteSpace(passcode))
//             throw new ArgumentException("Passcode cannot be empty.", nameof(passcode));

//         this.Passcode = passcode;
//     }
    
//     public AppIdentity User { get; protected set; } = new AppIdentity();

//     /// <summary></summary>
//     /// <param name="token">The basic auth header base64 encoded as id:password</param>
//     /// <returns></returns>
//     public AppIdentity ValidateUser(string token)
//     {
//         if (String.IsNullOrWhiteSpace(token) )
//             return this.User;

//         var parts = token.BASE64Decode().Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );

//         if ( parts.Length < 2 || !this.Passcode.Equals(parts[1], StringComparison.InvariantCulture) )
//             return this.User;
        
//         this.User = new AppIdentity( new ClaimsIdentity( new BasicIdentity(), [
//             new Claim( ClaimTypes.Role, "User"),
//             new Claim( ClaimTypes.Email, parts[0] ),
//             new Claim( "jti", Guid.NewGuid().ToString(), ClaimValueTypes.String ),
//             ])
//         );
//         return this.User;
//     }

//     /// <summary></summary>
//     /// <returns></returns>
//     public bool IsAuthorized(UserRoleLevel minLevel = UserRoleLevel.Guest)
//     {
//         if (this.User.IsAnonymous || !this.User.IsAuthenticated)
//             return false;

//         if (String.IsNullOrWhiteSpace(this.User.Email) || this.User.Role < minLevel)
//             return false;

//         return this.User.Email.IsValidJacobsEmail();
//     }
// }
