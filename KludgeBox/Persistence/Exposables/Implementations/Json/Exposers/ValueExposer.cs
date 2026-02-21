namespace KludgeBox.Persistence.Exposables.Json;

public partial class JsonPersistenceContainer
{
    
    public void Expose_Value<TValue>(ref TValue value, string label, TValue defaultValue = default)
    {
        if (State is ContainerState.Saving)
        {
            if (value is null)
            {
                SaveAsNull(label);
                return;
            }
            
            if (Serializers.CanSerialize(typeof(TValue)))
            {
                _currentNode[label] = Serializers.Serialize(value);
                return;
            }
        }

        if (State is ContainerState.Loading)
        {
            if (IsNull(label) || !Serializers.CanSerialize(typeof(TValue)))
            {
                value = defaultValue;
                return;
            }

            value = Serializers.Deserialize<TValue>(_currentNode[label]?.ToString());
        }
    }
}