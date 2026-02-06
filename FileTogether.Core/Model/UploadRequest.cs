namespace FileTogether.Core;

[Serializable]
public class UploadRequest
{
    public string FileName { get; set; }
    public long FileSize { get; set; }
    
    public UploadRequest() { }
    
    public UploadRequest(string fileName, long fileSize)
    {
        FileName = fileName;
        FileSize = fileSize;
    }
}
