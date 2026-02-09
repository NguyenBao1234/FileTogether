namespace FileTogether.Core;

public class FileDisplayInfo(FileInfo fileInfo)
{
    public string FileName { get; set; } = fileInfo.FileName;
    public string FormattedSize { get; set; } = fileInfo.GetFormattedSize();
    public System.DateTime LastModified { get; set; } = fileInfo.LastModified;
    public FileInfo OriginalFile { get; set; } = fileInfo;
}