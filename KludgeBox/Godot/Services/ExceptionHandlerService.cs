using System.Diagnostics;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace KludgeBox.Godot.Services;

public class ExceptionHandlerService
{
    [Logger] private ILogger _log;

    public ExceptionHandlerService()
    {
        Di.Process(this);
    }
    
    public void AddExceptionHandlerForUnhandledException()
    {
        AppDomain.CurrentDomain.UnhandledException += HandleException;
    }
    
    private void HandleException(object sender, UnhandledExceptionEventArgs args)
    {
        // If logging will produce unhandled exception then we fucked up, so we need try/catch here.
        try
        {
            if (args.ExceptionObject is not Exception exception) return;
			
            _log.Error(exception.ToString());
        }
        catch (Exception e)
        {
            // Use GD.Print instead of Log.Error to avoid infinite recursion.
            GD.Print($"Unhandled exception: {args?.ExceptionObject}");
            GD.Print($"Unexpected exception was thrown while handling unhandled exception: {e}");
        }
		
        Debugger.Break();
    }
}