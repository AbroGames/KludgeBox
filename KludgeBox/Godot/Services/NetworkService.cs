namespace KludgeBox.Godot.Services;

public abstract class NetworkService
{
    
    public abstract bool IsClient();
    public abstract bool IsServer();

    public void DoClient(Action clientAction)
    {
        if (IsClient())
        {
            clientAction();
        }
    }
    
    public void DoNotClient(Action notClientAction)
    {
        if (!IsClient())
        {
            notClientAction();
        }
    }
    
    public void DoServer(Action serverAction)
    {
        if (IsServer())
        {
            serverAction();
        }
    }
    
    public void DoNotServer(Action notServerAction)
    {
        if (!IsServer())
        {
            notServerAction();
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
    
    public void DoServerNotServer(Action serverAction, Action notServerAction)
    {
        DoServer(serverAction);
        DoNotServer(notServerAction);
    }
    
    public void DoClientNotClient(Action clientAction, Action notClientAction)
    {
        DoClient(clientAction);
        DoNotClient(notClientAction);
    }
}