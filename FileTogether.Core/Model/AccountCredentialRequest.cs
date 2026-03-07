namespace FileTogether.Core;

/// <summary>
/// Request from client. It can use for Login or Register.
/// Contain: Username and password 
/// </summary>
[Serializable]
public class AccountCredentialRequest(string username, string password)
{
    public string Username { get; set; } = username;
    public string Password { get; set; } = password; // Plain text (sẽ hash ở server)
}