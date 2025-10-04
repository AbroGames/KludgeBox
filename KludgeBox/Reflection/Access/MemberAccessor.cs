using System.Reflection;

namespace KludgeBox.Reflection.Access;

public class MemberAccessor : IMemberAccessor
{
    public MemberInfo Member => _wrappedAccessor.Member;
    public Type ValueType => _wrappedAccessor.ValueType;
    public IReadOnlyList<Attribute> Attributes => _wrappedAccessor.Attributes;
    public bool IsPublic => _wrappedAccessor.IsPublic;

    private readonly IMemberAccessor _wrappedAccessor;

    public MemberAccessor(MemberInfo member)
    {
        if (member is FieldInfo field)
        {
            _wrappedAccessor = new FieldAccessor(field);
            return;
        }

        if (member is PropertyInfo property)
        {
            _wrappedAccessor = new PropertyAccessor(property);
            return;
        }
        
        throw new NotSupportedException($"Member accessor {member} of type {member.GetType()} is not supported");
    }
    public MemberAccessor(PropertyInfo propertyInfo)
    {
        _wrappedAccessor = new PropertyAccessor(propertyInfo);
    }

    public MemberAccessor(FieldInfo fieldInfo)
    {
        _wrappedAccessor = new FieldAccessor(fieldInfo);
    }
    
    public bool TryGetAttribute(Type attributeType, out Attribute attribute)
    {
        return _wrappedAccessor.TryGetAttribute(attributeType, out attribute);
    }

    public bool TryGetAttribute<TAttribute>(out TAttribute attribute) where TAttribute : Attribute
    {
        return _wrappedAccessor.TryGetAttribute<TAttribute>(out attribute);
    }

    public object GetValue(object target)
    {
        return _wrappedAccessor.GetValue(target);
    }

    public void SetValue(object target, object value)
    {
        _wrappedAccessor.SetValue(target, value);
    }
}