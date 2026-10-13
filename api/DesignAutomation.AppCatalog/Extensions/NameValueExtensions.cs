using System.Collections.Specialized;

namespace DesignAutomation.AppCatalog.Extensions;

/// <summary>Use this to replace HttpRequestData Extensions</summary>
public static class NameValueExtensions
{
    /// <summary>Get a string value from the request</summary>
    /// <param name="key">The key</param>
    /// <param name="def">Default value to set to if the key DNE which cannot be null only empty</param>
    /// <returns></returns>
    public static Guid GetGuidValue(this NameValueCollection? query, string key)
    {
        if (query == null || String.IsNullOrWhiteSpace(key))
            return Guid.Empty;

        var str = query[key.Trim()];

        return Guid.TryParse(str, out Guid value) ? value : Guid.Empty;
    }

    /// <summary>Get a string value from the request</summary>
    /// <param name="key">The key</param>
    /// <param name="def">Default value to set to if the key DNE which cannot be null only empty</param>
    /// <returns></returns>
    public static string GetStringValue(this NameValueCollection? query, string key, string def = "")
    {
        if ( query == null || String.IsNullOrWhiteSpace(key))
            return def;

        var value = query[key.Trim()]?.ToString();

        return String.IsNullOrWhiteSpace(value) ? def : value;
    }

    /// <summary>Get a string value from the request</summary>
    /// <param name="key">The key</param>
    /// <param name="def">Default is null if no key is found or is empty</param>
    /// <returns></returns>
    public static string? GetNullableStringValue(this NameValueCollection? query, string key)
    {
        if (query == null || String.IsNullOrWhiteSpace(key))
            return null;

        var value = query[key.Trim()]?.ToString();

        return String.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>Validate a string value from the request</summary>
    /// <param name="key"></param>
    /// <param name="def">Default value to set to if the key DNE</param>
    /// <returns></returns>
    public static IEnumerable<string> GetStringValues(this NameValueCollection? query, string key)
    {
        if (query == null || String.IsNullOrWhiteSpace(key))
            return [];

        return query.GetValues(key.Trim()) ?? [];
    }

    /// <summary>Parses a boolean value from the specified header</summary>
    /// <param name="key"></param>
    /// <param name="def">Default value to set to if the key DNE</param>
    /// <returns></returns>
    public static bool GetBoolValue(this NameValueCollection? query, string key, bool def = false)
    {
        if (query == null || String.IsNullOrWhiteSpace(key))
            return def;

        return query[key.Trim()]?.GetNullableBooleanValue() ?? def;
    }

    /// <summary>Parses an integer from a request query string</summary>
    /// <param name="key"></param>
    /// <param name="def">Default value to set to if the key DNE</param>
    /// <returns></returns>
    public static int GetIntValue(this NameValueCollection? query, string key, int def = 0)
    {
        if (query == null || String.IsNullOrWhiteSpace(key))
            return def;

        return query[key.Trim()]?.GetNullableIntValue() ?? def;
    }

    /// <summary>Parses a long from a request query string</summary>
    /// <param name="key"></param>
    /// <param name="def">Default value to set to if the key DNE</param>
    /// <returns></returns>
    public static long GetLongValue(this NameValueCollection? query, string key, int def = 0)
    {
        if (query == null || String.IsNullOrWhiteSpace(key))
            return def;

        return query[key.Trim()]?.GetLongValue() ?? def;
    }

    /// <summary></summary>
    /// <param name="key"></param>
    /// <param name="def">Default value to set to if the key DNE</param>
    /// <returns></returns>
    public static float GetFloatValue(this NameValueCollection? query, string key, float def = 0)
    {
        if (query == null || String.IsNullOrWhiteSpace(key))
            return def;

        return query[key.Trim()]?.GetNullableFloatValue() ?? def;
    }

    /// <summary></summary>
    /// <param name="key"></param>
    /// <param name="def">Default value to set to if the key DNE</param>
    /// <returns></returns>
    public static double GetDoubleValue(this NameValueCollection? query, string key, double def = 0)
    {
        if (query == null || String.IsNullOrWhiteSpace(key))
            return def;

        return query[key.Trim()]?.GetNullableDoubleValue() ?? def;
    }

    /// <summary>Parses an integer from a request query string</summary>
    /// <param name="key"></param>
    /// <param name="def">Default value to set to if the key DNE</param>
    /// <returns></returns>
    public static double? GetNullableDoubleValue(this NameValueCollection? query, string key)
    {
        if (query == null || String.IsNullOrWhiteSpace(key))
            return null;

        return query[key.Trim()]?.GetNullableDoubleValue();
    }

    /// <summary>Parse an enum value from the Request</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="req"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static T GetEnumValue<T>(this NameValueCollection? query, string key, T def = default) where T : struct, IConvertible
    {
        if (query == null || String.IsNullOrWhiteSpace(key))
            return def;

        key = key.Trim();

        if (!query.AllKeys.Contains(key))
            return def;

        var value = query[key]?.ToString();

        if (value == null)
            return def;

        return value.GetEnumValue<T>(true);
    }

    /// <summary>Parse an enum value from the Request</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="req"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static T? GetNullableEnumValue<T>(this NameValueCollection? query, string key) where T : struct, IConvertible
    {
        if (query == null || String.IsNullOrWhiteSpace(key))
            return null;

        key = key.Trim();

        if (!query.AllKeys.Contains(key))
            return null;

        var value = query[key]?.ToString();

        if (value == null)
            return null;

        return value.GetNullableEnumValue<T>(true);
    }

    /// <summary>Parse a list of enum values from the same key</summary>
    /// <param name="req"></param>
    /// <param name="key"></param>
    /// <param name="def">If default is true and no values are parse from the query - the result will contain at least default so there is one value.</param>
    /// <returns></returns>
    public static List<T> GetEnumValues<T>(this NameValueCollection? query, string key, bool def = false) where T : struct, IConvertible
    {
        List<T> results = new List<T>();

        // if a default is set then that value MUST be in the result list IFF no other values are found
        if (def)
            results.Add(default);

        if (query == null || String.IsNullOrWhiteSpace(key))
            return results;

        var values = GetStringValues(query, key).ToList();

        if (values.Count < 1)
            return results;

        results.Clear();

        values.ForEach(v =>
        {
            var ev = v.GetEnumValue<T>(true);
            if (!results.Contains(ev))
                results.Add(ev);
        });

        return results;
    }

}
