using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace KludgeBox.Core;

public class TypesMappingService
{
    /// <summary>
    /// The mapped types in id order.
    /// </summary>
    public IReadOnlyList<Type> Types => _typeById;
    
    // The id is the index: ids are dense, 0..Count-1.
    private readonly List<Type> _typeById = new();
    private readonly Dictionary<Type, int> _idByType = new();

    [Logger] private ILogger _log;
    
    public TypesMappingService()
    {
        Di.Process(this);
    }

    public void SetTypes(List<Type> types)
    {
        // Find and sort all types (except abstract and interface)
        List<Type> filteredTypes = types
            .Where(t => !t.IsInterface)
            .Where(t => !t.IsAbstract)
            // Ordinal: a culture-aware comparison could assign different ids on machines with different locales.
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

        _typeById.Clear();
        _idByType.Clear();
        _typeById.AddRange(filteredTypes);
        for (int i = 0; i < filteredTypes.Count; i++)
        {
            _idByType[filteredTypes[i]] = i;
        }

        _log.Information("Added {count} types.", _typeById.Count);
    }

    public int GetIdByType(Type type)
    {
        if (_idByType.TryGetValue(type, out int id))
        {
            return id;
        }
        throw new KeyNotFoundException($"Type {type.Name} is not found in {nameof(TypesMappingService)}");
    }
    
    public int GetIdByType<T>()
    {
        return GetIdByType(typeof(T));
    }

    public Type GetTypeById(int id)
    {
        if (id >= 0 && id < _typeById.Count)
        {
            return _typeById[id];
        }
        throw new KeyNotFoundException($"Id {id} is not found in {nameof(TypesMappingService)}.");
    }
}