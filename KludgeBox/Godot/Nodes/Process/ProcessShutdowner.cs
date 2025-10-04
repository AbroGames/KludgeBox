using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace KludgeBox.Godot.Nodes.Process;

// ReSharper disable once Godot.MissingParameterlessConstructor
public partial class ProcessShutdowner : Node
{
    
    private static readonly long[] ProcessShutdownNotificationTypes =
    [
        NotificationWMCloseRequest, 
        NotificationCrash, 
        NotificationDisabled, 
        NotificationPredelete,
        NotificationExitTree
    ];

    [Logger] private ILogger _log;
    
    private readonly int _processPid;
    private readonly Func<int, string> _logMessageGenerator = pid => $"Kill process {pid}.";
    
    public ProcessShutdowner(int processPid, Func<int, string> logMessageGenerator = null)
    {
        Di.Process(this);
        
        _processPid = processPid;
        if (logMessageGenerator != null) _logMessageGenerator = logMessageGenerator;
    }
    
    public override void _Notification(int id)
    {
        if (ProcessShutdownNotificationTypes.Contains(id) && OS.IsProcessRunning(_processPid))
        {
            Shutdown();
        }
    }

    public void Shutdown()
    {
        _log.Information(_logMessageGenerator(_processPid));
        OS.Kill(_processPid);
    }
}
