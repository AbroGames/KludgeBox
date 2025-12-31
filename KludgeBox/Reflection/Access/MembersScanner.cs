using System.Reflection;

namespace KludgeBox.Reflection.Access;

public class MembersScanner
{
    private Dictionary<Type, List<IMemberAccessor>> _memberAccessorsCache = new();
    
    /// <summary>
    /// Scans members of the type and returns them as <see cref="IMemberAccessor"/>. Also caches scanning results.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// TODO: This potentially may result in uncontrollable cache growth. Probably need to make cache as a separate service, or add more control over caching.
    /// TODO: Also, 'ScanMembers' name is kinda misleading.
    public IReadOnlyList<IMemberAccessor> ScanMembers(Type type)
    {
        if (_memberAccessorsCache.TryGetValue(type, out var cachedAccessors))
        {
            return cachedAccessors;
        }
        
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        var fields = type.GetFields(flags).Where(field => !field.IsPrivate);
        var properties = type.GetProperties(flags)
            .Where(IsAccessibleProperty)
            .Where(property => !IsPrivateProperty(property))
            .Select(AsAccessibleProperty);
        
        var privateMembers = GetPrivateMembers(type);
        
        var members = new List<MemberInfo>();
        members.AddRange(fields);
        members.AddRange(properties);
        members.AddRange(privateMembers);

        var accessors = members
            .Select(IMemberAccessor (member) => new MemberAccessor(member))
            .ToList();
        
        _memberAccessorsCache[type] = accessors;
        
        return accessors;
    }

    private static List<MemberInfo> GetPrivateMembers(Type type)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        var members = new List<MemberInfo>();
        
        var fields = type.GetFields(flags);
        var properties = type.GetProperties(flags)
            .Where(IsAccessibleProperty)
            .Select(AsAccessibleProperty);
        
        members.AddRange(fields.Where(field => field.IsPrivate));
        members.AddRange(properties.Where(IsPrivateProperty));
        
        var baseType = GetBaseType(type);
        if (baseType is not null)
        {
            members.AddRange(GetPrivateMembers(baseType));
        }
        
        return members;
    }

    private static Type GetBaseType(Type type)
    {
        if (type == typeof(Object))
        {
            return null;
        }

        return type.BaseType;
    }

    private static bool IsPrivateProperty(PropertyInfo property)
    {
        var getMethod = property.GetGetMethod(true);
        var setMethod = property.GetSetMethod(true);
        
        var isGetterPrivate = getMethod?.IsPrivate ?? true;
        var isSetterPrivate = setMethod?.IsPrivate ?? true;
        
        return isGetterPrivate && isSetterPrivate;
    }

    private static bool IsAccessibleProperty(PropertyInfo property)
    {
        var sourceProperty = AsAccessibleProperty(property);
        
        var getMethod = sourceProperty.GetGetMethod(true);
        var setMethod = sourceProperty.GetSetMethod(true);
        
        var isAccessible = getMethod is not null && setMethod is not null;
        return isAccessible;
    }

    private static PropertyInfo AsAccessibleProperty(PropertyInfo property)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var declaringType = property.DeclaringType;
        var sourceProperty = declaringType.GetProperty(property.Name, flags);
        
        return sourceProperty;
    }
}