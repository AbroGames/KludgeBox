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
        object result = null;
        
        // Next bug: this thing can't find the default constructor
        // TODO: save info about this behavior somewhere
        // result = Activator.CreateInstance(type, ctorArgs);
        
        // Ok, it looks like some serious shit, but now it at least works
        result = Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            args: ctorArgs,
            culture: null
        );

        return result;
    }
}