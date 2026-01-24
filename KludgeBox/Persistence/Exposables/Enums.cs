namespace Persistence;

public enum ExposeAs
{
    /// <summary>
    /// Попытаться определить автоматически
    /// </summary>
    Undefined,
    
    /// <summary>
    /// Сохранить как значение
    /// </summary>
    Value,
    
    /// <summary>
    /// Сохранить как сложный объект
    /// </summary>
    Deep,
    
    /// <summary>
    /// Сохранить как ссылку на сложный объект
    /// </summary>
    Reference
}

public enum ContainerState
{
    /// <summary>
    /// Контейнер ничего не делает
    /// </summary>
    Idle,
    
    /// <summary>
    /// Контейнер обходит дерево в поисках ссылок
    /// </summary>
    ScanReferences,
    
    /// <summary>
    /// Контейнер сохраняет объект
    /// </summary>
    Saving,
    
    /// <summary>
    /// Контейнер загружает объект
    /// </summary>
    Loading,
    
    /// <summary>
    /// Контейнер восстанавливает объекты после загрузки (в основном ссылки)
    /// </summary>
    RefsResolving
}