using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace DesignAutomation.AppCatalog.Security;

/// <summary>Handles creating and validating a key using HMACSHA512</summary>
public class JWTManager
{
    /// <summary>Token Issuer</summary>
    public const string Issuer = "http://jacobs.com";

    /// <summary>Token Audience</summary>
    public const string Audience = "http://jacobs.com";

    #region [ Constructors ]

    /// <summary>Construct with the actual bytes</summary>
    public JWTManager(byte[] key)
    {
        if (key == null || key.Length != 32)
            throw new ArgumentException("The key must be exactly 32 bytes for now.");

        this.Key = new SymmetricSecurityKey(new HMACSHA256(key).Key);
    }

    /// <summary>Construct with the key in base64 format</summary>
    public JWTManager(string base64key)
    {
        if (String.IsNullOrWhiteSpace(base64key))
            throw new ArgumentException("No key.");

        byte[] key = Convert.FromBase64String(base64key);

        if (key == null || key.Length != 32)
            throw new ArgumentException("The key must be exactly 32 bytes for now.");

        this.Key = new SymmetricSecurityKey(new HMACSHA256(key).Key);
    }

    #endregion [ Constructors ]

    /// <summary>32 byte key</summary>
    protected SecurityKey Key { get; private set; }

    /// <summary>Validates a token</summary>
    /// <param name="jwt"></param>
    /// <returns>A claims principal which is empty if not valid</returns>
    public ClaimsPrincipal ValidateToken(string jwt)
    {
        if (String.IsNullOrWhiteSpace(jwt))
            return new ClaimsPrincipal();

        // set the validation parameters
        TokenValidationParameters validationParameters = new TokenValidationParameters
        {
            ValidateLifetime = true, // optional
            ValidateAudience = true, // optional
            ValidateIssuer = true, // optional
            ValidIssuer = Issuer,
            ValidAudiences = new string[] { Audience },
            IssuerSigningKey = this.Key
            //IssuerSigningKeys = new List<SecurityKey> { key }
        };

        SecurityToken validatedToken;
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

        try
        {
            ClaimsPrincipal claims = handler.ValidateToken(jwt, validationParameters, out validatedToken);
            return claims;
        }
        catch
        {
            return new ClaimsPrincipal(new AnonymousPrincipal());
        }
    }

    /// <summary>Create a new token</summary>
	/// <param name="id">The user ID</param>
	/// <param name="name">the user name</param>
	/// <param name="email">user email</param>
	/// <param name="picture">url to users image</param>
	/// <param name="clientKey">A thumbprint of the client to use to verify subsequent requests</param>
	/// <param name="expiresIn">How many days from now to expire the token</param>
	/// <param name="role">the users role</param>
	/// <returns></returns>
    public TokenResponse GenerateToken(Guid id, string name, string email, string? clientKey, int expiresIn = 14, UserRoleLevel role = UserRoleLevel.Guest)
    {
        if ( id == Guid.Empty || String.IsNullOrWhiteSpace(email))
            return new TokenResponse();

        expiresIn = Math.Clamp(expiresIn, 0, 365);
        name = name.Trim();

        DateTimeOffset expires = DateTimeOffset.UtcNow.AddDays(expiresIn);

        // define the token
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // create a token that last 14 days
            Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim( "id", $"{id}", ClaimValueTypes.String ), // app DB ID
                    new Claim( "name", name, ClaimValueTypes.String ), // full name
                    new Claim( ClaimTypes.Email, email ),
                    new Claim( ClaimTypes.Role, $"{role}"),
                    new Claim( "jti", Guid.NewGuid().ToString(), ClaimValueTypes.String ),
                    //new Claim( "picture", picture, ClaimValueTypes.String ),
                }),
            Expires = expires.DateTime,
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(this.Key, SecurityAlgorithms.HmacSha256Signature)
        };

        // adds the client signature as as claim
        if ( !String.IsNullOrWhiteSpace(clientKey) )
            tokenDescriptor.Subject.AddClaim(new Claim( "cid", clientKey.Trim(), ClaimValueTypes.String ));

        // make a token
        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

        return new TokenResponse
        {
            Token = tokenHandler.WriteToken(token),
            Expires = expires,
            //UserID = userID
        };
    }

    /// <summary>Generate a simple token from just an email address</summary>
    /// <param name="expiresIn">How many days from now to expire the token</param>
    /// <param name="interactive">True for normal users, false for integration accounts like PowerBI</param>
    /// <returns></returns>
    // public TokenResponse GenerateToken(Guid userID, string email, int expiresIn = 14, UserRoleLevel role = UserRoleLevel.Guest, bool interactive = true)
    // {
    //     if (userID == Guid.Empty || String.IsNullOrWhiteSpace(email))
    //         return new TokenResponse();

    //     expiresIn = Math.Clamp(expiresIn, 0, 365);

    //     DateTimeOffset expires = DateTimeOffset.UtcNow.AddDays(expiresIn);

    //     // define the token
    //     var tokenDescriptor = new SecurityTokenDescriptor
    //     {
    //         // create a token that last 14 days
    //         Subject = new ClaimsIdentity(new Claim[]
    //             {
    //                 new Claim( "userid", $"{userID}", ClaimValueTypes.String ),
    //                 new Claim( ClaimTypes.Email, email ),
    //                 new Claim( ClaimTypes.Role, $"{role}"),
    //                 new Claim( "jti", Guid.NewGuid().ToString(), ClaimValueTypes.String ),
    //                 // interactive
    //                 new Claim( "int", $"{interactive}", ClaimValueTypes.Boolean )
    //             }),
    //         Expires = expires.DateTime,
    //         Issuer = Issuer,
    //         Audience = Audience,
    //         SigningCredentials = new SigningCredentials(this.Key, SecurityAlgorithms.HmacSha256Signature)
    //     };

    //     // make a token
    //     var tokenHandler = new JwtSecurityTokenHandler();
    //     SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

    //     return new TokenResponse
    //     {
    //         Token = tokenHandler.WriteToken(token),
    //         Expires = expires,
    //         UserID = email
    //     };
    // }

    /// <summary>Validate an existing token with the key</summary>
    /// <param name="jwt">The JWT token to validate</param>
    /// <returns></returns>
    // public AppIdentity ValidateToken(string jwt)
    // {
    //     if (String.IsNullOrWhiteSpace(jwt))
    //         return new AppIdentity();

    //     // set the validation parameters
    //     TokenValidationParameters validationParameters = new TokenValidationParameters
    //     {
    //         ValidateLifetime = true, // optional
    //         ValidateAudience = true, // optional
    //         ValidateIssuer = true, // optional
    //         ValidIssuer = Issuer,
    //         ValidAudiences = new string[] { Audience },
    //         IssuerSigningKey = this.Key
    //         //IssuerSigningKeys = new List<SecurityKey> { key }
    //     };

    //     SecurityToken validatedToken;
    //     JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

    //     try
    //     {
    //         ClaimsPrincipal claims = handler.ValidateToken(jwt, validationParameters, out validatedToken);
    //         return new AppIdentity( claims.Identities.First() );
    //     }
    //     catch
    //     {
    //         return new AppIdentity();
    //     }
    // }

}
