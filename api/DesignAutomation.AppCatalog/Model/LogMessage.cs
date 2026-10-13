using System.Text;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>Holds just the Log Messsage and Arguments for formatting</summary>
public record LogMessage
{
	public string Message { get; set; } = String.Empty;

	public Dictionary<string, string> Args { get; set; } = new Dictionary<string, string>();

	/// <summary>Func that formats the log message object</summary>
	public static string FormatIt(LogMessage state, Exception? exp)
	{
		if (String.IsNullOrWhiteSpace(state?.Message))
			return String.Empty;

		// check for token replacement in the string
		foreach (KeyValuePair<string, string> arg in state.Args)
		{
			if (state.Message.Contains($"{{{arg.Key}}}"))
				state.Message = state.Message.Replace($"{{{arg.Key}}}", arg.Value);
		}

		return state.Message;
	}

	    /// <summary>Creates a verbose string message from an exception</summary>
    /// <param name="exp">The exception itself</param>
    /// <param name="includeStackTrace">True if you want the full stack trace, otherwise defaults to false</param>
    /// <returns></returns>
    public static string ToLogMessage( Exception exp, bool includeStackTrace = false )
    {
        StringBuilder msg = new StringBuilder( $"{exp.GetType().FullName} : {exp.Message} " );

        if ( includeStackTrace )
            msg.Append( exp.StackTrace );

        // get the inner exception if there is one
        if ( exp.InnerException != null )
        {
            msg.Append( $"{msg}; {exp.InnerException.Message} " );
            if ( includeStackTrace )
                msg.Append( exp.InnerException.StackTrace );
        }

        return msg.ToString();
    }
}