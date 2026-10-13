using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using System.Net.Mime;
using DesignAutomation.AppCatalog.Model;

namespace DesignAutomation.AppCatalog.Extensions;

/// <summary>Extensions to HttpRequestData</summary>
public static partial class RequestExtensions
{
    /// <summary>Rolls a simple OK response with no body</summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public static HttpResponseData EmptyResponse( this HttpRequestData req )
    {
        HttpResponseData resp = req.CreateResponse( HttpStatusCode.OK );
        resp.Headers.Add( "Content-Type", "text/plain; charset=utf-8" );
        resp.Headers.Add( "Content-Length", "0" );
        return resp;
    }

    /// <summary>Return an OK Response using the Newton Soft JSON serializer</summary>
    /// <param name="req">Request Object</param>
    /// <param name="results">result data</param>
    /// <param name="code">Response code to use which will default to 200</param>
    /// <returns></returns>
    /// <remarks>Rename to JSON Response since it could be any kind of response</remarks>
    public static HttpResponseData OKResponse<T>( this HttpRequestData req, APIResult<T> results, HttpStatusCode code = HttpStatusCode.OK )
    {
        HttpResponseData resp = req.CreateResponse( code );
        resp.Headers.Add( "Content-Type", "text/json; charset=utf-8" );
        //resp.Cookies.Append( new HttpCookie("raydreams", $"{Guid.NewGuid()}" ) { HttpOnly = true, Expires = DateTimeOffset.UtcNow.AddDays( 1 ) } );
        resp.WriteString( JsonConvert.SerializeObject( results ), Encoding.UTF8 );
        return resp;
    }

    /// <summary>Return a 400 Bad Request Response as plain text</summary>
    /// <param name="req"></param>
    /// <param name="errorMessage"></param>
    /// <returns></returns>
    public static HttpResponseData BadResponse( this HttpRequestData req, string errorMessage )
    {
        HttpResponseData resp = req.CreateResponse( HttpStatusCode.BadRequest );
        resp.Headers.Add( "Content-Type", "text/plain; charset=utf-8" );
        resp.WriteString( errorMessage, Encoding.UTF8 );

        return resp;
    }

    /// <summary>Return a 401 Unauthorized</summary>
    public static HttpResponseData UnauthorizedResponse(this HttpRequestData req) => req.CreateResponse(HttpStatusCode.Unauthorized);

    /// <summary>Rolls a redirect response to the specified URL</summary>
    /// <param name="req">The original HTTP request</param>
    /// <param name="url">Where to redirect to</param>
    /// <returns></returns>
    public static HttpResponseData Redirect( this HttpRequestData req, string url )
    {
        url = ( !String.IsNullOrWhiteSpace( url ) ) ? url.Trim() : String.Empty;

        HttpResponseData resp = req.CreateResponse( HttpStatusCode.Redirect );
        resp.Headers.Add( "Location", url );

        return resp;
    }

    /// <summary>Based on the environment will handle the correct error repsonse for debugging and logging</summary>
    /// <typeparam name="T">The normal return type of the function</typeparam>
    /// <param name="logger">The MS function logger</param>
    /// <param name="exp">exception</param>
    /// <param name="results">The results object that would have been returned to add debug to</param>
    /// <returns>In PROD returns a 500, otherwise a 200 with a Debug message</returns>
    [Obsolete("Replace in all the controllers with the non-generic one.")]
    public static HttpResponseData ReponseError<T>( this HttpRequestData req, APIResult<T> results, Exception exp, ILogger logger )
    {
        var type = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        EnvironmentType env = type?.GetEnumValue<EnvironmentType>(EnvironmentType.Local, true) ?? EnvironmentType.Local;

        string error = LogMessage.ToLogMessage(exp, true );

        // only in production
        if ( env == EnvironmentType.Production )
        {
            logger.LogError( error );
            return req.CreateResponse( HttpStatusCode.InternalServerError );
        }

        // in debug
        results.Debug = error;

        HttpResponseData resp = req.CreateResponse( HttpStatusCode.OK );
        resp.Headers.Add( "Content-Type", "text/json; charset=utf-8" );
        resp.WriteString( JsonConvert.SerializeObject( results ) );

        return resp;
    }

    /// <summary>Based on the environment will handle the correct error repsonse for debugging and logging</summary>
    /// <typeparam name="T">The normal return type of the function</typeparam>
    /// <param name="logger">The MS function logger</param>
    /// <param name="exp">exception</param>
    /// <returns>In PROD returns a 500, otherwise a 200 with a Debug message</returns>
    public static HttpResponseData ReponseError( this HttpRequestData req, Exception exp, ILogger logger )
    {
        var type = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        EnvironmentType env = type?.GetEnumValue<EnvironmentType>(EnvironmentType.Local, true) ?? EnvironmentType.Local;

        string error = LogMessage.ToLogMessage(exp, true );

        // Production will not echo the error but throw 500
        if ( env == EnvironmentType.Production )
        {
            logger.LogError( error );
            return req.CreateResponse( HttpStatusCode.InternalServerError );
        }

        APIResult<bool> results = new APIResult<bool> { ResultObject = false, ResultCode = APIResultType.Exception, Debug = error };

        HttpResponseData resp = req.CreateResponse( HttpStatusCode.OK );
        resp.Headers.Add( "Content-Type", "text/json; charset=utf-8" );
        resp.WriteString( JsonConvert.SerializeObject( results ) );

        return resp;
    }

    /// <summary>Use for return most text types reponses including HTML and plain text</summary>
    /// <param name="req"></param>
    /// <param name="body"></param>
    /// <param name="inline">When true serves the file in the browser while false forces a download</param>
    /// <param name="filename">filename to use when downloaded</param>
    /// <param name="contentType">MIME type</param>
    /// <param name="code">Response code</param>
    /// <returns></returns>
    public static HttpResponseData TextResponse( this HttpRequestData req, string body, bool inline = true, string contentType = "text/plain", string filename = "download.txt", HttpStatusCode code = HttpStatusCode.OK )
    {
        if ( String.IsNullOrWhiteSpace( body ) )
            return req.EmptyResponse();

        // set the stream disposition
        ContentDisposition dispo = new ContentDisposition
        {
            FileName = filename,
            Inline = inline
        };

        contentType = ( String.IsNullOrWhiteSpace( contentType ) ) ? "text/plain" : contentType.Trim();

        HttpResponseData resp = req.CreateResponse( code );
        resp.Headers.Add( "Content-Type", $"{contentType}; charset=utf-8" );
        resp.Headers.Add( "Content-Disposition", dispo.ToString() );
        resp.WriteString( body, Encoding.UTF8 );
        return resp;
    }

}