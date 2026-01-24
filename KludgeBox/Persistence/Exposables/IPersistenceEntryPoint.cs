namespace Persistence;

public interface IPersistenceEntryPoint
{
    /// <summary>
    /// Состояние (режим работы) контейнера в данный момент.
    /// </summary>
    ContainerState State { get; }
    
    /// <summary>
    /// Сохраняет объект в поток байтов. Что делать с ними дальше - решай сам.
    /// </summary>
    /// <param name="root">Корневой объект</param>
    /// <returns>Поток байтов с указателем, установленным в конец потока</returns>
    Stream Save(IExposable root);

    /// <summary>
    /// Читает поток байт и преобразует его в корневой объект. Убедись, что поток байтов, который ты десериализуешь, был сериализован контейнером того же типа.
    /// </summary>
    /// <param name="source">Поток байтов с указателем, установленным в начало потока</param>
    /// <param name="ctorArgs">Аргументы конструктора для создания экземпляра перед его восстановлением</param>
    /// <returns>Восстановленный из потока экземпляр IExposable</returns>
    TExposable Load<TExposable>(Stream source, object[] ctorArgs = null) where TExposable : class, IExposable;
}