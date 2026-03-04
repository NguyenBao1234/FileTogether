namespace FileTogether.Core;

[Serializable]
public class LoginResponse(bool success, string message, string sessionToken = null, UserRole role = UserRole.User)
{
    public bool Success { get; set; } = success;
    public string Message { get; set; } = message;
    public string SessionToken { get; set; } = sessionToken; // Unique token for session
    public UserRole Role { get; set; } = role;
}