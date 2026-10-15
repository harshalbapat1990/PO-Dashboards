using System.Net;
using System.Security.Cryptography;
using System.Text;
using DesignAutomation.AppCatalog.Config;
using DesignAutomation.AppCatalog.Data;
using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.API;

/// <summary>Issues short-lived local admin JWTs for development without an external identity provider.</summary>
public sealed class LocalDevelopmentAuthController(
    AppConfiguration configuration,
    IUsersRepository users,
    ILogger<LocalDevelopmentAuthController> logger)
{
    [Function("LocalDevelopmentToken")]
    public async Task<HttpResponseData> IssueToken(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/local-auth/token")] HttpRequestData request)
    {
        if (configuration.Environment != EnvironmentType.Local)
            return request.CreateResponse(HttpStatusCode.NotFound);

        LocalDevelopmentLogin? login;
        try
        {
            var body = await request.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return request.BadResponse("Email and local development password are required.");
            login = JsonConvert.DeserializeObject<LocalDevelopmentLogin>(body);
        }
        catch (JsonException)
        {
            return request.BadResponse("Invalid login payload.");
        }

        if (login is null || string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password)
            || !FixedTimeEquals(login.Email, configuration.LocalAdminEmail)
            || !FixedTimeEquals(login.Password, configuration.LocalAdminPassword))
        {
            logger.LogWarning("Rejected local development token request.");
            return request.CreateResponse(HttpStatusCode.Unauthorized);
        }

        try
        {
            var user = users.GetByEmail(configuration.LocalAdminEmail);
            if (user is null)
            {
                user = new AppUser { Email = configuration.LocalAdminEmail, Role = UserRoleLevel.Admin };
                var id = await users.Insert(user);
                if (id == Guid.Empty)
                    user = users.GetByEmail(configuration.LocalAdminEmail);
            }
            else if (user.Role != UserRoleLevel.Admin)
            {
                await users.UpdateRole(user.ID, UserRoleLevel.Admin);
                user.Role = UserRoleLevel.Admin;
            }

            if (user is null)
                throw new InvalidOperationException("Unable to provision the local development user.");

            var headers = request.GetClientHeaders();
            var token = new JWTManager(Convert.FromBase64String(configuration.TokenKey))
                .GenerateToken(user.ID, user.Email, user.Email, headers.ClientKey, 1, UserRoleLevel.Admin);
            return request.OKResponse(new APIResult<TokenResponse>(token, APIResultType.Success));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to issue a local development token.");
            return request.ReponseError(exception, logger);
        }
    }

    private static bool FixedTimeEquals(string supplied, string expected)
    {
        if (string.IsNullOrEmpty(supplied) || string.IsNullOrEmpty(expected))
            return false;

        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash);
    }
}

public sealed record LocalDevelopmentLogin(string Email, string Password);
