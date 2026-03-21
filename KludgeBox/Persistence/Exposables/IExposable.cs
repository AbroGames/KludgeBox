namespace KludgeBox.Persistence.Exposables;


/// <summary>
/// Сложный объект, который можно сохранить в IPersistenceContainer.
/// </summary>
public interface IExposable
{
    /// <summary>
    /// При сохранении вызывается единожды, при загрузке может быть вызван несколько раз, в разных состояниях контейнера.<br/>
    /// Пример использования:
    /// <code>
    /// public void ExposeData(IPersistenceContainer container)
    /// {
    ///     container.ExposeValue(ref hp, "hp");
    /// }
    /// </code>
    /// </summary>
    /// <param name="container">Контейнер сохранения с методами доступа</param>
    void ExposeData(IPersistenceContainer container);
}

/// <summary>
/// Сложный объект, имеющий уникальный идентификатор. Может быть сохранен как IExposable, но в отличие от него,
/// может быть частью сразу нескольких других объектов, ссылающихся на него.
/// </summary>
public interface IRefExposable : IExposable
{
    /// <summary>
    /// Уникальный Id этого экземпляра. Должен сохранять свое уникальное значение только в процессе сохранения.
    /// </summary>
    /// <returns></returns>
    string GetUniqueId();
}
