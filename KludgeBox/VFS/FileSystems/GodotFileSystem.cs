using System.ComponentModel;
using Godot;
using KludgeBox.VFS.Base;
using FileAccess = Godot.FileAccess;

namespace KludgeBox.VFS.FileSystems;

public enum GodotFsRoot
{
    /// <summary>
    /// Sets the root to res://
    /// </summary>
    Res,
    
    /// <summary>
    /// Sets the root to user://
    /// </summary>
    User,
    
    /// <summary>
    /// Sets no root, so you can access host filesystem or specify the root manually
    /// </summary>
    System
}

public class GodotFileSystem : FileSystem, IProxyFileSystem
{
    private string _root;
    private static int _count = 0;
    public override bool IsReadOnly => false;
    public string RealPath => _root;
    public GodotFileSystem(GodotFsRoot root, string path = null, bool createDirectories = false)
    {
        _root = root switch
        {
            GodotFsRoot.System => path ?? "/",
            GodotFsRoot.User => "user://" + (path?.RemoveRoot() ?? ""),
            GodotFsRoot.Res => "res://" + (path?.RemoveRoot() ?? ""),
            _ => throw new InvalidEnumArgumentException()
        };

        if(createDirectories) DirAccess.MakeDirRecursiveAbsolute(_root);
        
        DirAccess dir = DirAccess.Open(_root);
        if (DirAccess.GetOpenError() != Error.Ok)
            throw new IOException($"Unable to open directory '{_root}'");
        
        Name = $"GodotFS #{_count++} ({_root})";
    }
        
    public override FsDirectory CreateDirectory(string path)
    {
        DirAccess access = DirAccess.Open(_root);
        if (access.DirExists(path))
            return new FsDirectory(this, this.GetRealPath(path));
        
        var result = access.MakeDirRecursive(this.GetRealPath(path));
        if (result != Error.Ok)
            throw new IOException($"Can't create directory {this.GetRealPath(path)}");
        
        return new FsDirectory(this, this.GetRealPath(path));
    }

    
    public override void DeleteDirectory(string path, bool recursive = false)
    {
        DoDirectoryCheck(this.GetRealPath(path));
        DirAccess access = DirAccess.Open(_root);
        if (access.DirExists(this.GetRealPath(path)))
        {
            if (recursive)
            {
                foreach (var file in GetFiles(this.GetRealPath(path)))
                    file.Delete();

                foreach (var dir in GetDirectories(this.GetRealPath(path)))
                    dir.Delete(true);
            }
            
            access.Remove(this.GetRealPath(path));
        }
    }

    public override FsFile CreateFile(string path)
    {
        var fa = FileAccess.Open(
            this.GetRealPath(path), 
            FileAccess.ModeFlags.Write
            );
        try
        {
            fa.Close();
        }
        catch
        {
            //Log.Warning($"Can't close created file ({this.GetRealPath(path)}): {FileAccess.GetOpenError()}");
        }
        return new FsFile(this, path);
    }

    public override void DeleteFile(string path)
    {
        DoFileCheck(this.GetRealPath(path));
        var dir = DirAccess.Open(this.GetRealPath(path));
        dir.Remove(this.GetRealPath(path));
    }

    public override Stream OpenRead(string path)
    {
        return new GodotFileStream(this.GetRealPath(path), FileAccess.ModeFlags.ReadWrite);
    }

    public override Stream OpenWrite(string path)
    {
        return new GodotFileStream(this.GetRealPath(path), FileAccess.ModeFlags.ReadWrite);
    }

    public override FsFile[] GetFiles(string path)
    {
        DoDirectoryCheck(this.GetRealPath(path));
        var dir = DirAccess.Open(this.GetRealPath(path));
        if (DirAccess.GetOpenError() != Error.Ok)
            throw new IOException($"Unable to open directory '{this.GetRealPath(path)}'");

        return dir.GetFiles().Select(f => new FsFile(this, PathHelper.Combine(path, f))).ToArray();
    }

    public override FsDirectory[] GetDirectories(string path)
    {
        DoDirectoryCheck(this.GetRealPath(path));
        var dir = DirAccess.Open(this.GetRealPath(path));
        if (DirAccess.GetOpenError() != Error.Ok)
            throw new IOException($"Unable to open directory '{this.GetRealPath(path)}'");

        return dir.GetDirectories().Select(f => new FsDirectory(this, PathHelper.Combine(path, f))).ToArray();
    }

    public override bool IsDirectory(string path)
    {
        return DirAccess.DirExistsAbsolute(this.GetRealPath(path));
    }

    public override bool IsFile(string path)
    {
        return FileAccess.FileExists(this.GetRealPath(path));
    }

    public override void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        sourcePath = this.GetRealPath(sourcePath);
        destinationPath = this.GetRealPath(destinationPath);
        DoFileCheck(sourcePath);

        if (IsDirectory(destinationPath))
            throw new IOException($"{destinationPath} is directory");

        if (IsFile(destinationPath) && !overwrite)
            throw new IOException($"{destinationPath} already exists");

