namespace KludgeBox.DI.Requests.SceneServiceInjection;

public class SceneServiceAttribute : Attribute
{
    public Type ServiceType { get; }
    
    public SceneServiceAttribute() : this(null) { }

    public SceneServiceAttribute(Type serviceType)
    {
        ServiceType = serviceType;
    }
}