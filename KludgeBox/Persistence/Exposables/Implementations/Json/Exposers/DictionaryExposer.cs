namespace Persistence.Json;

public partial class JsonPersistenceContainer
{
    private const string KeysListLabel = "keys";
    private const string ValuesListLabel = "values";
    
    public void Expose_Dictionary<TKey, TValue>(ref Dictionary<TKey, TValue> dictionary, string label,
        ExposeAs exposeKeyAs = ExposeAs.Undefined,
        ExposeAs exposeValueAs = ExposeAs.Undefined)
    {
        if (exposeKeyAs == ExposeAs.Undefined) exposeKeyAs = ResolveExpositionType(typeof(TKey));
        
        if (exposeValueAs == ExposeAs.Undefined)
            exposeValueAs = ResolveExpositionType(typeof(TValue));
        
        if (State is ContainerState.ScanReferences)
        {
            if (dictionary is null) return;
            
            var (keys, values) = SplitToLists(dictionary);
            Expose_List(ref keys, "ref_keys", exposeKeyAs);
            Expose_List(ref values, "ref_values", exposeValueAs);
        }
        
        if (State is ContainerState.Saving)
        {
            if (dictionary is null)
            {
                SaveAsNull(label);
                return;
            }

            if (EnterNode(label))
            {
                var (keys, values) = SplitToLists(dictionary);
                Expose_List(ref keys, KeysListLabel, exposeKeyAs);
                Expose_List(ref values, ValuesListLabel, exposeValueAs);
                ExitNode();
            }
        }

        if (State is ContainerState.Loading)
        {
            if (IsNull(label))
            {
                dictionary = null;
                return;
            }

            if (EnterNode(label))
            {
                List<TKey> keys = null;
                List<TValue> values = null;
                
                Expose_List(ref keys, KeysListLabel, exposeKeyAs);
                Expose_List(ref values, ValuesListLabel, exposeValueAs);
                
                dictionary = MergeToDictionary(keys, values);
                
                ExitNode();
            }
        }

        if (State is ContainerState.RefsResolving)
        {
            if (exposeKeyAs is not ExposeAs.Reference)
            {
                if (dictionary is null) return;
            }
            else
            {
                dictionary ??= dictionary = new Dictionary<TKey, TValue>();
            }
            
            if (EnterNode(label))
            {
                var (keys, values) = SplitToLists(dictionary);

                Expose_List(ref keys, KeysListLabel, exposeKeyAs);
                Expose_List(ref values, ValuesListLabel, exposeValueAs);

                dictionary = MergeToDictionary(keys, values);
                ExitNode();
            }
        }
    }

    private (List<TKey> keys, List<TValue> values) SplitToLists<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
    {
        var count = dictionary.Count;
        List<TKey> keys = new List<TKey>(count);
        List<TValue> values = new List<TValue>(count);
        
        keys.AddRange(dictionary.Keys);
        values.AddRange(dictionary.Values);
        
        return (keys, values);
    }

    private Dictionary<TKey, TValue> MergeToDictionary<TKey, TValue>(List<TKey> keys, List<TValue> values)
    {
        if (keys.Count != values.Count)
        {
            return null;
            // throw new ArgumentException(
            //     $"Unable to merge lists to Dictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>. Keys count: {keys.Count}, values count: {values.Count}");
        }

        var dict = new Dictionary<TKey, TValue>(keys.Count);

        for (int i = 0; i < keys.Count; i++)
        {
            dict[keys[i]] = values[i];
        }
        
        return  dict;
    }
}