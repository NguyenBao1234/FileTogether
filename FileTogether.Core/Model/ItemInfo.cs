namespace FileTogether.Core;

[Serializable]
public class ItemInfo
{
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsDirectory { get; set; }
        
    // Constructor
    public ItemInfo() { FileName =  string.Empty; FileSize = 0; }
        
    public ItemInfo(string fileName, long fileSize, DateTime lastModified)
    {
        FileName = fileName;
        FileSize = fileSize;
        LastModified = lastModified;
    }
        
    // Hiển thị size dễ đọc (KB, MB, GB)
    public string GetFormattedSize()
    {
        if (IsDirectory) return "<DIR>";
        if (FileSize < 1024) return $"{FileSize} B";
        if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F2} KB";
        if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024.0 * 1024):F2} MB";
        return $"{FileSize / (1024.0 * 1024 * 1024):F2} GB";
    }
}