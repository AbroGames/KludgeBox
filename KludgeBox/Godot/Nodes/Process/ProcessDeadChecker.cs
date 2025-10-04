using Godot;
using KludgeBox.Core.Cooldown;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.Logging;
using Serilog;

namespace KludgeBox.Godot.Nodes.Process;

// ReSharper disable once Godot.MissingParameterlessConstructor
public partial class ProcessDeadChecker : Node
{
    
    private readonly int? _processPid;
    private readonly Func<int, string> _logMessageGenerator = pid => $"Process {pid} is dead.";
    private readonly Action _actionWhenDead;
    
    private readonly AutoCooldown _processDeadCheckCooldown = new(5);
    
    [Logger] private ILogger _log;
    
    public ProcessDeadChecker(int processPid, Action actionWhenDead, Func<int, string> logMessageGenerator = null)
    {
        Di.Process(this);
        
        _processPid = processPid;
        _actionWhenDead = actionWhenDead;
        if (logMessageGenerator != null) _logMessageGenerator = logMessageGenerator;
        
        _processDeadCheckCooldown.ActionWhenReady += CheckProcessIsDead;
    }

    public override void _Process(double delta)
    {
        _processDeadCheckCooldown.Update(delta);
    }

    private void CheckProcessIsDead()
    {
        if (_processPid.HasValue && !System.Diagnostics.Process.GetProcesses().Any(x => x.Id == _processPid.Value))
        {
            _log.Information(_logMessageGenerator(_processPid.Value));
            _actionWhenDead?.Invoke();
        }
    }
}
