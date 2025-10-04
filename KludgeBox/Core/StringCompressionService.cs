using System.IO.Compression;
using System.Text;

namespace KludgeBox.Core;

public class StringCompressionService
{
    
    public string CompressGzipBase64(string text)
    {
        byte[] compressedBytes = CompressGzip(text);
        return Convert.ToBase64String(compressedBytes);
    }

    public string DecompressGzipBase64(string compressed)
    {
        byte[] compressedBytes = Convert.FromBase64String(compressed);
        return DecompressGzip(compressedBytes);
    }
    
    public byte[] CompressGzip(string text, CompressionLevel level = CompressionLevel.Optimal)
    {
        byte[] rawBytes = Encoding.UTF8.GetBytes(text);

        using var output = new MemoryStream();
        using var gzip = new GZipStream(output, level);
        
        gzip.Write(rawBytes, 0, rawBytes.Length);
        return output.ToArray();
    }

    public string DecompressGzip(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        
        gzip.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }
}