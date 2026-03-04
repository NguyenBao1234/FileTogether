namespace FileTogether.Core;
[Serializable]
public class User
{
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
        
    public User()
    {
        CreatedDate = DateTime.Now;
        IsActive = true;
    }
        
    public User(string username, string passwordHash, UserRole role = UserRole.User)
    {
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
        CreatedDate = DateTime.Now;
        IsActive = true;
    }
}
    
public enum UserRole
{
    User = 0,      // Chỉ đọc + tải về
    PowerUser = 1, // Đọc + download + upload + delete
    Admin = 2      // Full quyền (delete, manage users)
}
