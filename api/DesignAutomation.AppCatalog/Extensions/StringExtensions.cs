using System.Security.Cryptography;
using System.Text;
using DesignAutomation.AppCatalog.Logic;

namespace DesignAutomation.AppCatalog.Extensions;

/// <summary>Tons of string utility functions</summary>
public static class StringExtensions
{
    public static bool IsAnyNullOrWhiteSpace( this string[] strs ) => strs.Count( s => String.IsNullOrWhiteSpace( s ) ) > 0;

    /// <summary>Returns all of a string after the specified LAST occurance of the specified token</summary>
    /// <returns>The substring</returns>
    public static string GetLastAfter(this string str, char token)
    {
        if (str.Length < 1)
            return String.Empty;

        int idx = str.LastIndexOf(token);

        if (idx < 0 || idx >= str.Length - 1)
            return String.Empty;

        return str.Substring(idx + 1, str.Length - idx - 1).Trim();
    }

    /// <summary>Truncates a string to the the specified length or less</summary>
    public static string Truncate(this string str, int length, bool trim = true)
    {
        if (String.IsNullOrEmpty(str))
            return String.Empty;

        // if greater than length
        if (str.Length > length)
            return trim ? str.Substring(0, length).Trim() : str.Substring(0, length);

        return trim ? str.Trim() : str;
    }

    /// <summary></summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool IsValidJacobsEmail(this string str)
    {
        if (String.IsNullOrWhiteSpace(str))
            return false;

        str = str.Trim();

        if (!str.EndsWith("@jacobs.com"))
            return false;

        return EmailValidator.Validate(str);
    }

    /// <summary></summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public static string BASE64Decode(this string str)
    {
        if (String.IsNullOrWhiteSpace(str))
            return String.Empty;

        var bytes = Convert.FromBase64String(str);
        return Encoding.UTF8.GetString(bytes);
    }
    
    /// <summary>Decodes a BASE64 URL encoded string back to its original bytes</summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public static byte[] BASE64UrlDecode(this string str)
    {
        str = str.Replace('-', '+'); // 62nd char of encoding
        str = str.Replace('_', '/'); // 63rd char of encoding

        // the modulus of the length by 4 can not be remaninder 1
        switch (str.Length % 4)
        {
            // no padding necessary
            case 0: break;
            // pad with two =
            case 2: str += "=="; break;
            // pad once
            case 3: str += "="; break;
            // hopefully this does not happen
            default:
                throw new System.Exception("Illegal BASE64URL string!");
        }

        return Convert.FromBase64String(str); // Standard base64 decoder
    }

        /// <summary>Hash the string to a SHA256 digest</summary>
    /// <param name="str">single input string to hash</param>
    /// <returns>byte array</returns>
    public static byte[] HashToSHA256(this string str)
    {
        using SHA256 haser = SHA256.Create();
        return haser.ComputeHash(Encoding.UTF8.GetBytes(str));
    }
}
