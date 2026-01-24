namespace Persistence.Json;

public partial class JsonPersistenceContainer
{
    
    public void Expose_Deep<TValue>(ref TValue value, string label, object[] ctorArgs = null) where TValue : class, IExposable
    {
        if (State is ContainerState.ScanReferences)
        {
            if (value is null) return;
            
            value.ExposeData(this);
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
                WriteDeep(value);
                ExitNode();
                return;
            }
        }
        
        if (State is ContainerState.Loading)
        {
            if (IsNull(label))
            {
                value = null;
                return;
            }

            if (EnterNode(label))
            {
                value = (TValue)RestoreCurrentExposable(ctorArgs);
                ExitNode();
                return;
            }
        }

        if (State is ContainerState.RefsResolving)
        {
            value?.ExposeData(this);
        }
    }

    private void WriteDeep(IExposable value)
    {
        WriteMeta(TypeMetaPropertyName, value.GetType().FullName);
        value.ExposeData(this);
    }
    
    private IExposable RestoreCurrentExposable(object[]  ctorArgs)
    {
        var typeName = ReadMeta(TypeMetaPropertyName);
        var exposable = (IExposable)ExposableReflection.GetTypeByName(typeName).GetInstanceOfType(ctorArgs);
        exposable.ExposeData(this);
        
        return exposable;
    }
}

