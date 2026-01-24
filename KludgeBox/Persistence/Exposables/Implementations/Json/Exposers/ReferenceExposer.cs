namespace Persistence.Json;

public partial class JsonPersistenceContainer
{
    private Dictionary<string, IRefExposable> _knownReferences = new ();
    private Dictionary<IRefExposable, string> _refIds = new ();
    private const string RefIdMetaPropertyName = "referenceId";
    
    public void Expose_Reference<TValue>(ref TValue value, string label, object[] ctorArgs = null) where TValue : class, IRefExposable
    {
        if (State is ContainerState.ScanReferences)
        {
            if (value is null) return;
            if (RegisterReferencable(value)) value.ExposeData(this);
            return;
        }

        if (State is ContainerState.Saving)
        {
            if (value is null)
            {
                SaveAsNull(label);
                return;
            }

            if (EnterNode(label))
            {
                WriteMeta(RefIdMetaPropertyName, value.GetUniqueId());
                ExitNode();
                return;
            } 
        }

        if (State is ContainerState.Loading)
        {
            if (value is null) return;

            if (!_refIds.ContainsKey(value))
            {
                if (EnterNode(label))
                {
                    var refId = ReadMeta(RefIdMetaPropertyName);
                    _refIds[value] = refId;
                    ExitNode();
                    return;
                }
            }
        }

        if (State is ContainerState.RefsResolving)
        {
            if (EnterNode(label))
            {
                var refId = ReadMeta(RefIdMetaPropertyName);
                ExitNode();
                value = (TValue)_knownReferences[refId];
            }
        }
    }

    private void Expose_ReferenceAnonymous<TValue>(ref TValue value,  object[] ctorArgs = null) where TValue : class, IRefExposable
    {
        Expose_Reference(ref value, "no_label_ref", ctorArgs);
    }


    private bool RegisterReferencable(IRefExposable refExposable)
    {
        var id = refExposable.GetUniqueId();
        if (!_knownReferences.ContainsKey(id) || !_refIds.ContainsKey(refExposable))
        {
            _knownReferences[id] = refExposable;
            _refIds[refExposable] = id;
            return true;
        }
        
        return false;
    }

    private void ResolveReferenceInstances()
    {
        if (State is ContainerState.ScanReferences)
        {
            foreach (var (label, value) in _knownReferences)
            {
                if (EnterNode(label))
                {
                    value.ExposeData(this);
                    ExitNode();
                }
            }
            return;
        }
        
        // Last fix: no type metadata was stored during refs scanning
        if (State is ContainerState.Saving)
        {
            foreach (var (label, value) in _knownReferences)
            {
                var refExposable = value;
                Expose_Deep(ref refExposable, label);
            }
            return;
        } 
        
        if (State is ContainerState.Loading)
        {
            foreach (var (label, node) in _currentNode)
            {
                if (EnterNode(label))
                {
                    var exposable = RestoreCurrentExposable(null);
                    exposable.ExposeData(this);
                    var refExposable = exposable as IRefExposable;
                    _knownReferences[label] = refExposable;
                    _refIds[refExposable] = label;
                    ExitNode();
                }
            }
        }
    }

}