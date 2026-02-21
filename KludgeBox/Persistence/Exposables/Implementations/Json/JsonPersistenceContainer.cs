using System.Text.Json;
using System.Text.Json.Nodes;

namespace KludgeBox.Persistence.Exposables.Json;

public partial class JsonPersistenceContainer : IPersistenceContainer, IPersistenceEntryPoint
{
    public ContainerState State { get; protected set; } = ContainerState.Idle;
    public Serializers Serializers { get; protected set; }

    private const string RefsObjectName = "refs";
    private const string RootObjectName = "root";
    
    private const string MetaObjectName = "__meta__";
    private const string IsNullMetaPropertyName  = "isNull";
    private const string TypeMetaPropertyName = "exposableType";
    
    private JsonObject _mainContainerObject; // Корневой объект всего сохранения
    private JsonObject _rootObject; // Сохраняемый/загружаемый объект
    private JsonObject _refsContainerObject; // Хранилище ссылочных объектов
    private JsonObject _currentNode; // Указатель на текущее положение в дереве

    
    private bool IsWorking => (State is not ContainerState.Idle) && (_mainContainerObject is not null);

    public JsonPersistenceContainer() : this(null) { }
    public JsonPersistenceContainer(Serializers serializers)
    {
        if (serializers is null)
        {
            serializers = new Serializers();
            serializers.RegisterDefaultSerializers();
        }
        
        Serializers = serializers;
    }
    
    public Stream Save(IExposable root)
    {
        if (State is not ContainerState.Idle) throw  new InvalidOperationException($"Attempt to save IExposable while container is not idle ({State})");

        InitializeSaveContainer();
        State = ContainerState.ScanReferences;
        Expose_Deep(ref root, RootObjectName);
        //root.ExposeData(this);
        
        State = ContainerState.Saving;
        //root.ExposeData(this);
        Expose_Deep(ref root, RootObjectName);
        _currentNode = _refsContainerObject;
        ResolveReferenceInstances();
        
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, _mainContainerObject, new JsonSerializerOptions { WriteIndented = true });
        stream.Position = 0;
        State = ContainerState.Idle;
        
        return stream;
    }

    public TExposable Load<TExposable>(Stream source, object[] ctorArgs = null) where TExposable : class, IExposable
    {
        if (State is not ContainerState.Idle) throw  new InvalidOperationException($"Attempt to load IExposable while container is not idle ({State})");
        
        InitializeLoadContainer(source);
        State = ContainerState.Loading;
        TExposable exposable = default;
        Expose_Deep(ref exposable, RootObjectName, ctorArgs);
        _currentNode = _refsContainerObject;
        ResolveReferenceInstances();
        State = ContainerState.RefsResolving;
        _currentNode = _rootObject;
        exposable.ExposeData(this);

        return exposable;
    }
    
    private void InitializeSaveContainer()
    {
        _mainContainerObject = new JsonObject();
        _rootObject = new JsonObject();
        _refsContainerObject = new JsonObject();
        
        _mainContainerObject.Add(RootObjectName, _rootObject);
        _mainContainerObject.Add(RefsObjectName, _refsContainerObject);
        _currentNode = _mainContainerObject;
        
        _knownReferences = new();
        _refIds = new();
    }

    private void InitializeLoadContainer(Stream source)
    {
        var json = (JsonObject) JsonNode.Parse(source);

        _mainContainerObject = json;
        _rootObject = (JsonObject)json[RootObjectName];
        _refsContainerObject = (JsonObject)json[RefsObjectName];
        
        _currentNode = _mainContainerObject;
        
        _knownReferences = new();
        _refIds = new();
    }

    // Переключается на вложенный объект, создавая его при необходимости.
    // Возвращает true, если удалось переключиться.
    private bool EnterNode(string name)
    {
        if (!IsWorking) return false;

        if (_currentNode.ContainsKey(name))
        {
            var property = _currentNode[name] as JsonObject;
            if (property is null) return false;
            
            _currentNode = property;
            return true;
        }
        
        var newNode = new JsonObject();
        _currentNode.Add(name, newNode);
        _currentNode = newNode;
        return true;
    }

    // Переключается на родительскую ноду
    private void ExitNode()
    {
        if (!IsWorking) return;

        _currentNode = (JsonObject) _currentNode.Parent;
    }


    private bool IsNull(string label)
    {
        if (!_currentNode.ContainsKey(label)) return true;
        
        if (EnterNode(label))
        {
            var nullMeta = ReadMeta(IsNullMetaPropertyName);
            bool result = false;
            if (nullMeta is not null)
            {
                result = true;
            }
            ExitNode();
            return result;
        }
        return false;
    }

    private bool IsNull(JsonObject jsonObject)
    {
        var preservedNode = _currentNode;
        _currentNode = jsonObject;
        var nullMeta = ReadMeta(IsNullMetaPropertyName);
        bool result = false;
        if (nullMeta is not null)
        {
            result = true;
        }
        _currentNode = preservedNode;
        return result;
    }
    
    private void SaveAsNull(string label)
    {
        if (EnterNode(label))
        {
            WriteMeta(IsNullMetaPropertyName, "true");
            ExitNode();
        }
    }

    private void WriteMeta(string key, string value)
    {
        if (EnterNode(MetaObjectName))
        {
            _currentNode[key] = value;
            ExitNode();
        }
    }

    private string ReadMeta(string key)
    {
        if (EnterNode(MetaObjectName))
        {
            var result = _currentNode[key]?.ToString();
            ExitNode();
            return result;
        }

        return null;
    }

    private ExposeAs ResolveExpositionType(Type type)
    {
        if (Serializers.CanSerialize(type)) return ExposeAs.Value;

        if (type.IsAssignableTo(typeof(IRefExposable))) return ExposeAs.Reference;

        if (type.IsAssignableTo(typeof(IExposable))) return ExposeAs.Deep;

        return ExposeAs.Undefined;
    }
}