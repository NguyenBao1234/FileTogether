namespace FileTogether.Core;

[Serializable]
public class RegisterResponse(bool success, string message)
{
    public bool Success { get; set; } = success;
    public string Message { get; set; } = message;
}