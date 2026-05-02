namespace KludgeBox.SourceGenerators;

internal class ProjectFile
{
    public string Path { get; }
    public ProjectFile(string path)
    {
        Path = path;
    }
}