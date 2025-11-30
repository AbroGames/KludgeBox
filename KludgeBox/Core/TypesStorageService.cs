using System.Reflection;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace KludgeBox.Core;

public class TypesStorageService
{
    private readonly Dictionary<int, Type> _typeById = new();
    private readonly Dictionary<Type, int> _idByType = new();

    [Logger] private ILogger _log;
    
    public TypesStorageService()
    {
        Di.Process(this);
    }

    public void AddTypes(List<Type> types)
    {
        // Find and sort all types (except abstract and interface)
        List<Type> filteredTypes = types
            .Where(t => !t.IsInterface)
            .Where(t => !t.IsAbstract)
            .OrderBy(t => t.FullName)
            .ToList();

        for (int i = 0; i < filteredTypes.Count; i++)
        {
            Type type = filteredTypes[i];
            
            _typeById[i] = type;
            _idByType[type] = i;
        }

        _log.Information("Add {count} types.", _typeById.Count);
    }

    public int GetId(Type type)
    {
        if (_idByType.TryGetValue(type, out int id))
        {
            return id;
        }
        throw new KeyNotFoundException($"Type {type.Name} is not found in {nameof(TypesStorageService)}");
    }
    
    public int GetId<T>()
    {
        return GetId(typeof(T));
    }

    public Type GetType(int id)
    {
        if (_typeById.TryGetValue(id, out Type type))
        {
            return type;
        }
        throw new KeyNotFoundException($"Id {id} is not found in {nameof(TypesStorageService)}.");
    }
}