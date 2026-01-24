namespace Persistence;



public interface IPersistenceContainer
{
    /// <summary>
    /// Сохраняет или читает значение
    /// </summary>
    /// <param name="value">Значение</param>
    /// <param name="label">Имя значения в контейнере сохранения. Должно быть уникальным в пределах текущего IExposable</param>
    /// <param name="defaultValue">Значение по умолчанию. Никуда не сохраняется.</param>
    /// <typeparam name="TValue">Тип сохраняемого значения</typeparam>
    void Expose_Value<TValue>(ref TValue value, string label, TValue defaultValue = default);

    /// <summary>
    /// Сохраняет или читает сложный объект (IExposable)
    /// </summary>
    /// <param name="value">Объект</param>
    /// <param name="label">Имя значения в контейнере сохранения. Должно быть уникальным в пределах текущего IExposable</param>
    /// <param name="ctorArgs">Аргументы, передаваемые в конструктор для создания объекта. Они не сериализуются.</param>
    /// <typeparam name="TValue">Тип сохраняемого значения</typeparam>
    void Expose_Deep<TValue>(ref TValue value, string label, object[] ctorArgs = null) where TValue : class, IExposable;

    /// <summary>
    /// Сохраняет или читает сложный объект как ссылку (IRefExposable)
    /// </summary>
    /// <param name="value">Объект</param>
    /// <param name="label">Имя значения в контейнере сохранения. Должно быть уникальным в пределах текущего IExposable</param>
    /// <param name="ctorArgs">Аргументы, передаваемые в конструктор для создания объекта. Они не сериализуются.</param>
    /// <typeparam name="TValue">Тип сохраняемого значения</typeparam>
    void Expose_Reference<TValue>(ref TValue value, string label, object[] ctorArgs = null) where TValue : class, IRefExposable;

    /// <summary>
    /// Сохраняет или читает список. 
    /// </summary>
    /// <param name="list">Список</param>
    /// <param name="label">Имя значения в контейнере сохранения. Должно быть уникальным в пределах текущего IExposable</param>
    /// <param name="exposeValueAs">Каким образом будет сохраняться значение</param>
    /// <typeparam name="TValue">Тип сохраняемого значения</typeparam>
    /// <remarks>
    /// Зачем нужен exposeValueAs: метод не всегда может самостоятельно понять каким образом надо сохранять переданный объект.
    /// Если Value еще можно определить автоматически, то IExposable и IRefExposable могут сохраняться разными способами,
    /// в зависимости от ситуации.
    /// </remarks>
    void Expose_List<TValue>(ref List<TValue> list, string label, ExposeAs exposeValueAs = ExposeAs.Undefined, object[] ctorArgs = null);

    /// <summary>
    /// Сохраняет или читает словарь
    /// </summary>
    /// <param name="dictionary">Словарь</param>
    /// <param name="label">Имя значения в контейнере сохранения. Должно быть уникальным в пределах текущего IExposable</param>
    /// <param name="exposeKeyAs">Каким образом будет сохраняться ключ в словаре</param>
    /// <param name="exposeValueAs">Каким образом будет сохраняться значение словаря</param>
    /// <typeparam name="TKey">Тип сохраняемого ключа</typeparam>
    /// <typeparam name="TValue">Тип сохраняемого значения</typeparam>
    /// /// <remarks>
    /// Зачем нужны exposeKeyAs и exposeValueAs: метод не всегда может самостоятельно понять каким образом надо сохранять переданный объект.
    /// Если Value еще можно определить автоматически, то IExposable и IRefExposable могут сохраняться разными способами,
    /// в зависимости от ситуации.
    /// </remarks>
    void Expose_Dictionary<TKey, TValue>(ref Dictionary<TKey, TValue> dictionary, string label, ExposeAs exposeKeyAs = ExposeAs.Undefined, ExposeAs exposeValueAs = ExposeAs.Undefined);
}