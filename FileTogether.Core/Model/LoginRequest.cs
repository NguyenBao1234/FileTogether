namespace FileTogether.Core;

[Serializable]
public class LoginRequest(string username, string password)
{
    public string Username { get; set; } = username;
    public string Password { get; set; } = password; // Plain text (sẽ hash ở server)
}