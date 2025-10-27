namespace KludgeBox.Persistence.DataContainers;

public enum ContainerState
{
    Idle,
    Saving,
    Loading,
    PostLoading
}

public class RootContainer
{
    public ExposableContainer Root { get; private set; }
    private Dictionary<string, ExposableContainer> _references = new();
}