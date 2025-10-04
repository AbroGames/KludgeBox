namespace KludgeBox.DI.Requests.ParentInjection;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ParentAttribute : Attribute
{
    private const bool DefaultDeepScan = false;
    private const By DefaultSearchBy = By.Type;
    public By SearchBy { get; }
    public bool DeepScan { get; }
    public string Name { get; }

    
    public ParentAttribute(By by, bool deep, string name)
    {
        SearchBy = by;
        DeepScan = deep;
        Name = name;
    }
    
    public ParentAttribute() : this(DefaultSearchBy, DefaultDeepScan, null) { }
    
    public ParentAttribute(By by) : this(by, DefaultDeepScan, null) { }
    public ParentAttribute(bool deep) : this(DefaultSearchBy, deep, null) { }
    public ParentAttribute(By by, bool deep) : this(by, deep, null) { }
    public ParentAttribute(By by, string name) : this(by, DefaultDeepScan, name) { }
}