using System.Globalization;
using Godot;

namespace Persistence.Json;


public class Serializers
{
    private Dictionary<Type, ValueSerializer> _valueSerializers = new();

    public bool CanSerialize(Type type)
    {
        return _valueSerializers.ContainsKey(type);
    }

    public string Serialize(object value)
    {
        var type = value.GetType();
        var serializer = _valueSerializers[type];
        var result = serializer.Serialize(value);
        
        return result;
    }

    public TValue Deserialize<TValue>(string value)
    {
        var result = Deserialize(typeof(TValue), value);
        return (TValue)result;
    }

    public object Deserialize(Type type, string value)
    {
        var serializer = _valueSerializers[type];
        var result = serializer.Deserialize(value);
        
        return result;
    }

    public void Register<TType>(ValueSerializer<TType> serializer)
    {
        Register(typeof(TType), serializer);
    }

    public void Register(Type type, ValueSerializer serializer)
    {
        _valueSerializers[type] = serializer;
    }
    
    public void RegisterDefaultSerializers()
    {
        Register<string>(new (
            parser: text => text,
            serializer: value => value
            ));
        
        Register<bool>(new (
            parser: text => Boolean.Parse(text),
            serializer: value => value.ToString(CultureInfo.InvariantCulture)
        ));
        
        Register<byte>(new (
            parser: text => Byte.Parse(text, CultureInfo.InvariantCulture),
            serializer: value => value.ToString(CultureInfo.InvariantCulture)
        ));
        
        Register<sbyte>(new (
            parser: text => SByte.Parse(text, CultureInfo.InvariantCulture),
            serializer: value => value.ToString(CultureInfo.InvariantCulture)
        ));
        
        Register<short>(new (
            parser: text => Int16.Parse(text, CultureInfo.InvariantCulture),
            serializer: value => value.ToString(CultureInfo.InvariantCulture)
        ));
        
        Register<ushort>(new (
            parser: text => UInt16.Parse(text, CultureInfo.InvariantCulture),
            serializer: value => value.ToString(CultureInfo.InvariantCulture)
        ));
        
        Register<int>(new (
            parser: text => Int32.Parse(text, CultureInfo.InvariantCulture),
            serializer: value => value.ToString(CultureInfo.InvariantCulture)
        ));
        
        Register<uint>(new (
            parser: text => UInt32.Parse(text, CultureInfo.InvariantCulture),
            serializer: value => value.ToString(CultureInfo.InvariantCulture)
        ));
        
        Register<long>(new (
            parser: text => Int64.Parse(text, CultureInfo.InvariantCulture),
            serializer: value => value.ToString(CultureInfo.InvariantCulture)
        ));
        
        Register<ulong>(new (
            parser: text => UInt64.Parse(text, CultureInfo.InvariantCulture),
            serializer: value => value.ToString(CultureInfo.InvariantCulture)
        ));
        
        Register<Half>(new (
            parser: text => Half.Parse(text, CultureInfo.InvariantCulture),
            serializer: value => value.ToString(CultureInfo.InvariantCulture)
        ));
        
        Register<float>(new (
            parser: text => Single.Parse(text, CultureInfo.InvariantCulture),
            serializer: value => value.ToString(CultureInfo.InvariantCulture)
        ));
        
        Register<double>(new (
            parser: text => Double.Parse(text, CultureInfo.InvariantCulture),
            serializer: value => value.ToString(CultureInfo.InvariantCulture)
        ));
        
        Register<Color>(new (
            parser: text => Color.FromHtml(text),
            serializer: value => value.ToHtml()
        ));
        
        Register<Vector2>(new (
            parser: text => (Vector2)GD.StrToVar(text),
            serializer: value => GD.VarToStr(value)
        ));
        
        Register<Vector2I>(new (
            parser: text => (Vector2I)GD.StrToVar(text),
            serializer: value => GD.VarToStr(value)
        ));
        
        Register<Vector3>(new (
            parser: text => (Vector3)GD.StrToVar(text),
            serializer: value => GD.VarToStr(value)
        ));
        
        Register<Vector3I>(new (
            parser: text => (Vector3I)GD.StrToVar(text),
            serializer: value => GD.VarToStr(value)
        ));
        
        Register<Vector4>(new (
            parser: text => (Vector4)GD.StrToVar(text),
            serializer: value => GD.VarToStr(value)
        ));
        
        Register<Vector4I>(new (
            parser: text => (Vector4I)GD.StrToVar(text),
            serializer: value => GD.VarToStr(value)
        ));
        
        Register<Quaternion>(new (
            parser: text => (Quaternion)GD.StrToVar(text),
            serializer: value => GD.VarToStr(value)
        ));
        
        Register<Basis>(new (
            parser: text => (Basis)GD.StrToVar(text),
            serializer: value => GD.VarToStr(value)
        ));
        
        Register<Transform2D>(new (
            parser: text => (Transform2D)GD.StrToVar(text),
            serializer: value => GD.VarToStr(value)
        ));
        
        Register<Transform3D>(new (
            parser: text => (Transform3D)GD.StrToVar(text),
            serializer: value => GD.VarToStr(value)
        ));
        
        Register<Aabb>(new (
            parser: text => (Aabb)GD.StrToVar(text),
            serializer: value => GD.VarToStr(value)
        ));
        
        Register<Variant>(new (
            parser: text => GD.StrToVar(text),
            serializer: value => GD.VarToStr(value)
        ));
    }
}
