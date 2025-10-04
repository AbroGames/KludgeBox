using Serilog;

namespace KludgeBox.Logging;

public class LogFactory
{
    private static Dictionary<Type, ILogger> _loggersByType = new();
    private static Dictionary<string, ILogger> _loggersByName = new();

    private const string DefaultTemplate =
        "|{Timestamp:HH:mm:ss.fff}| ({Level:u3}) ({ShortSourceContext}) {Message:lj}{NewLine}{Exception}";
    
    public static ILogger GetForStatic<TContextType>()
    {
        return GetForStatic(typeof(TContextType));
    }
    
    public ILogger GetFor<TContextType>()
    {
        return GetForStatic<TContextType>();
    }
    
    public ILogger GetFor(Type contextType)
    {
        return GetForStatic(contextType); 
    }

    public ILogger GetFor(string contextName)
    {
        return GetForStatic(contextName); 
    }
    
    public static ILogger GetForStatic(Type contextType)
    {
        if (_loggersByType.TryGetValue(contextType, out var existingLogger))
        {
            return existingLogger;
        }
        
        var logger = GetDefaultLoggerConfiguration()
            .CreateLogger()
            .ForContext(contextType);
        
        _loggersByType.Add(contextType, logger);
        
        return logger;
    }

    public static ILogger GetForStatic(string contextName)
    {
        if (_loggersByName.TryGetValue(contextName, out var existingLogger))
        {
            return existingLogger;
        }
        
        var logger = GetDefaultLoggerConfiguration()
            .CreateLogger()
            .ForContext("ShortSourceContext", contextName);
        
        _loggersByName.Add(contextName, logger);
        
        return logger;
    }

    private static LoggerConfiguration GetDefaultLoggerConfiguration()
    {
        return GetBaseRichLoggerConfiguration();
    }

    private static LoggerConfiguration GetBaseRichLoggerConfiguration()
    {
        var config = new LoggerConfiguration()
            .Enrich.With<ShortSourceContextEnricher>()
            .WriteTo.Async(wt => wt.GodotRich(
                outputTemplate: DefaultTemplate
            ));
        
        return config;
    }
}