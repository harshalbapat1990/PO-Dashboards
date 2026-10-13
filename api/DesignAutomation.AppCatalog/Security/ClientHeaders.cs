using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker.Http;

namespace DesignAutomation.AppCatalog.Security;

/// <summary>Encapsulates the Client Headers we need later because context to the request will disapate</summary>
// public record ClientHeaders
// {
//     /// <summary></summary>
//     public const string StaleRequestKey = "x-timestamp";

//     /// <summary></summary>
//     public const string APIKeyKey = "x-functions-key";

//     /// <summary></summary>
//     public const string CSRFTokenKey = "x-csrf-token";

//     /// <summary>The clients IP address</summary>
//     private string _ip = "0.0.0.0";

//     public ClientHeaders() {}

//     /// <summary>Gets all the client headers we are interested in for later</summary>
//     public ClientHeaders(HttpRequestData req)
//     {
//         if (req == null || req.Headers == null)
//             return;

//         this.ClientIP = GetClientIP(req);
//         this.ClientAgent = GetUserAgent(req);
//         this.AuthToken = GetToken(req); //GetBearerToken(req);
//         this.CSRFToken = GetHeaderValue(req, CSRFTokenKey) ?? String.Empty;
//         this.APIKey = GetHeaderValue(req, APIKeyKey) ?? req.Query["code"]?.ToString() ?? string.Empty;
//     }

//     #region [ Properties ]

//     /// <summary>Store the client agent</summary>
//     public string ClientAgent { get; set; } = "no client";

//     /// <summary>Store the Client IP from the Request</summary>
//     public string ClientIP
//     {
//         get => !String.IsNullOrWhiteSpace(this._ip) ? this._ip.Trim() : "0.0.0.0";
//         set => this._ip = value;
//     }

//     /// <summary>Stores the type of auth such as None, Basic, Bearer, Cookie</summary>
//     public string AuthType { get; set; } = String.Empty;

//     /// <summary>Stores a Bearer JWT Auth Token from the header</summary>
//     public string AuthToken { get; set; } = String.Empty;

//     /// <summary>Stores any CSRF Token from the header</summary>
//     public string CSRFToken { get; set; } = String.Empty;

//     /// <summary></summary>
//     public string APIKey { get; set; } = String.Empty;

//     #endregion [ Properties ]

//     /// <summary>Get a header value by its key</summary>
//     /// <param name="key">Case sensitive key</param>
//     public static string? GetHeaderValue(HttpRequestData req, string key)
//     {
//         if (String.IsNullOrWhiteSpace(key))
//             return null;

//         key = key.Trim();

//         // only gets the first value
//         if (req.Headers.Contains(key))
//             return req.Headers.GetValues(key).FirstOrDefault()?.Trim();

//         return null;
//     }

//     /// <summary>Get a header value by its key</summary>
//     /// <param name="key">Case sensitive key</param>
//     public static string GetUserAgent(HttpRequestData req)
//     {
//         if (!req.Headers.Contains("User-Agent"))
//             return String.Empty;

//         // only gets the first value
//         var values = req.Headers.GetValues("User-Agent");

//         return (values != null && values.Count() > 0) ? String.Join(" ", values) : String.Empty;
//     }

//     /// <summary>Gets a Authorization Bearer token from the header</summary>
//     /// <param name="req"></param>
//     /// <returns>Returns JUST the token with no Bearer prefix</returns>
//     /// <remarks>Needs to check for different schemes and enumerate those</remarks>
//     public static string GetBearerToken(HttpRequestData req)
//     {
//         string? value = GetHeaderValue(req, "Authorization");

//         if (String.IsNullOrWhiteSpace(value) || !value.StartsWith("Bearer"))
//             return String.Empty;

//         var token = value.Substring("Bearer".Length).Trim();
//         return !String.IsNullOrWhiteSpace(token) ? token : String.Empty;
//     }

//     /// <summary>Gets a Authorization Bearer or Basic token from the header</summary>
//     /// <param name="req"></param>
//     /// <returns>Returns JUST the token with no prefix</returns>
//     /// <remarks>
//     /// DO NOT USE BUGGY needs more tests against different tokens
//     /// Needs to check for different schemes and enumerate those</remarks>
//     public static string GetToken(HttpRequestData req)
//     {
//         string? value = GetHeaderValue(req, "Authorization");

//         if (String.IsNullOrWhiteSpace(value))
//             return String.Empty;

//         var matches = new (@"^(Bearer|Basic|Cookie)\s+([-_\.\w\+=]{3,})$").Match(value);

//         return matches.Success ? matches.Groups[2].Value : String.Empty;
//     }

//     /// <summary>Specific to getting the IP address from the header without the client's port</summary>
//     /// <param name="req"></param>
//     /// <returns></returns>
//     /// <remarks>Only tested with IPv4</remarks>
//     public static string GetClientIP(HttpRequestData req)
//     {
//         string? value = GetHeaderValue(req, "X-Forwarded-For");

//         // should test for IP v4 vs v6
//         if (String.IsNullOrWhiteSpace(value))
//             return "0.0.0.0";

//         int idx = value.LastIndexOf(':');
//         if (idx > 2)
//             return value.Substring(0, idx);

//         return value;
//     }
// }
