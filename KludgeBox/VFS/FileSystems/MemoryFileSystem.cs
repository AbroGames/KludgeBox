using System.Text;
using KludgeBox.VFS.Base;

namespace KludgeBox.VFS.FileSystems;

public class MemoryFileSystem : FileSystem
{
    public override bool IsReadOnly => false;
    public override FsDirectory Root => new FsDirectory(this, "/");
    public bool Multipart { get; private set; }
    
    private Dictionary<string, ISet<FsEntry>> _directories = new();
    private Dictionary<string, MemoryFile> _files = new();
    private static int _nextId = 0;
    
    /// <summary>
    /// Creates a new file system within the computer's RAM (memory).
    /// If multipart is set to false, files will store their data in a single monolithic byte array.
    /// Using <b>multipart</b> consumes additional memory during READ operations and may slow down file reading.
    /// However, it significantly speeds up operations like <b>AppendBytes</b> and <b>AppendText</b>.
    /// <b>Multipart</b> can also be applied individually to each file.
    /// </summary>
    /// <param name="multipart">If true, all files will operate in multipart mode by default.</param>
    public MemoryFileSystem(bool multipart = false)
    {
        Multipart = multipart;
        _directories.Add("/", new HashSet<FsEntry>());
        Name = $"MemFS #{_nextId++} {(multipart ? "(multipart)" : "")}";
    }

    
    public override byte[] ReadAllBytes(string path)
    {
        DoFileCheck(path);
        return _files[path].Data;
    }

    public override string ReadAllText(string path)
    {
        DoFileCheck(path);
        return Encoding.UTF8.GetString(_files[path].Data);
    }

    public override void WriteAllBytes(string path, byte[] bytes)
    {
        if (IsDirectory(path))
            throw new InvalidOperationException($"{path} is a directory");
        
        if(!IsFile(path))
            CreateFile(path);

        _files[path].Data = bytes;
    }

    public override void WriteAllText(string path, string text)
    {
        WriteAllBytes(path, Encoding.UTF8.GetBytes(text));
    }

    public override void AppendText(string path, string text)
    {
        AppendBytes(path, Encoding.UTF8.GetBytes(text));
    }

    public override void AppendBytes(string path, byte[] bytes)
    {
        if(!IsFile(path) && !IsDirectory(path))
            CreateFile(path);
        
        _files[path].Append(bytes);
    }

    public override FsDirectory CreateDirectory(string path)
    {
        path = path.EnsureEndingDirectorySeparator();
        var result = new FsDirectory(this, path);
        if (IsDirectory(path))
            return result;

        var dirs = path.PathsChain();
        string prev = "/";
        foreach (var dir in dirs)
        {
            if (IsDirectory(dir))
            {
                prev = dir;
                continue;
            }
            var set = new HashSet<FsEntry>();
            _directories[prev].Add(new FsDirectory(this, dir));
            _directories[dir] = set;
            prev = dir;
        }

        return result;
    }

    public override void DeleteDirectory(string path, bool recursive = false)
    {
        if (path.IsRoot())
            throw new IOException($"Attempt to delete the root of {Name}");
        
        DoDirectoryCheck(path);

        var entries = GetEntries(path);

        foreach (var entry in entries)
        {
            if (entry is FsDirectory dir)
            {
                if (!dir.IsEmpty && !recursive)
                    throw new IOException($"Directory not empty: {dir.Path}");
                
                DeleteDirectory(dir.Path, recursive);
            }
            
            if (entry is FsFile file)
                DeleteFile(file.Path);
        }
        var parent = _directories[PathHelper.GetParent(path)];
        parent.Remove(parent.First(entry => entry.Path == path));
        _directories.Remove(path);
    }

    public override FsFile CreateFile(string path)
    {
        return CreateFile(path, Array.Empty<byte>());
    }

    public FsFile CreateFile(string path, byte[] data)
    {
        path = path.TrimEndingDirectorySeparator();
        string filename = PathHelper.GetName(path);
        string parent = PathHelper.GetParent(path);
        string normalizedPath = PathHelper.NormalizePath(path);
        
        if (!_directories.ContainsKey(parent))
            throw new DirectoryNotFoundException(parent);
        
        _directories[parent].Add(new FsFile(this, path));
        var file = new MemoryFile(data, Multipart);
        _files[normalizedPath] = file;
        
        return new FsFile(this, path);
    }

