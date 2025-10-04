using System.Reflection;

namespace KludgeBox.Reflection.Access;

public class MembersScanner
{
    public List<IMemberAccessor> ScanMembers(Type type)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        var fields = type.GetFields(flags);
        var properties = type.GetProperties(flags).Where(p => p.CanWrite);
        
        var privateMembers = GetPrivateMembers(type);
        
        var members = new List<MemberInfo>();
        members.AddRange(fields);
        members.AddRange(properties);
        members.AddRange(privateMembers);

        var accessors = members
            .Select(IMemberAccessor (member) => new MemberAccessor(member))
            .ToList();
        
        return accessors;
    }

    private static List<MemberInfo> GetPrivateMembers(Type type)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        var members = new List<MemberInfo>();
        
        var fields = type.GetFields(flags);
        var properties = type.GetProperties(flags).Where(p => p.CanWrite);
        
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
}