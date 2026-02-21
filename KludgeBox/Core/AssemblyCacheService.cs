using System.Reflection;

namespace KludgeBox.Core;

public class AssemblyCacheService
{
    private readonly Dictionary<Assembly, IReadOnlyList<Type>> _typesByAssembly = new();
    
    public void AddAssembly(Assembly assembly)
    {
        _typesByAssembly.TryAdd(assembly, assembly.GetTypes().ToList());
    }
    
    public void AddAssembly(IEnumerable<Assembly> assemblies)
    {
        foreach (Assembly assembly in assemblies)
        {
            AddAssembly(assembly);
        }
    }

    public IReadOnlyList<Type> GetTypes(Assembly assembly)
    {
        return _typesByAssembly.GetValueOrDefault(assembly, []);
    }
}