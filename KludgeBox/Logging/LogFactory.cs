using System.Collections.Immutable;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace KludgeBox.Logging;

public class LogFactory
{
    public static bool GodotPushEnable { get; set; } = false;
    
    private static Dictionary<Type, ILogger> _loggersByType = new();
    private static Dictionary<string, ILogger> _loggersByName = new();
    private static ImmutableArray<ILogEventSink> _subscribers = ImmutableArray<ILogEventSink>.Empty;
    private static readonly BroadcastSink Broadcast = new();

    private const string DefaultTemplate =
        "|{Timestamp:HH:mm:ss.fff}| ({Level:u3}) ({ShortSourceContext}) {Message:lj}{NewLine}{Exception}";
    
    /// <summary>
    /// The sink receives the events of every logger, including the loggers created before the subscription.
    /// Emit is called from the async workers of the loggers, so the sink must be thread-safe.
    /// </summary>
    public static void Subscribe(ILogEventSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        ImmutableInterlocked.Update(ref _subscribers, subscribers => subscribers.Add(sink));
    }

    public static void Unsubscribe(ILogEventSink sink)
    {
        ImmutableInterlocked.Update(ref _subscribers, subscribers => subscribers.Remove(sink));
    }
    
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
            .WriteTo.Async(wt =>
            {
                wt.GodotRich(
                    outputTemplate: DefaultTemplate,
                    null,
                    () => GodotPushEnable
                );
                wt.Sink(Broadcast);
            });
        
        return config;
    }

    private class BroadcastSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            foreach (ILogEventSink subscriber in _subscribers)
            {
                // A failing subscriber must not take the events away from the others.
                try
                {
                    subscriber.Emit(logEvent);
                }
                catch (Exception e)
                {
                    SelfLog.WriteLine("Log subscriber {0} failed: {1}", subscriber, e);
                }
            }
        }
    }
}