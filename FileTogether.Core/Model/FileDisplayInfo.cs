namespace FileTogether.Core;

public class FileDisplayInfo(ItemInfo itemInfo)
{
    public string FileName { get; set; } = itemInfo.FileName;
    public string FormattedSize { get; set; } = itemInfo.GetFormattedSize();
    public System.DateTime LastModified { get; set; } = itemInfo.LastModified;
    public ItemInfo OriginalFile { get; set; } = itemInfo;
}