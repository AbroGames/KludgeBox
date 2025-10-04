namespace KludgeBox.DI.Requests.ChildInjection;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ChildAttribute : Attribute
{
    private const bool DefaultDeepScan = true;
    private const By DefaultSearchBy = By.Name;
    public By SearchBy { get; }
    public bool DeepScan { get; }
    public string Name { get; }

    public ChildAttribute(By by, bool deep, string name)
    {
        SearchBy = by;
        DeepScan = deep;
        Name = name;
    }
    
    public ChildAttribute() : this(DefaultSearchBy, DefaultDeepScan, null) { }
    
    public ChildAttribute(By by) : this(by, DefaultDeepScan, null) { }
    public ChildAttribute(By by, bool deep) : this(by, deep, null) { }
    public ChildAttribute(By by, string name) : this(by, DefaultDeepScan, name) { }
}