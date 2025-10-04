using Serilog.Core;
using Serilog.Events;

namespace KludgeBox.Logging;

public class ShortSourceContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextValue))
        {
            var fullName = sourceContextValue.ToString().Trim('"');
            var shortName = fullName.Split('.').Last();
            var shortContext = propertyFactory.CreateProperty("ShortSourceContext", shortName);
            logEvent.AddPropertyIfAbsent(shortContext);
        }
    }
}