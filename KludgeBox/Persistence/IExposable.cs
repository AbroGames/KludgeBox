using KludgeBox.Persistence.DataContainers;

namespace KludgeBox.Persistence;

public interface IExposable
{
    void ExposeData(ExposableContainer container);
}

interface IRefExposabe : IExposable
{
    string GetUniqueId();
}

interface ISceneExposable : IExposable
{
    string GetScenePath();
}