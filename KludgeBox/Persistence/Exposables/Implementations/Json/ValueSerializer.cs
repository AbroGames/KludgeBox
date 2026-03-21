namespace KludgeBox.Persistence.Exposables.Json;

public delegate object ParseDelegate(string text);
public delegate string SerializeDelegate(object value);

public delegate TValue ParseDelegate<out TValue>(string text);
public delegate string SerializeDelegate<in TValue>(TValue value);

public class ValueSerializer
{
    protected ParseDelegate _parser;
    protected SerializeDelegate _serializer;

    public ValueSerializer(ParseDelegate parser, SerializeDelegate serializer)
    {
        _parser = parser;
        _serializer = serializer;
    }
    protected ValueSerializer() { }
        
    public string Serialize(object value)
    {
        return _serializer.Invoke(value);
    }

    public object Deserialize(string text)
    {
        return _parser.Invoke(text);
    }
}

public class ValueSerializer<TValue> : ValueSerializer
{
    public ValueSerializer(ParseDelegate<TValue> parser, SerializeDelegate<TValue> serializer)
    {
        _parser = text => parser(text);
        _serializer = value => serializer((TValue)value); 
    }
}
