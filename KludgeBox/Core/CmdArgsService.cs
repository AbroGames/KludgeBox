using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.Logging;
using Serilog;

namespace KludgeBox.Core;

public class CmdArgsService
{

    protected readonly string[] CmdArgs = OS.GetCmdlineArgs();
    
    private bool _logIfEmpty; // Write message to log, then param doesn't exist in args
    private bool _logIfException; // Write message to log, then we catch Exception while find/parsing param
    private bool _logIfSuccessful; // Write message to log, then param successfully found
    
    [Logger] private ILogger _log;

    public CmdArgsService(bool logIfEmpty = false, bool logIfException = true, bool logIfSuccessful = false)
    {
        Di.Process(this);
        
        _logIfEmpty = logIfEmpty;
        _logIfException = logIfException;
        _logIfSuccessful = logIfSuccessful;
    }

    public void LogCmdArgs()
    {
        if (!CmdArgs.IsEmpty())
        {
            _log.Information("Cmd args: " + CmdArgs.Join());
        }
        else
        {
            _log.Information("Cmd args is empty");
        }
    }

    public bool ContainsInCmdArgs(string paramName)
    {
        bool argFound = CmdArgs.Contains(paramName);
        
        if (argFound && _logIfSuccessful) _log.Information($"Arg found: \"{paramName}\"");
        
        return argFound;
    }
    
    public string GetStringFromCmdArgs(string paramName, string defaultValue = null)
    {
        string arg = defaultValue;
        try
        {
            int argPos = CmdArgs.ToList().IndexOf(paramName);
            if (argPos == -1)
            {
                if (_logIfEmpty) _log.Information($"Arg {paramName} not setup.");
                return arg;
            }

            arg = CmdArgs[argPos + 1];
        }
        catch
        {
            if (_logIfException) _log.Warning($"Error while arg {paramName} setup.");
        }
        
        if (_logIfSuccessful) _log.Information($"Arg found: \"{paramName} {arg}\"");
        return arg;
    }

    public int? GetIntFromCmdArgs(string paramName)
    {
        string argAsString = GetStringFromCmdArgs(paramName, null);
        int? arg = null;

        try
        {
            if (argAsString != null)
            {
                arg = Convert.ToInt32(argAsString);
            }
        }
        catch (Exception ex) when (ex is FormatException || ex is OverflowException)
        {
            if (_logIfException) _log.Warning($"Arg {paramName} can't convert to Int32");
        }

        return arg;
    }
    
    public int GetIntFromCmdArgs(string paramName, int defaultValue)
    {
        string argAsString = GetStringFromCmdArgs(paramName, defaultValue.ToString());
        int arg = defaultValue;

        try
        {
            if (argAsString != null)
            {
                arg = Convert.ToInt32(argAsString);
            }
        }
        catch (Exception ex) when (ex is FormatException || ex is OverflowException)
        {
            if (_logIfException) _log.Warning($"Arg {paramName} can't convert to Int32");
        }

        return arg;
    }
}