        DirAccess.RenameAbsolute(sourcePath, destinationPath);
    }
    
    public override void MoveDirectory(string sourcePath, string destinationPath)
    {
        sourcePath = this.GetRealPath(sourcePath);
        destinationPath = this.GetRealPath(destinationPath);
        DoDirectoryCheck(sourcePath);
        DoDirectoryCheck(sourcePath);
        if (Exists(destinationPath))
            throw new IOException($"{destinationPath} already exists");

        DirAccess.RenameAbsolute(sourcePath, destinationPath);
    }

    public override void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        sourcePath = this.GetRealPath(sourcePath);
        destinationPath = this.GetRealPath(destinationPath);
        DoFileCheck(sourcePath);
        DoFileCheck(destinationPath, true);
        if (Exists(destinationPath) && !overwrite)
            throw new IOException($"{destinationPath} already exists");

        DirAccess.CopyAbsolute(sourcePath, destinationPath);
    }


    public override void WriteAllBytes(string path, byte[] bytes)
    {
        path = this.GetRealPath(path);
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file.StoreBuffer(bytes);
        file.Close();
    }

    public override void WriteAllText(string path, string text)
    {
        path = this.GetRealPath(path);
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file.StoreString(text);
        file.Close();
    }

    public override byte[] ReadAllBytes(string path)
    {
        path = this.GetRealPath(path);
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var data = file.GetBuffer((long)file.GetLength());
        file.Close();
        return data;
    }

    public override string ReadAllText(string path)
    {
        path = this.GetRealPath(path);
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var text = file.GetAsText();
        file.Close();
        return text;
    }


    #region GodotFileStream
    private class GodotFileStream : Stream, IDisposable
    {
        private FileAccess _file;
        private FileAccess.ModeFlags _flags;
        
        public GodotFileStream(string path, FileAccess.ModeFlags flags, FileAccess.CompressionMode? compressionMode = null)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            _flags = flags;
            Error result;
            if (compressionMode.HasValue)
            {
                _file = FileAccess.OpenCompressed(path, flags, compressionMode.Value);
                result = _file.GetError();
            }
            else
            {
                _file = FileAccess.Open(path, flags);
                result = _file.GetError();
            }

            if (result != Error.Ok)
            {
                throw new IOException($"Unable to open \"{path}\": {result}");
            }
        }
        
        public override bool CanRead => _flags == FileAccess.ModeFlags.Read || _flags == FileAccess.ModeFlags.ReadWrite || _flags == FileAccess.ModeFlags.WriteRead;

        public override bool CanSeek => true;

        public override bool CanWrite => _flags == FileAccess.ModeFlags.Write || _flags == FileAccess.ModeFlags.ReadWrite || _flags == FileAccess.ModeFlags.WriteRead;

        public override long Length => (long)_file.GetLength();
        
        public override long Position
        {
            get => (long)_file.GetPosition();
            set => _file.Seek((ulong)value);
        }
        
        public override void Flush()
        {
            _file.Flush();
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_file.IsOpen())
                throw new ObjectDisposedException($"File has been closed");

            if (!CanRead)
                throw new NotSupportedException($"Cannot Read on a GodotFileStream with flags {_flags}");

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count + offset > buffer.Length)
                throw new ArgumentException($"count + offset is greater than the given buffer length");
        
            var actualCount = Position + count < Length ? count : (int)(Length - Position);
            GD.Print($"Actual count: {actualCount}, offset: {offset}, count: {count}, position: {Position}, length: {Length}");
            var data = _file.GetBuffer(actualCount);
            if (_file.GetError() != Error.Ok)
            {
                throw new IOException($"Error reading file: {_file.GetError()}");
            }
            
            Array.Copy(data, 0, buffer, offset, data.Length);
            return data.Length;
        }
        
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!_file.IsOpen())
                throw new ObjectDisposedException($"File has been closed");

            switch (origin)
            {
                case SeekOrigin.Begin:
                    _file.Seek((ulong)offset);
                    break;

                case SeekOrigin.Current:
                    _file.Seek(_file.GetPosition() + (ulong)offset);
                    break;

                case SeekOrigin.End:
                    _file.SeekEnd(offset);
                    break;
            }

            if (_file.GetError() != Error.Ok)
                throw new IOException($"Error seeking file: {_file.GetError()}");

            return (long)_file.GetPosition();
        }
        
        public override void SetLength(long value)
        {
            throw new NotSupportedException("GodotFileStream does not support SetLength");
        }
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_file.IsOpen())
                throw new ObjectDisposedException($"File has been closed");

            if (!CanWrite)
                throw new NotSupportedException($"Cannot Write on a GodotFileStream with flags {_flags}");

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count + offset > buffer.Length)
                throw new ArgumentException($"count + offset is greater than the given buffer length");

            var data = new byte[count];
            Array.Copy(buffer, offset, data, 0, count);

            _file.StoreBuffer(data);
            if (_file.GetError() != Error.Ok)
                throw new IOException($"Error writing file: {_file.GetError()}");
        }
        
        protected override void Dispose(bool disposing)
        {
            _file.Close();
        }
    }
    #endregion

}
