using System.Linq.Expressions;
using System.Reflection;

namespace KludgeBox.Reflection.Access;

public class FieldAccessor : IMemberAccessor
{
    public MemberInfo Member => _field;
    public Type ValueType { get; }
    public IReadOnlyList<Attribute> Attributes { get; }
    public bool IsPublic => _field.IsPublic;

    private FieldInfo _field;

    private Func<object, object> _getter;
    private Action<object, object> _setter;

    public FieldAccessor(FieldInfo field)
    {
        _field = field;
        ValueType = field.FieldType;
        Attributes = field.GetCustomAttributes().ToList();

        try
        {
            _getter = CreateGetter(field);
            if (!field.IsInitOnly)
            {
                _setter = CreateSetter(field);
            }
            else
            {
                _setter = CreateInvalidSetter(field);
            }
        }
        catch(Exception e)
        {
            throw new Exception($"Failed to create accessor for member {field.DeclaringType?.FullName}.{field.Name}", e);
        }
    }
    
    public bool TryGetAttribute(Type attributeType, out Attribute attribute)
    {
        attribute = Attributes.FirstOrDefault(a => a.GetType().IsAssignableTo(attributeType));
        return attribute is not null;
    }

    public bool TryGetAttribute<TAttribute>(out TAttribute attribute) where TAttribute : Attribute
    {
        var found = TryGetAttribute(typeof(TAttribute), out Attribute foundAttribute);
        attribute = foundAttribute as TAttribute;
        return found;
    }

    private static Func<object, object> CreateGetter(FieldInfo field)
    {
        var targetParam = Expression.Parameter(typeof(object), "target");

        // (TTarget)target
        var castTarget = Expression.Convert(targetParam, field.DeclaringType);

        // target.Field
        var fieldAccess = Expression.Field(castTarget, field);

        // (object)target.Field
        var castResult = Expression.Convert(fieldAccess, typeof(object));

        return Expression.Lambda<Func<object, object>>(castResult, targetParam).Compile();
    }

    private static Action<object, object> CreateSetter(FieldInfo field)
    {
        var targetParam = Expression.Parameter(typeof(object), "target");
        var valueParam = Expression.Parameter(typeof(object), "value");

        var castTarget = Expression.Convert(targetParam, field.DeclaringType);
        var castValue = Expression.Convert(valueParam, field.FieldType);

        var fieldAccess = Expression.Field(castTarget, field);
        var assign = Expression.Assign(fieldAccess, castValue);

        return Expression.Lambda<Action<object, object>>(assign, targetParam, valueParam).Compile();
    }

    private static Action<object, object> CreateInvalidSetter(FieldInfo field)
    {
        return (_, _) => throw new AccessViolationException($"Attempt to write to readonly field {field.DeclaringType?.FullName}.{field.Name}");
    }
    public object GetValue(object target) => _getter(target);
    public void SetValue(object target, object value) => _setter(target, value);
}