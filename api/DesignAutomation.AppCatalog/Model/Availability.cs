using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DesignAutomation.AppCatalog.Model;

/// <summary></summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum AvailabilityLevel : byte
{
	/// <summary></summary>
	[Description("Undecided")]
	Undecided = 0,
	/// <summary></summary>
	[Description("One Week")]
	OneWeek = 1,
	/// <summary></summary>
	[Description("One Day")]
	OneDay = 2,
	/// <summary></summary>
	[Description("Half Day")]
	HalfDay = 3,
	/// <summary></summary>
	[Description("One Hour")]
	OneHour = 4,
	/// <summary>99.99%</summary>
	[Description("One Minute")]
	OneMinute = 5
}