using System.Text;
using DesignAutomation.AppCatalog.Extensions;
using DesignAutomation.AppCatalog.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DesignAutomation.AppCatalog.Security;

/// <summary>How does the client want to get the token back</summary>
public enum TokenRetrieveType
{
    /// <summary>Send back to as a redirect to localhost</summary>
    Redirect = 0,
    /// <summary>The client will poll for it's token</summary>
    Polling = 1,
    /// <summary>The client will scrape the browser window for the token.</summary>
    Scrape = 2
}

/// <summary></summary>
public class OAuthState
{
    /// <summary>Minimum port range to allow</summary>
    public const int MinPort = 5001;

    /// <summary>Max port range to allow</summary>
    public const int MaxPort = 65535;

    private int _port = 0;

    private string _id = String.Empty;

    private string _last = String.Empty;

    /// <summary>Parameterless constructor</summary>
    public OAuthState()
    { }

    /// <summary>Port to use with a redirect if needed</summary>
    /// <remarks></remarks>
    [JsonProperty("port")]
    public int Port
    {
        get => this._port;
        set => this._port = Math.Clamp(value, MinPort, MaxPort);
    }

    /// <summary>The page the user tried to going before being sent to login</summary>
    /// <remarks></remarks>
    [JsonProperty("last")]
    public string LastPage
    {
        get => this._last;
        set => this._last = !String.IsNullOrWhiteSpace(value) ? value.Trim() : "/home";
    }

    /// <summary>How the client gets the token</summary>
    /// <remarks></remarks>
    [JsonProperty("type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public TokenRetrieveType Method { get; set; } = TokenRetrieveType.Redirect;

    /// <summary>Environment the request came from</summary>
    /// <remarks></remarks>
    [JsonProperty("env")]
    [JsonConverter(typeof(StringEnumConverter))]
    public EnvironmentType Environment { get; set; } = EnvironmentType.Production;

    /// <summary>The App/Client instance install ID - not a user ID</summary>
    /// <remarks>Optional</remarks>
    [JsonProperty("clientID")]
    public string ClientID
    {
        get => this._id;
        set => this._id = !String.IsNullOrWhiteSpace(value) ? value.Trim() : String.Empty;
    }

    /// <summary>true if an ID was set</summary>
    [JsonIgnore]
    public bool HasID => !String.IsNullOrWhiteSpace(this.ClientID);

    /// <summary>Encodes to BASE64UrlEncoding</summary>
    /// <returns></returns>
    public string Encode()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        return bytes.BASE64UrlEncode();
    }

    /// <summary>Encodes an object</summary>
    /// <param name="encoded"></param>
    /// <returns></returns>
    public static OAuthState? Decode(string? encoded)
    {
        if (String.IsNullOrWhiteSpace(encoded))
            return null;

        byte[] bytes = encoded.BASE64UrlDecode();
        string json = Encoding.UTF8.GetString(bytes);

        // catch a deserialize error
        return JsonConvert.DeserializeObject<OAuthState>(json);
    }
}
