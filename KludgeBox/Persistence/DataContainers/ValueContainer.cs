namespace KludgeBox.Persistence.DataContainers;

public class ValueContainer
{
    public Type ValueType { get; private set; }
    public object Value { get; private set; }

    public ValueContainer(Type valueType, object value)
    {
        ValueType = valueType;
        Value = value;
    }
    
    public static ValueContainer FromValue<T>(T value)
    {
        return new ValueContainer(typeof(T), value);
    }
}