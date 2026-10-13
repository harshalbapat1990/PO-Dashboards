using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>This class is used to encapsulate the result of an API method call.</summary>
public class APIResult<T>
{
    #region [ Constructors ]


    /// <summary></summary>
    /// <param name="code"></param>
    public APIResult( APIResultType code ) : this( default!, code )
    {
    }

    /// <summary></summary>
    public APIResult() : this( default!, APIResultType.Unknown )
    {
    }

    /// <summary></summary>
    public APIResult(T data, APIResultType code = APIResultType.Unknown)
    {
        this.ResultCode = code;
        this.ResultObject = data;
    }

    #endregion [ Constructors ]

    /// <summary>The resulting data of a successful API method call</summary>
    [JsonProperty( "result" )]
    public T ResultObject { get; set; }

    /// <summary>The error code on error</summary>
    [JsonProperty( "resultType" )]
    [JsonConverter( typeof( StringEnumConverter ) )]
    public APIResultType ResultCode { get; set; }

    /// <summary>Returns whether or not the API method call was successful.</summary>
    [JsonProperty( "isSuccess" )]
    public bool IsSuccess => this.ResultCode == APIResultType.Success;

    /// <summary>Any additional debugging info</summary>
    [JsonProperty( "debug", NullValueHandling = NullValueHandling.Ignore)]
    public string? Debug { get; set; }
}

