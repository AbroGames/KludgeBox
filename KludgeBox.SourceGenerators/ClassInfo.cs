namespace KludgeBox.SourceGenerators;

internal sealed record ClassInfo
{
    public ClassInfo(string ClassName,
        string ns,
        string[] include,
        string[] exclude,
        string basePath)
    {
        this.ClassName = ClassName;
        Namespace = ns;
        Include = include;
        Exclude = exclude;
        BasePath = basePath;
    }

    public string ClassName { get; }
    public string Namespace { get; }
    public string[] Include { get; }
    public string[] Exclude { get; }
    public string BasePath { get; }

    public void Deconstruct(out string className, out string ns, out string[] include, out string[] exclude, out string basePath)
    {
        className = this.ClassName;
        ns = this.Namespace;
        include = this.Include;
        exclude = this.Exclude;
        basePath = this.BasePath;
    }
}