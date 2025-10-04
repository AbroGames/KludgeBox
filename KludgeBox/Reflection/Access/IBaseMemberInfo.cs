using System.Reflection;

namespace KludgeBox.Reflection.Access;

public interface IBaseMemberInfo
{
    MemberInfo Member { get; }
    Type ValueType { get; }
    IReadOnlyList<Attribute> Attributes { get; }
    bool IsPublic { get; }

    bool HasAttribute(Type attributeType)
    {
        return Attributes.Any(attr => attr.GetType() == attributeType);
    }

    bool HasAttribute<TAttribute>() where TAttribute : Attribute
    {
        return HasAttribute(typeof(TAttribute));
    }
    
    bool TryGetAttribute(Type attributeType, out Attribute attribute);
    bool TryGetAttribute<TAttribute>(out TAttribute attribute) where TAttribute : Attribute;
}