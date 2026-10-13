using System.Text;

namespace DesignAutomation.AppCatalog.Extensions;

public static class ByteExtensions
{
    /// <summary>Encodes a byte array as BASE64 URL encoded</summary>
    public static string BASE64UrlEncode(this byte[] arg)
    {
        string s = Convert.ToBase64String(arg); // Regular base64 encoder

        s = s.TrimEnd('='); // remove any trailing =
        s = s.Replace('+', '-'); // 62nd char of encoding
        return s.Replace('/', '_'); // 63rd char of encoding
    }

    /// <summary></summary>
    public static string BytesToString(this byte[] bytes, char delim = ' ')
    {
        StringBuilder sb = new StringBuilder();

        foreach (byte b in bytes)
            sb.Append($"{b:X2}{delim}");

        return sb.ToString().TrimEnd();
    }

    /// <summary>Converts a byte array as a hex string</summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    public static string ToHexString( this byte[] hex )
    {
        if ( hex == null || hex.Length < 1 )
            return String.Empty;

        StringBuilder s = new StringBuilder();

        Array.ForEach( hex, b => s.Append( b.ToString( "x2" ) ) );

        return s.ToString();
    }

}
