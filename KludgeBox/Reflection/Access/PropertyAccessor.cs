using System.Reflection;
using Godot;
using KludgeBox.Logging;
using Serilog;
using Expression = System.Linq.Expressions.Expression;

namespace KludgeBox.Reflection.Access;

public class PropertyAccessor : IMemberAccessor
{
	private static ILogger _log = LogFactory.GetForStatic<PropertyAccessor>();
	public MemberInfo Member => _property;
	public Type ValueType { get; }
	public IReadOnlyList<Attribute> Attributes { get; }
	public bool IsPublic => (_property.GetMethod?.IsPublic ?? false) || (_property.SetMethod?.IsPublic ?? false);

	private PropertyInfo _property;

	// Делегаты универсального вида: object target, object value
	private Func<object, object> _getter;
	private Action<object, object> _setter;

	public PropertyAccessor(PropertyInfo property)
	{
		_log.Debug("Creating accessor for property {Property}", property.Name);
		/*if (property.GetGetMethod(true) is null)
			throw new ArgumentException($"Property {property.Name} does not have a getter.");

		if (property.GetSetMethod(true) is null)
			throw new ArgumentException($"Property {property.Name} does not have a setter.");*/
		if (!IsAccessibleProperty(property))
		{
			throw new ArgumentException($"Property {property.Name} are not fully accessible (has no setter or getter).");
		}
		
		_property = property;
		Attributes = _property.GetCustomAttributes().ToList();
		ValueType = _property.PropertyType;

		// ⚠ Создание делегатов через Expression (чтобы избежать boxing проблем и ускорить доступ)
		var accessibleProperty = AsAccessibleProperty(property);
		_getter = CreateGetter(accessibleProperty);
		_setter = CreateSetter(accessibleProperty);
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
	private static Func<object, object> CreateGetter(PropertyInfo property)
	{
		var objParam = Expression.Parameter(typeof(object), "target");

		// (TTarget)target
		var targetExpr = Expression.Convert(objParam, property.DeclaringType);

		// target.Property
		var propertyAccess = Expression.Property(targetExpr, property);

		// (object)target.Property
		var convertResult = Expression.Convert(propertyAccess, typeof(object));

		return Expression.Lambda<Func<object, object>>(convertResult, objParam).Compile();
	}

	private static Action<object, object> CreateSetter(PropertyInfo property)
	{
		var objParam = Expression.Parameter(typeof(object), "target");
		var valueParam = Expression.Parameter(typeof(object), "value");

		var targetExpr = Expression.Convert(objParam, property.DeclaringType);
		var valueExpr = Expression.Convert(valueParam, property.PropertyType);

		var propertyAccess = Expression.Property(targetExpr, property);
		var assignExpr = Expression.Assign(propertyAccess, valueExpr);

		return Expression.Lambda<Action<object, object>>(assignExpr, objParam, valueParam).Compile();
	}

	public object GetValue(object target)
	{
		// ✅ Использование скомпилированного геттера
		return _getter(target);
	}

	public void SetValue(object target, object value)
	{
		// ✅ Использование скомпилированного сеттера
		_setter(target, value);
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

