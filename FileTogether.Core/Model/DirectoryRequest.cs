namespace FileTogether.Core;

public class DirectoryRequest(string clientCurrentDirectory, string requestDirectory)
{
    public string ClientCurrentDirectory { get; set; } = clientCurrentDirectory;
    public string RequestDirectory { get; set; } = requestDirectory;
}