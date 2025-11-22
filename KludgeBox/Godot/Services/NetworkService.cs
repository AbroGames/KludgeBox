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