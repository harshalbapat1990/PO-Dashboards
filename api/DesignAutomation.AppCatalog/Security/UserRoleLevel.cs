using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DesignAutomation.AppCatalog.Security;

/// <summary>Enumerates possible domain roles</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum UserRoleLevel
{
    /// <summary>No access</summary>
    [Description("guest")]
    Guest = 0,

    /// <summary>Lowest level read only access</summary>
    [Description("user")]
    User = 1,

    /// <summary>Powerful user in the site who can modify most everything</summary>
    [Description("admin")]
    Admin = 90
}