    public override void DeleteFile(string path)
    {
        string filename = PathHelper.GetName(path);
        string parent = PathHelper.GetParent(path);
        string normalizedPath = PathHelper.NormalizePath(path);

        DoFileCheck(normalizedPath);
        var file = _files[normalizedPath];
        file.Data = null;
        _files.Remove(normalizedPath);
        var dir = _directories[parent];
        dir.Remove(dir.First(e => e.Path == normalizedPath));
        File.Delete(filename);
    }
    
    

    public override Stream OpenRead(string path)
    {
        DoFileCheck(path);
        return new MemoryStream(_files[path].Data);
    }

    public override Stream OpenWrite(string path)
    {
        if (IsDirectory(path))
            throw new InvalidOperationException($"{path} is a directory");
        
        if(!IsFile(path))
            CreateFile(path);
            
        return new MemoryFileStream(_files[path]);
    }

    public override FsDirectory GetDirectory(string path)
    {
        DoDirectoryCheck(path);
        return new FsDirectory(this, path);
    }

    public override FsFile GetFile(string path, bool softChecks = false)
    {
        DoFileCheck(path, softChecks);
        return new FsFile(this, path);
    }

    public override bool Exists(string path)
    {
        return IsDirectory(path) || IsFile(path);
    }

    public override FsEntry[] GetEntries(string path)
    {
        DoDirectoryCheck(path);
        return _directories[path.EnsureEndingDirectorySeparator()].ToArray();
    }

    public override FsFile[] GetFiles(string path)
    {
        return GetEntries(path).OfType<FsFile>().ToArray();
    }

    public override FsDirectory[] GetDirectories(string path)
    {
        return GetEntries(path).OfType<FsDirectory>().ToArray();
    }

    public override void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        DoFileCheck(sourcePath);
        DoDirectoryCheck(PathHelper.GetParent(destinationPath));
        if (IsDirectory(destinationPath))
            throw new IOException($"{destinationPath} is directory");

        var alreadyExists = IsFile(destinationPath);
        
        if (alreadyExists && !overwrite)
            throw new IOException($"{destinationPath} already exists");
        
