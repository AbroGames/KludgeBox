namespace KludgeBox.Godot.Services;

public class NetworkService
{
    
    private Func<bool> _isServerChecker;
    private Func<bool> _isClientChecker;

    public NetworkService(Func<bool> isServerChecker, Func<bool> isClientChecker)
    {
        _isServerChecker = isServerChecker;
        _isClientChecker = isClientChecker;
    }
    
    public bool IsClient()
    {
        return _isClientChecker();
    }
    
    public bool IsServer()
    {
        return _isServerChecker();
    }

    public void DoClient(Action clientAction)
    {
        if (IsClient())
        {
            clientAction();
        }
    }
    
    public void DoServer(Action serverAction)
    {
        if (IsServer())
        {
            serverAction();
        }
    }
    
    public void DoClientServer(Action clientAction, Action serverAction)
    {
        DoClient(clientAction);
        DoServer(serverAction);
    }
    
    public void DoServerClient(Action serverAction, Action clientAction)
    {
        DoServer(serverAction);
        DoClient(clientAction);
    }
}