namespace FileTogether.Core;

[Serializable]
public class UploadRequest(string fileName, long fileSize, string clientCurrentDirectory)
{
    public string FileName { get; set; } = fileName;
    public long FileSize { get; set; } = fileSize;
    public string ClientCurrentDirectory { get; set; } = clientCurrentDirectory;
}
