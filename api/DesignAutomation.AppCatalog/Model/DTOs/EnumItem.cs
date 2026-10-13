using DesignAutomation.AppCatalog.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DesignAutomation.AppCatalog.Model;

/// <summary></summary>
/// <typeparam name="T"></typeparam>
public record EnumItem<T> where T : System.Enum
{
	protected T _value = default!;

	public EnumItem(T value)
	{
		this._value = value;
	}

	[JsonProperty("key", Order = 2)]
	[JsonConverter(typeof(StringEnumConverter))]
	public virtual T Value => this._value;

	/// <summary>Item Description from the enum item's description attribute</summary>
	[JsonProperty("desc", Order = 3)]
	public virtual string Description => this._value.GetDescription();
}