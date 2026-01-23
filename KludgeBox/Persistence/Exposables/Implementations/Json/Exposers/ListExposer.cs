using System.Text.Json.Nodes;

namespace Persistence.Json;

public partial class JsonPersistenceContainer
{
    public void Expose_List<TValue>(ref List<TValue> list, string label, ExposeAs exposeValueAs = ExposeAs.Undefined, object[] ctorArgs = null)
    {
        var elementType = typeof(TValue);
        if (exposeValueAs is ExposeAs.Undefined)
        {
            exposeValueAs = ResolveExpositionType(elementType);
        }

        if (exposeValueAs is ExposeAs.Undefined)
        {
            throw new Exception("exposeValueAs is Undefined");
        }
        
        if (State is ContainerState.ScanReferences)
        {
            foreach (var item in list)
            {
                if (item is IRefExposable refExposable)
                {
                    Expose_ReferenceAnonymous(ref refExposable);
                }
            }
            return;
        }

        if (State is ContainerState.Saving)
        {
            if (list is null)
            {
                SaveAsNull(label);
                return;
            }

            var array = new JsonArray();

            foreach (var value in list)
            {
                WriteElementToArray(value, exposeValueAs, array);
            }
            return;
        }

        if (State is ContainerState.Loading)
        {
            if (IsNull(label))
            {
                list = null;
                return;
            }
            
            var array = _currentNode[label] as JsonArray;
            list = new List<TValue>(array.Count);
            if (exposeValueAs is not ExposeAs.Reference)
            {
                foreach (var item in array)
                {
                    ReadElementToList(item, exposeValueAs, list, ctorArgs);
                }
            }
            
            return;
        }

        if (State is ContainerState.RefsResolving)
        {
            if (list is null)
                return;
            
            var array = _currentNode[label] as JsonArray;
            
            if (exposeValueAs is ExposeAs.Reference)
            {
                foreach (var item in array)
                {
                    if (item is JsonObject jsonObject)
                    {
                        var preservedNode = _currentNode;
                        _currentNode = jsonObject;
                        var refId = ReadMeta(RefIdMetaPropertyName);
                        var restoredRefExposable = _knownReferences[refId];
                        list.Add((TValue)restoredRefExposable);
                        _currentNode = preservedNode;
                    }
                }
                
                return;
            }

            if (typeof(TValue).IsAssignableTo(typeof(IExposable)))
            {
                foreach (var item in list)
                {
                    var exposable = (IExposable)item;
                    exposable.ExposeData(this);
                }
            }
        }
    }

    private void ReadElementToList<TValue>(JsonNode value, ExposeAs exposeAs, List<TValue> list, object[] ctorArgs)
    {
        var valueType = typeof(TValue);
        if (value is JsonObject jsonObject)
        {
            if (IsNull(jsonObject))
            {
                list.Add(default);
                return;
            }
            
            if (exposeAs is ExposeAs.Deep)
            {
                var preservedNode = _currentNode;
                _currentNode = jsonObject;
                var restoredExposable = RestoreCurrentExposable(ctorArgs);
                list.Add((TValue)restoredExposable);
                _currentNode = preservedNode;
                return;
            }
        }
        
        if (exposeAs is ExposeAs.Value)
        {
            if (Serializers.CanSerialize(valueType))
            {
                list.Add(Serializers.Deserialize<TValue>(value.ToString()));
                return;
            }
        }
    }

    private void WriteElementToArray<TValue>(TValue value, ExposeAs exposeValueAs, JsonArray array)
    {
        var preservedNode = _currentNode;
        if (value is null)
        {
            var newNode = new JsonObject();
            _currentNode = newNode;
            WriteMeta(IsNullMetaPropertyName, "true");
            array.Add(newNode);
            _currentNode = preservedNode;
            return;
        }

        if (exposeValueAs is ExposeAs.Value)
        {
            if (Serializers.CanSerialize(value.GetType()))
            {
                var serializedValue = Serializers.Serialize(value);
                array.Add(serializedValue);
                return;
            }
            else
            {
                throw new Exception($"Attempt to serialize value of type {value.GetType()} which is not supported");
            }
        }

        if (exposeValueAs is ExposeAs.Deep)
        {
            if (value is not IExposable exposable)
                throw new Exception($"Attempt to expose value of type {value.GetType()} which is not IExposable");
            
            var newNode = new JsonObject();
            _currentNode = newNode;
            exposable.ExposeData(this);
            array.Add(newNode);
            _currentNode = preservedNode;
            return;
        }

        if (exposeValueAs is ExposeAs.Reference)
        {
            if (value is not IRefExposable refExposable)
                throw new Exception($"Attempt to expose as reference value of type {value.GetType()} which is not IRefExposable");
            
            var newNode = new JsonObject();
            _currentNode = newNode;
            WriteMeta(RefIdMetaPropertyName, refExposable.GetUniqueId());
            array.Add(newNode);
            _currentNode = preservedNode;
            return;
        }
    }
}