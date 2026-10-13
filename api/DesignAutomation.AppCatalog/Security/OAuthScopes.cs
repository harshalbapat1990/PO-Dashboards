using System.ComponentModel;
using System.Text;
using DesignAutomation.AppCatalog.Extensions;

namespace DesignAutomation.AppCatalog.Security;

[FlagsAttribute]
public enum OAuthScope
{
    /// <summary>No scopes defined</summary>
    [Description("")]
    None = 0,
    /// <summary></summary>
    [Description("p1:read:user")]
    UserRead = 1,
    /// <summary></summary>
    [Description("openid")]
    OpenID = 2,
    /// <summary></summary>
    [Description("email")]
    Email = 4,
    /// <summary></summary>
    [Description("profile")]
    Profile = 8,
    /// <summary></summary>
    [Description("offline_access")]
    OfflineAccess = 16
}

/// <summary>Fromats an enum of scopes</summary>
public static class ScopeFormatter
{
    /// <summary>Reformats the scopes in the correct string format based on the selected flags</summary>
    public static string ToString(this OAuthScope scopes)
    {
        StringBuilder sb = new StringBuilder();

        Array all = Enum.GetValues(typeof(OAuthScope));

        foreach (OAuthScope flag in all)
        {
            if (scopes.HasFlag(flag))
            {
                string desc = flag.GetDescription();
                if (!String.IsNullOrWhiteSpace(desc))
                    sb.Append($"{desc}%20");
            }
        }

        sb.Length -= 3;
        return sb.ToString().ToLowerInvariant();
    }
}