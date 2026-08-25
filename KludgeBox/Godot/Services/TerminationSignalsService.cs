using System.Runtime.InteropServices;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace KludgeBox.Godot.Services;

public abstract class TerminationSignalsService
{

    private static readonly PosixSignal[] TerminationSignals = [PosixSignal.SIGTERM, PosixSignal.SIGINT];

    [Logger] private ILogger _log;

    // Kept for the whole process lifetime: disposing a registration removes its handler.
    private readonly List<PosixSignalRegistration> _registrations = [];
    private int _terminationRequested;

    protected TerminationSignalsService()
    {
        Di.Process(this);
    }

    /// <summary>
    /// Turns SIGTERM and SIGINT (Ctrl+C, <c>kill</c>, <c>systemctl stop</c>, <c>docker stop</c>) into
    /// <see cref="Shutdown"/>. Godot installs no handler of its own, so without this the signal kills the
    /// process on the spot: no <c>NotificationExitTree</c>, no graceful shutdown.
    /// Must be called only when everything <see cref="Shutdown"/> goes through is initialized.
    /// </summary>
    public void Init()
    {
        foreach (PosixSignal signal in TerminationSignals)
        {
            _registrations.Add(PosixSignalRegistration.Create(signal, OnTerminationSignal));
        }
    }

    /// <summary>
    /// Shuts the application down gracefully. Called once, on the first termination signal.<br/>
    /// Runs on a runtime thread, not the main one: queue the work onto the main thread
    /// (for example, with <c>CallDeferred</c>) instead of touching the scene tree directly.
    /// </summary>
    protected abstract void Shutdown();

    private void OnTerminationSignal(PosixSignalContext context)
    {
        // A repeated signal is not cancelled and kills the process: the way out when a shutdown hangs.
        if (Interlocked.Exchange(ref _terminationRequested, 1) == 1) return;

        context.Cancel = true;
        _log.Information("Received {signal}, shutting down", context.Signal);
        Shutdown();
    }
}
