namespace FileTogether.Core;

public class ItemRequest(string clientCurrentDirectory, string requestItemName)
{
    public string ClientCurrentDirectory { get; set; } = clientCurrentDirectory;
    public string RequestItemName { get; set; } = requestItemName;
}