using DesignAutomation.AppCatalog.Model;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Extensions;

/// <summary>Extensions to HttpRequestData</summary>
public static partial class RequestExtensions
{
    /// <summary></summary>
    public const string APIKeyKey = "x-functions-key";

    /// <summary></summary>
    public const string StaleRequestKey = "x-timestamp";

    /// <summary>The header field key that holds the region value US|EU|JP</summary>
    public const string AuthHeaderKey = "x-authorization";

    /// <summary>Deserialize the request body as the specified type</summary>
    /// <typeparam name="T">Usually the DTO you want to deserialize into</typeparam>
    /// <param name="req">The request</param>
    /// <returns>True if success</returns>
    public static bool TryDeserializeJSONBody<T>(this HttpRequestData req, out T? data)
    {
        string? requestBody = req.ReadAsString();

        if (String.IsNullOrWhiteSpace(requestBody))
        {
            data = default;
            return false;
        }

        data = JsonConvert.DeserializeObject<T>(requestBody);

        return data != null;
    }

    /// <summary>Gets all the client headers we are interested in for later</summary>
    public static ClientHeaders GetClientHeaders( this HttpRequestData req )
    {
        if ( req == null || req.Headers == null )
            return new ClientHeaders();

        return new ClientHeaders
        {
            ClientIP = req.GetClientIP(),
            ClientAgent = req.GetUserAgent(),
            AuthToken = req.GetBearerToken() ?? String.Empty,
            RequestTimestamp = req.GetHeaderValue(StaleRequestKey)?.GetDateTimeValue(DateTimeOffset.MinValue) ?? DateTimeOffset.MinValue,
            CSRFToken = req.GetHeaderValue("x-csrf-token") ?? String.Empty,
            APIKey = GetHeaderValue(req, APIKeyKey) ?? req.Query["code"]?.ToString() ?? string.Empty
        };
    }

    /// <summary>Get a header value by its key</summary>
    /// <param name="key">Case sensitive key</param>
    public static string? GetHeaderValue( this HttpRequestData req, string key )
    {
        if ( String.IsNullOrWhiteSpace( key ) )
            return null;

        key = key.Trim();

        // only gets the first value
        if ( req.Headers.Contains( key ) )
            return req.Headers.GetValues( key ).FirstOrDefault()?.Trim();

        return null;
    }

    /// <summary>Get a header value by its key</summary>
    /// <param name="key">Case sensitive key</param>
    public static string GetUserAgent( this HttpRequestData req )
    {
        if ( !req.Headers.Contains( "User-Agent" ) )
            return String.Empty;

        // only gets the first value
        var values = req.Headers.GetValues( "User-Agent" );

        return ( values != null && values.Count() > 0 ) ? String.Join( " ", values ) : String.Empty;
    }

    /// <summary>Gets a Authorization Bearer token from the header</summary>
    /// <param name="req"></param>
    /// <returns>Returns JUST the token with no Bearer prefix</returns>
    /// <remarks>Needs to check for different schemes and enumerate those</remarks>
    public static string? GetBearerToken( this HttpRequestData req )
    {
        string? value = req.GetHeaderValue( "Authorization" );

        if ( String.IsNullOrWhiteSpace( value ) || !value.StartsWith( "Bearer" ) )
            return null;

        var token = value.Substring( "Bearer".Length ).Trim();
        return !String.IsNullOrWhiteSpace( token ) ? token : null;
    }

    /// <summary>Specific to getting the IP address from the header without the client's port</summary>
    /// <param name="req"></param>
    /// <returns></returns>
    /// <remarks>Only tested with IPv4</remarks>
    public static string GetClientIP( this HttpRequestData req )
    {
        string? value = req.GetHeaderValue( "X-Forwarded-For" );

        // should test for IP v4 vs v6
        if ( String.IsNullOrWhiteSpace( value ) )
            return "0.0.0.0";

        int idx = value.LastIndexOf( ':' );
        if ( idx > 2 )
            return value.Substring( 0, idx );

        return value;
    }
}