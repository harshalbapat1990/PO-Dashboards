using DesignAutomation.AppCatalog.Model;
using Newtonsoft.Json;

namespace DesignAutomation.AppCatalog.Serializers;

/// <summary>Reads and writes JSON of a Flags Enum array</summary>
public class FlagsArrayConverter : JsonConverter<Tier1AppType>
{
    /// <summary>Deserailize the JSON back to an enum flags array</summary>
    /// <returns></returns>
    public override Tier1AppType ReadJson( JsonReader reader, Type objectType, Tier1AppType value, bool hasExistingValue, JsonSerializer serializer )
    {
        Tier1AppType results = Tier1AppType.None;

        string s = reader.ReadAsString()?.Trim() ?? String.Empty;

        if ( String.IsNullOrWhiteSpace(s) )
            return Tier1AppType.None; 

        // read through the string array parsing each enum
        do
        {
            // only add if s actually parses to an enum
            if ( !String.IsNullOrWhiteSpace( s ) && Enum.TryParse<Tier1AppType>( s, true, out Tier1AppType t ) )
                results |= results;

            s = reader.ReadAsString()?.Trim() ?? String.Empty;

        } while ( reader.TokenType != JsonToken.EndArray );

        return results;
    }

    /// <summary>Write out the JSON from an array of flags</summary>
    public override void WriteJson( JsonWriter writer, Tier1AppType value, JsonSerializer serializer )
    {
        // if no incoming value then just create empty array
        if ( value == Tier1AppType.None )
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
            return;
        }

        // get all the values as an array except for 0
        List<Tier1AppType> all = Enum.GetValues(typeof(Tier1AppType)).Cast<Tier1AppType>().Where( i => i != Tier1AppType.None).ToList();

        writer.WriteStartArray();
        all.ForEach( flag =>
        {
            if ( value.HasFlag(flag) )
                writer.WriteValue( $"{flag}" );
        });
        writer.WriteEndArray();
    }
}