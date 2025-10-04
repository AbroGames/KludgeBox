using System.IO.Compression;
using KludgeBox.VFS.Base;

namespace KludgeBox.VFS.FileSystems;

public class ZipFileSystem : ArchiveFileSystem
{
    // basic implementation with ReadOnly access
    public override bool IsReadOnly => true;//_access == FileAccess.Read;
    public override FsDirectory Root => new FsDirectory(this, "/");

    private Stream _archiveStream;
    private int _fsNumber = 0;
    private ZipArchive _archive;
    
    private Dictionary<string, ISet<FsEntry>> _directories = new();
    private Dictionary<string, ZipArchiveEntry> _files = new();

    public ZipFileSystem(string path, FileAccess access)
        : this(File.Open(path, FileMode.Open, access))
    {
        if (access == FileAccess.Write)
            throw new ArgumentException($"{nameof(access)}: can't create ZipFS in write-only mode");
    }

    public ZipFileSystem(Stream stream)
    {
        if (!stream.CanRead)
            throw new IOException("Can't read Zip stream");

        Name = $"ZipFS #{_fsNumber++} (R{(IsReadOnly ? "" : "W")})";
        _archiveStream = stream;
        
        _archive = new ZipArchive(_archiveStream, IsReadOnly ? ZipArchiveMode.Read : ZipArchiveMode.Update);
        _directories["/"] = new HashSet<FsEntry>();
        ScanEntries();
    }
    public override FsDirectory CreateDirectory(string path)
    {
        throw new NotSupportedException();
    }

    public override void DeleteDirectory(string path, bool recursive = false)
    {
        throw new NotSupportedException();
    }

    public override FsFile CreateFile(string path)
    {
        throw new NotSupportedException();
    }

    public override void DeleteFile(string path)
    {
        throw new NotSupportedException();
    }

    public override Stream OpenRead(string path)
    {
        path = path.NormalizePath();
        DoFileCheck(path);
        return _files[path].Open();
    }

    public override Stream OpenWrite(string path)
    {
        throw new NotSupportedException();
    }

    public override FsFile[] GetFiles(string path)
    {
        path = path.NormalizePath().EnsureEndingDirectorySeparator();
        DoDirectoryCheck(path);
        return _directories[path].OfType<FsFile>().ToArray();
    }

    public override FsDirectory[] GetDirectories(string path)
    {
        
        path = path.NormalizePath().EnsureEndingDirectorySeparator();
        DoDirectoryCheck(path);
        return _directories[path].OfType<FsDirectory>().ToArray();
    }

    public override bool IsDirectory(string path)
    {
        return _directories.ContainsKey(path.NormalizePath().EnsureEndingDirectorySeparator());
    }

    public override bool IsFile(string path)
    {
        
        return _files.ContainsKey(path.NormalizePath());
    }

    public override void Dispose()
    {
        _archiveStream.Dispose();
        _archive.Dispose();
    }

    private void ScanEntries()
    {
        foreach (var zipEntry in _archive.Entries)
        {
            var path = zipEntry.FullName.NormalizePath().EnsureRoot();
            if (path.EndsWith("/"))
            {
                BuildDirectories(path);
                continue;
            }
                
            var dirPath = path.GetParent();
            
            _files.TryAdd(path, zipEntry);
            BuildDirectories(dirPath);
            _directories[dirPath].Add(new FsFile(this, path));
        }
    }

    private void BuildDirectories(string dirPath)
    {
        dirPath = dirPath.NormalizePath().EnsureEndingDirectorySeparator();
        if (dirPath.IsRoot())
            return;
        
        var added = _directories.TryAdd(dirPath, new HashSet<FsEntry>());
        if (!added)
            return;
        
        var parent = dirPath.GetParent();
        BuildDirectories(parent);
        _directories[parent].Add(new FsDirectory(this, dirPath));
    }
}
