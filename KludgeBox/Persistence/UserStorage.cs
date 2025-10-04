using Godot;

namespace KludgeBox.Persistence;

public class UserStorage
{
    public string ReadAllText(string fileName)
    {
        return File.ReadAllText(GetPathForFile(fileName));
    }

    public async Task<string> ReadAllTextAsync(string fileName)
    {
        return await File.ReadAllTextAsync(GetPathForFile(fileName));
    }
    
    public void WriteAllText(string fileName, string text)
    {
        File.WriteAllText(GetPathForFile(fileName), text);
    }

    public async Task WriteAllTextAsync(string fileName, string text)
    {
        await File.WriteAllTextAsync(GetPathForFile(fileName), text);
    }
    
    public byte[] ReadAllBytes(string fileName)
    {
        return File.ReadAllBytes(GetPathForFile(fileName));
    }

    public async Task<byte[]> ReadAllBytesAsync(string fileName)
    {
        return await File.ReadAllBytesAsync(GetPathForFile(fileName));
    }

    public void WriteAllBytes(string fileName, byte[] bytes)
    {
        File.WriteAllBytes(GetPathForFile(fileName), bytes);
    }
    
    public async Task WriteAllBytesAsync(string fileName, byte[] bytes)
    {
        await File.WriteAllBytesAsync(GetPathForFile(fileName), bytes);
    }

    
    public bool FileExists(string fileName)
    {
        return File.Exists(GetPathForFile(fileName));
    }

    public void DeleteFile(string fileName)
    {
        File.Delete(GetPathForFile(fileName));
    }
    
    public void DeleteDirectory(string directoryName) => DeleteDirectory(directoryName, false);
    
    public void DeleteDirectory(string directoryName, bool recursive)
    {
        Directory.Delete(GetPathForFile(directoryName), recursive);
    }

    public void CreateDirectory(string directoryName)
    {
        Directory.CreateDirectory(GetPathForFile(directoryName));
    }

    public string GetUserDirectory()
    {
        return ProjectSettings.GlobalizePath("user://");
    }

    public string GetPathForFile(string fileName)
    {
        return GetUserDirectory().PathJoin(fileName);
    }
}