        if(!alreadyExists)
            CreateFile(destinationPath);
        var dir = _directories[PathHelper.GetParent(sourcePath)];
        dir.Remove(dir.First(x => x.Path == sourcePath));
        _files[destinationPath] = _files[sourcePath];
        _files.Remove(sourcePath);
    }

    public override void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        DoFileCheck(sourcePath);
        DoDirectoryCheck(PathHelper.GetParent(destinationPath));
        if (IsDirectory(destinationPath))
            throw new IOException($"{destinationPath} is directory");

        var alreadyExists = IsFile(destinationPath);
        
        if (alreadyExists && !overwrite)
            throw new IOException($"{destinationPath} already exists");
        
        if(!alreadyExists)
            CreateFile(destinationPath);
        
        _files[destinationPath].Data = _files[sourcePath].Data;
    }

    public override void MoveDirectory(string sourcePath, string destinationPath)
    {
        if (sourcePath.IsRoot())
            throw new IOException($"Attempt to move the root of {Name}");
        
        DoDirectoryCheck(sourcePath);
        if (Exists(destinationPath))
            throw new IOException($"{destinationPath} already exists");
        
        var entries = _directories[sourcePath].ToArray();
        CreateDirectory(destinationPath);
        
        foreach (var entry in entries)
        {
            var entryDestination = PathHelper.Combine(destinationPath, entry.Name);
            if (entry is FsFile)
            {
                MoveFile(entry.Path, entryDestination);
            }

            if (entry is FsDirectory)
            {
                MoveDirectory(entry.Path, entryDestination);
            }
        }
        DeleteDirectory(sourcePath, true);
    }

    public override void CopyDirectory(string sourcePath, string destinationPath)
    {
        DoDirectoryCheck(sourcePath);
        if (Exists(destinationPath))
            throw new IOException($"{destinationPath} already exists");

        var entries = _directories[sourcePath].ToArray();
        CreateDirectory(destinationPath);

        foreach (var entry in entries)
        {
            var entryDestination = PathHelper.Combine(destinationPath, entry.Name);
            if(entry is FsFile)
                CopyFile(entry.Path, entryDestination);
            
            if(entry is FsDirectory)
                CopyDirectory(entry.Path, entryDestination);
        }
    }

    public override bool IsDirectory(string path)
    {
        return _directories.ContainsKey(path.EnsureEndingDirectorySeparator());
    }

    public override bool IsFile(string path)
    {
        return _files.ContainsKey(path);
    }

    public override FsEntry GetEntry(string path)
    {
        DoFileCheck(path);
        return new FsEntry(this, path);
    }
    
    #region File and Stream

    private sealed class MemoryFile
    {
        public int Size
        {
            get
            {
                if(!Multipart)
                    return _data.Length;

                int size = _data.Length;
                if (_dataIsDirty)
                    size += _multipartData.Sum(part => part.Length);

                return size;
            }
        }
        public byte[] Data
        {
            get
            {
                if (Multipart)
                    MergeMultipartData();
                return Clone(_data);
            }
            set
            {
                _data = (value is not null) ? Clone(value) : null;
                if (Multipart)
                {
                    _multipartData = new();
                    _dataIsDirty = false;
                }
            }
        }

        public byte[] DataRef
        {
            get
            {
                if (Multipart)
                    MergeMultipartData();
                return _data;
            }
            set
            {
                _data = value;
                if (Multipart)
                {
                    _multipartData = new();
                    _dataIsDirty = false;
                }
            }
        }
        private byte[] _data;
        private List<byte[]> _multipartData = new();
        private bool _dataIsDirty = false;
        public bool Multipart { get; private set;}

        public void Append(byte[] data)
        {
            if (Multipart)
            {
                _multipartData.Add(Clone(data));
                _dataIsDirty = true;
                return;
            }

            _data = MergeUsingBlockCopy(_data, data);
        }
        
        private static byte[] MergeUsingBlockCopy(byte[] firstArray, byte[] secondArray)
        {
            var combinedArray = new byte[firstArray.Length + secondArray.Length];
            Buffer.BlockCopy(firstArray, 0, combinedArray, 0, firstArray.Length);
            Buffer.BlockCopy(secondArray, 0, combinedArray, firstArray.Length, secondArray.Length);
            return combinedArray;
        }

        private static byte[] Clone(byte[] source)
        {
            var result = new byte[source.Length];
            Buffer.BlockCopy(source, 0, result, 0, source.Length);
            return result;
        }
        
        private void MergeMultipartData()
        {
            if (!_dataIsDirty)
                return;
            
            int totalLength = _multipartData.Sum(array => array.Length);
            totalLength += _data.Length;
            byte[] result = new byte[totalLength];
            Buffer.BlockCopy(_data, 0, result, 0, _data.Length);
            
            int currentIndex = _data.Length;
            foreach (byte[] byteArray in _multipartData)
            {
                Buffer.BlockCopy(byteArray, 0, result, currentIndex, byteArray.Length);
                currentIndex += byteArray.Length;
            }

            _multipartData = new();
            _data = result;
            _dataIsDirty = false;
        }
        public MemoryFile() : this(true){}
        public MemoryFile(bool multipart) : this(Array.Empty<byte>(), multipart){}
        public MemoryFile(byte[] data, bool multipart = true)
        {
            _data = Clone(data);
            Multipart = multipart;
        }
    }

    private sealed class MemoryFileStream : Stream
    {
        private readonly MemoryFile _file;

            private byte[] Content
            {
                get { return _file.DataRef; }
                set { _file.DataRef = value; }
            }

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => true;

            public override long Length => _file.Size;

            public override long Position { get; set; }

            public MemoryFileStream(MemoryFile file)
            {
                _file = file;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (origin == SeekOrigin.Begin)
                    return Position = offset;
                if (origin == SeekOrigin.Current)
                    return Position += offset;
                return Position = Length - offset;
            }

            public override void SetLength(long value)
            {
                int newLength = (int)value;
                byte[] newContent = new byte[newLength];
                Buffer.BlockCopy(Content, 0, newContent, 0, Math.Min(newLength, (int)Length));
                Content = newContent;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int mincount = Math.Min(count, Math.Abs((int)(Length - Position)));
                Buffer.BlockCopy(Content, (int)Position, buffer, offset, mincount);
                Position += mincount;
                return mincount;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (Length - Position < count)
                    SetLength(Position + count);
                byte[] newContent = Content;
                Buffer.BlockCopy(buffer, offset, newContent, (int)Position, count);
                Position += count;
                Content = newContent;
            }
    }
    
    
    #endregion
    
}
