using Serilog;

namespace KludgeBox.Logging;

public class Logger
{
    private static ILogger _logger;
    
    static Logger()
    {
        _logger = LogFactory.GetForStatic("GENERAL");
    }

    public void Debug(string message) => _logger.Debug(message);
    public void Debug(object message) => _logger.Debug("{message}", message);
    public void Debug(string message, params object[] args) => _logger.Debug(message, args);
    public void Debug(Exception exception, string message) => _logger.Debug(exception, message);
    public void Debug(Exception exception, string message, params object[] args) => _logger.Debug(exception, message, args);

    public void Information(string message) => _logger.Information(message);
    public void Information(object message) => _logger.Information("{message}", message);
    public void Information(string message, params object[] args) => _logger.Information(message, args);
    public void Information(Exception exception, string message) => _logger.Information(exception, message);
    public void Information(Exception exception, string message, params object[] args) => _logger.Information(exception, message, args);
    
    public void Warning(string message) => _logger.Warning(message);
    public void Warning(object message) => _logger.Warning("{message}", message);
    public void Warning(string message, params object[] args) => _logger.Warning(message, args);
    public void Warning(Exception exception, string message) => _logger.Warning(exception, message);
    public void Warning(Exception exception, string message, params object[] args) => _logger.Warning(exception, message, args);
    
    public void Error(string message) => _logger.Error(message);
    public void Error(object message) => _logger.Error("{message}", message);
    public void Error(string message, params object[] args) => _logger.Error(message, args);
    public void Error(Exception exception, string message) => _logger.Error(exception, message);
    public void Error(Exception exception, string message, params object[] args) => _logger.Error(exception, message, args);
    
    public void Fatal(string message) => _logger.Fatal(message);
    public void Fatal(object message) => _logger.Fatal("{message}", message);
    public void Fatal(string message, params object[] args) => _logger.Fatal(message, args);
    public void Fatal(Exception exception, string message) => _logger.Fatal(exception, message);
    public void Fatal(Exception exception, string message, params object[] objects) => _logger.Fatal(exception, message, objects);
}