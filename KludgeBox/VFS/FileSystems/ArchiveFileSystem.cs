using KludgeBox.VFS.Base;

namespace KludgeBox.VFS.FileSystems;

public abstract class ArchiveFileSystem : FileSystem, IDisposable
{
    public bool IsExtrtacted { get; protected set; }
    internal ProxyFileSystem _extractionProxy;
    public abstract void Dispose();

    public virtual void Extract(FsDirectory destination)
    {
        destination.FileSystem.DoReadOnlyCheck();
        _extractionProxy = new ProxyFileSystem(destination);
        TransCopyDir(Root, destination);
        IsExtrtacted = true;
    }

    public virtual void Extract(FileSystem fs, string path)
    {
        fs.DoReadOnlyCheck();
        Extract(fs.CreateDirectory(path));
    }
}
