namespace KludgeBox.VFS.Base;

public class FsEntry : IEquatable<FsEntry>
{
    public FileSystem FileSystem { get; }
    public string Path { get; protected set; }
    public string Name => Path.GetName();

    public string RealPath
    {
        get
        {
            if (FileSystem is IProxyFileSystem proxyFs)
                return proxyFs.GetRealPath(Path);
            return Path;
        }
    }
    public FsDirectory Parent => FileSystem.GetParent(Path);
    
    public bool IsDirectory => FileSystem.IsDirectory(Path);
    public bool IsFile => FileSystem.IsFile(Path);
    public virtual bool Exists => FileSystem.Exists(Path);

    public FsEntry(FileSystem fileSystem, string path)
    {
        FileSystem = fileSystem;
        Path = path;
    }

    public bool Equals(FsEntry other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(FileSystem, other.FileSystem) && Path == other.Path;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((FsEntry)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FileSystem, Path);
    }

    /// <summary>
    /// Resolves the current <see cref="FsEntry"/> into its corresponding sub-entry, which can be either a <see cref="FsDirectory"/> or a <see cref="FsFile"/>.
    /// </summary>
    /// <returns>
    /// A new instance of a derived class of <see cref="FsEntry"/> representing the resolved sub-entry.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current entry is neither a file nor a directory.
    /// </exception>
    public FsEntry ResolveEntry()
    {
        if (IsDirectory)
            return new FsDirectory(FileSystem, Path);
        
        if(IsFile)
            return new FsFile(FileSystem, Path);
        
        throw new InvalidOperationException("This entry is not a file or directory.");
    }
}
