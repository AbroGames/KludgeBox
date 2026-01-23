using System.Reflection;

namespace Persistence;

public static class ExposableReflection
{
    private static Dictionary<string, Type> _nameToType = new();
    private static Dictionary<string, Type> _fullNameToType = new();

    public static Type GetTypeByName(string typeName)
    {
        if (_fullNameToType.TryGetValue(typeName, out Type result))
        {
            return result;
        }

        if (_nameToType.TryGetValue(typeName, out result))
        {
            return result;
        }
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types = assemblies.SelectMany(a => a.GetTypes());
        var type =  types.FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);

        if (type is not null)
        {
            _fullNameToType.Add(type.FullName, type);
            _nameToType.Add(type.Name, type);
        }
        
        return type;
    }

    public static object GetInstanceOfType(this Type type, object[] ctorArgs = null)
    {
        // Next bug: this thing can't find the default constructor
        object result = null;
        result = Activator.CreateInstance(type, BindingFlags.Public | BindingFlags.NonPublic, ctorArgs);

        return result;
    }
}