using KludgeBox.VFS.Base;

namespace KludgeBox.VFS.FileSystems;

/// <summary>
/// Allows to merge FileSystems. Added FileSystems will add new files or overwrite existing files.
/// </summary>
public class MergedFileSystem : FileSystem
{
    public override bool IsReadOnly => true;
    public override FsDirectory Root => FromVirtualDirectory(_virtualRoot);
    private readonly VirtualDirectory _virtualRoot;
    
    // Contains actual files.
    private readonly Dictionary<string, VirtualFile> _files = new Dictionary<string, VirtualFile>();
    
    // Contains virtual directories.
    private readonly Dictionary<string, VirtualDirectory> _directories = new Dictionary<string, VirtualDirectory>();
    private readonly List<FileSystem> _fileSystems = new List<FileSystem>();
    
    public MergedFileSystem()
    {
        _virtualRoot = new VirtualDirectory("/", null);
        _directories[_virtualRoot.path] = _virtualRoot;
        var root = new FsDirectory(this, "/");
    }

    public void Reload()
    {
        _directories.Clear();
        _files.Clear();
        _directories[_virtualRoot.path] = _virtualRoot;

        foreach (var fileSystem in _fileSystems)
        {
            MergeFileSystem(fileSystem);
        }
    }
    public void AddFileSystem(FileSystem fileSystem)
    {
        _fileSystems.Add(fileSystem);
        MergeFileSystem(fileSystem);
    }
    private void MergeFileSystem(FileSystem fileSystem)
    {
        var fsRoot = fileSystem.Root;
        ScanDirectory(fsRoot, _virtualRoot);
    }

    private void ScanDirectory(FsDirectory directory, VirtualDirectory virtualDirectory)
    {
        foreach (var dir in directory.GetDirectories())
        {
            if(virtualDirectory.HasFile(dir.Name))
                continue;
                
            var vDir = virtualDirectory.EnsureDirectoryExists(dir.Name);
            _directories[vDir.path] = vDir;
            ScanDirectory(dir, vDir);
        }
        
        foreach (var file in directory.GetFiles())
        {
            if(virtualDirectory.HasDirectory(file.Name))
                continue;
            
            var vFile = virtualDirectory.EnsureFileExists(file.Name, file);
            _files[vFile.path] = vFile;
        }
    }

    private FsDirectory FromVirtualDirectory(VirtualDirectory vDirectory)
    {
        return new FsDirectory(this, vDirectory.path);
    }

    private VirtualDirectory ToVirtualDirectory(FsDirectory directory)
    {
        if (Exists(directory.Path))
            return GetVirtualDirectory(directory.Path);

        throw new DirectoryNotFoundException(directory.Path);
    }

    private VirtualDirectory GetVirtualDirectory(string path)
    {
        return _directories[path];
    }
    
    
    
    public override byte[] ReadAllBytes(string path)
    {
        return GetFile(path).ReadAllBytes();
    }

    public override string ReadAllText(string path)
    {
        return GetFile(path).ReadAllText();
    }

    public override Stream OpenRead(string path)
    {
        return GetFile(path).OpenRead();
    }

    public override FsDirectory GetDirectory(string path)
    {
        if (IsDirectory(path))
            return new FsDirectory(this, path);

        throw new DirectoryNotFoundException(path);
    }

    public override FsFile GetFile(string path, bool softChecks = false)
    {
        DoFileCheck(path, softChecks);

        return new FsFile(this, path);
    }

    public override bool Exists(string path)
    {
        var directoryExists = _directories.ContainsKey(path);
        var fileExists = _files.ContainsKey(path);

        return directoryExists || fileExists;
    }

    public override FsEntry[] GetEntries(string path)
    {
        var entries = new List<FsEntry>();
        var vDir = GetVirtualDirectory(path);
        var dirs = vDir.GetDirectories().Select(directory => new FsDirectory(this, directory.path));
        var files = vDir.GetFiles().Select(file => file.realFile);
        entries.AddRange(dirs);
        entries.AddRange(files);

        return entries.ToArray();
    }

    public override FsFile[] GetFiles(string path)
    {
        var vDir = GetVirtualDirectory(path);
        return vDir.GetFiles().Select(file => file.realFile).ToArray();
    }

    public override FsDirectory[] GetDirectories(string path)
    {
        var vDir = GetVirtualDirectory(path);
        return vDir.GetDirectories().Select(dir => new FsDirectory(this, dir.path)).ToArray();
    }

