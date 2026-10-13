using DesignAutomation.AppCatalog.Extensions;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace DesignAutomation.AppCatalog.Security;

/// <summary>A collection of Keys</summary>
public class KeyChain
{
    [JsonProperty("keys")]
    public List<RSAKey> Keys { get; set; } = new List<RSAKey>();

    public RSAKey GetKey(string id) => this.Keys?.Where(k => k.ID == id).FirstOrDefault() ?? new RSAKey();
}

/// <summary>An RSA Public Key Data Object from APS</summary>
/// <remarks>JWKS is Base64URL encoded</remarks>
public class RSAKey
{
    /// <summary>Algorithm</summary>
    [JsonProperty("alg")]
    public string Algorithm { get; set; } = String.Empty;

    /// <summary>Algorithm Family</summary>
    [JsonProperty("kty")]
    public string Family { get; set; } = String.Empty;

    [JsonProperty("kid")]
    public string ID { get; set; } = String.Empty;

    [JsonProperty("use")]
    public string Usage { get; set; } = String.Empty;

    [JsonProperty("n")]
    public string Modulus { get; set; } = String.Empty;

    [JsonProperty("e")]
    public string Exponent { get; set; } = String.Empty;

    /// <summary>From the BASE64Url back into the byte representation</summary>
    [JsonIgnore]
    public RSAParameters Parameters
    {
        get
        {
            return new RSAParameters
            {
                Modulus = this.Modulus.BASE64UrlDecode(),
                Exponent = this.Exponent.BASE64UrlDecode()
            };
        }
    }
}