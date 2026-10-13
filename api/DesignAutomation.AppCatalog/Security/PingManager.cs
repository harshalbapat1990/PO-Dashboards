using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Security;

/// <summary>Manages basic auth integration with Ping</summary>
public class PingManager : IOAuthManager
{
    /// <summary></summary>
    public PingManager(string clientID, string secret, string tenant, string challenge, string verifier)
    {
        this.ClientID = clientID;
        this.Secret = secret;
        this.TenantURL = tenant;
        this.Challenge = challenge;
        this.Verifier = verifier;
    }

    /// <summary></summary>
    public string ClientID { get; set; }

    /// <summary></summary>
    public string TenantURL { get; set; }

    /// <summary></summary>
    protected string Secret { get; init; }

    /// <summary></summary>
    protected string ChallengeMethod { get; init; } = "S256";

    /// <summary></summary>
    protected string Challenge { get; init; }

    /// <summary></summary>
    protected string Verifier { get; init; }

    /// <summary></summary>
    protected OAuthScope Scopes { get; init; } = OAuthScope.UserRead | OAuthScope.OpenID | OAuthScope.Email | OAuthScope.Profile | OAuthScope.OfflineAccess;

    /// <summary>Generate a login URL to Auth0</summary>
    /// <param name="url">The token callback URL</param>
    /// <returns></returns>
    public string Login(string redirect, OAuthState state) => $"{this.TenantURL}/as/authorize?response_type=code&client_id={this.ClientID}&redirect_uri={redirect}&scope={ScopeFormatter.ToString(this.Scopes)}&code_challenge={this.Challenge}&code_challenge_method={this.ChallengeMethod}&state={state.Encode()}";

    /// <summary>Logout URL</summary>
    public string Logout(string redirect) => $"{TenantURL}/as/signoff?post_logout_redirect_uri={redirect}";

    /// <summary>Gets a token</summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public async Task<Auth0Token?> GetToken(string code, string redirect)
    {
        if (String.IsNullOrWhiteSpace(code))
            return null;

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{TenantURL}/as/token");
        request.Headers.Clear();
        request.Headers.Add("x-timestamp", DateTime.UtcNow.ToString("o"));

        string authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Format("{0}:{1}", this.ClientID, this.Secret)));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        FormUrlEncodedContent form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("grant_type", "authorization_code"),
            new KeyValuePair<string,string>("code", code.Trim()),
            new KeyValuePair<string,string>("redirect_uri", redirect),
            new KeyValuePair<string,string>("code_verifier", this.Verifier)
        });

        string body = await form.ReadAsStringAsync();
        request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
        request.Content.Headers.ContentLength = Encoding.UTF8.GetBytes(body).Length;

        try
        {
            HttpResponseMessage httpResponse = await new HttpClient().SendAsync(request); ;
            string response = await httpResponse.Content.ReadAsStringAsync();

            // deserialize the response
            Auth0Token? results = JsonConvert.DeserializeObject<Auth0Token>(response);
            return results;
        }
        catch (System.Exception)
        {
            return null;
        }
    }

    /// <summary>Gets more details about the user</summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<IOAuthUser> GetUserInfo(string token)
    {
        if (String.IsNullOrWhiteSpace(token))
            return new Auth0User();

        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, $"{TenantURL}/as/userinfo");
        message.Headers.Clear();
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            HttpResponseMessage httpResponse = await new HttpClient().SendAsync(message); ;
            string response = await httpResponse.Content.ReadAsStringAsync();

            // deserialize the response
            Auth0User results = JsonConvert.DeserializeObject<Auth0User>(response);
            return results;
        }
        catch (System.Exception)
        {
            return new Auth0User();
        }
    }

    /// <summary>Retrieves the JWKS public keys</summary>
    /// <returns></returns>
    public async Task<KeyChain> GetKeys()
    {
        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, $"{TenantURL}/as/jwks");
        message.Headers.Clear();

        try
        {
            HttpResponseMessage httpResponse = await new HttpClient().SendAsync(message);
            string response = await httpResponse.Content.ReadAsStringAsync();

            var results = JsonConvert.DeserializeObject<KeyChain>(response);
            return results ?? new KeyChain();
        }
        catch (System.Exception)
        {
            return new KeyChain();
        }
    }

}