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

    /// <summary>Read access granted at product or portfolio scope.</summary>
    [Description("viewer")]
    Viewer = 1,

    /// <summary>May maintain assigned product data.</summary>
    [Description("productmanager")]
    ProductManager = 20,

    /// <summary>May maintain products in an assigned portfolio.</summary>
    [Description("portfoliomanager")]
    PortfolioManager = 40,

    /// <summary>Powerful user in the site who can modify most everything</summary>
    [Description("admin")]
    Admin = 90
}
