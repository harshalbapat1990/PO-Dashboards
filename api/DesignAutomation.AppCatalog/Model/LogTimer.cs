using System.Runtime.CompilerServices;

namespace DesignAutomation.AppCatalog.Model;

public interface ILogTimer
{
    DateTimeOffset Start {get; init;}

    void Stop([CallerMemberName] string function = "unknown");
}

public class LogTimer : ILogTimer
{
    public LogTimer()
    {
        this.Start = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset Start {get; init;}

    public void Stop([CallerMemberName] string function = "unknown")
    {
        var ms = (DateTimeOffset.UtcNow - this.Start).Milliseconds;
        SentrySdk.Metrics.EmitDistribution("func_response", ms, MeasurementUnit.Duration.Millisecond, 
            [new KeyValuePair<string, object>("func", function)]);
    }

}