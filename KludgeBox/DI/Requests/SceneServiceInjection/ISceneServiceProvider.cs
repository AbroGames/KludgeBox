namespace KludgeBox.DI.Requests.SceneServiceInjection;

public interface ISceneServiceProvider
{
    /// <summary>
    /// Returns service of specified type if found in scene or null.
    /// </summary>
    /// <param name="serviceType">Type of required service</param>
    /// <returns></returns>
    object GetService(Type serviceType);
}