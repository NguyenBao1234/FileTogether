namespace FileTogether.Core;

[Serializable]
public class FileInfo
{
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
        
    // Constructor
    public FileInfo() { FileName =  string.Empty; FileSize = 0; }
        
    public FileInfo(string fileName, long fileSize, DateTime lastModified)
    {
        FileName = fileName;
        FileSize = fileSize;
        LastModified = lastModified;
    }
        
    // Hiển thị size dễ đọc (KB, MB, GB)
    public string GetFormattedSize()
    {
        if (FileSize < 1024) return $"{FileSize} B";
        if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F2} KB";
        if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024.0 * 1024):F2} MB";
        return $"{FileSize / (1024.0 * 1024 * 1024):F2} GB";
    }
}