    public override bool IsDirectory(string path)
    {
        return _directories.ContainsKey(path);
    }

    public override bool IsFile(string path)
    {
        return _files.ContainsKey(path);
    }

    public override FsEntry GetEntry(string path)
    {
        if (IsDirectory(path))
            return GetDirectory(path);
        if (IsFile(path))
            return GetFile(path);

        throw new FileNotFoundException(path);
    }

    private class VirtualDirectory : VirtualEntry
    {
        public List<VirtualEntry> contents = new List<VirtualEntry>();

        public VirtualDirectory(string path, VirtualDirectory parent = null) : base(path, parent)
        {
        }

        public bool HasFile(string fileName)
        {
            return GetFiles().Any(file => file.name == fileName);
        }

        public bool HasDirectory(string directoryName)
        {
            return GetDirectories().Any(directory => directory.name == directoryName);
        }

        public VirtualDirectory EnsureDirectoryExists(string directoryName)
        {
            if(HasDirectory(directoryName))
                return GetDirectories().First(dir => dir.name == directoryName);
            
            var dir = new VirtualDirectory(PathHelper.Combine(path, directoryName).EnsureEndingDirectorySeparator(), this);
            contents.Add(dir);
            return dir;
        }

        public VirtualFile EnsureFileExists(string fileName, FsFile realFile)
        {
            VirtualFile file;
            if (HasFile(fileName))
            {
                file = GetFiles().First(file => file.name == fileName);
                file.realFile = realFile;
                return file;
            }

            file = new VirtualFile(PathHelper.Combine(path,fileName), this);
            file.realFile = realFile;
            return file;
        }
        
        public VirtualFile[] GetFiles()
        {
            return contents
                .OfType<VirtualFile>()
                .ToArray();
        }

        public VirtualDirectory[] GetDirectories()
        {
            return contents
                .OfType<VirtualDirectory>()
                .ToArray();
        }

        public VirtualEntry[] GetEntries()
        {
            return contents.ToArray();
        }
    }

    private class VirtualFile : VirtualEntry
    {
        public FsFile realFile;

        public VirtualFile(string path, VirtualDirectory parent = null) : base(path, parent)
        {
        }
    }

    private class VirtualEntry
    {
        public string path;
        public VirtualDirectory parent;
        public string name;
        
        public VirtualEntry(string path, VirtualDirectory parent = null)
        {
            this.path = path;
            this.parent = parent ?? ((this is VirtualDirectory dir) ? dir : null);
            name = PathHelper.GetName(path);
        }
    }
    
    #region UNSUPPORTED

    private const string _UnsupportedDirExceptionMessage = "Merger filesystem only contains latest snapshot of merged filesystems. It can't directly modify it's structure.";
    private const string _UnsupportedFileExceptionMessage = "Merger filesystem only contains latest snapshot of merged filesystems. Use FsFile references to write it's contents.";
    public override Stream OpenWrite(string path)
    {
        throw new NotSupportedException(_UnsupportedFileExceptionMessage);
    }
    
    public override void WriteAllBytes(string path, byte[] bytes)
    {
        throw new NotSupportedException(_UnsupportedFileExceptionMessage);
    }

    public override void WriteAllText(string path, string text)
    {
        throw new NotSupportedException(_UnsupportedFileExceptionMessage);
    }

    public override void AppendText(string path, string text)
    {
        throw new NotSupportedException(_UnsupportedFileExceptionMessage);
    }

    public override FsDirectory CreateDirectory(string path)
    {
        throw new NotSupportedException(_UnsupportedDirExceptionMessage);
    }

    public override void DeleteDirectory(string path, bool recursive = false)
    {
        throw new NotSupportedException(_UnsupportedDirExceptionMessage);
    }

    public override FsFile CreateFile(string path)
    {
        throw new NotSupportedException(_UnsupportedDirExceptionMessage);
    }

    public override void DeleteFile(string path)
    {
        throw new NotSupportedException(_UnsupportedDirExceptionMessage);
    }
    
    public override void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        throw new NotSupportedException(_UnsupportedDirExceptionMessage);
    }

    public override void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        throw new NotSupportedException(_UnsupportedDirExceptionMessage);
    }

    public override void MoveDirectory(string sourcePath, string destinationPath)
    {
        throw new NotSupportedException(_UnsupportedDirExceptionMessage);
    }

    public override void CopyDirectory(string sourcePath, string destinationPath)
    {
        throw new NotSupportedException(_UnsupportedDirExceptionMessage);
    }
    #endregion
}
