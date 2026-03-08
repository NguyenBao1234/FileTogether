namespace FileTogether.Core;

public class ItemDisplayInfo(ItemInfo itemInfo)
{
    public string FileName { get; set; } = itemInfo.FileName;
    public string FormattedSize { get; set; } = itemInfo.GetFormattedSize();
    public DateTime LastModified { get; set; } = itemInfo.LastModified;
    public string TypeIcon  { get; set; } = itemInfo.IsDirectory ? "📁" : "📄";
    public ItemInfo OriginalFile { get; set; } = itemInfo;
}