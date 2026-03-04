using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FileTogether.Core;

namespace FileTogether.Server;

public class UserManager
{
    private Dictionary<string, User> _users;
    private readonly string _userFilePath;
        
    public UserManager(string userFilePath)
    {
        _userFilePath = userFilePath;
        LoadUsers();
    }

    private void LoadUsers()
    {
        if (File.Exists(_userFilePath))
        {
            string json = File.ReadAllText(_userFilePath);
            var userList = JsonSerializer.Deserialize<List<User>>(json);
            if (userList != null) _users = userList.ToDictionary(u => u.Username.ToLower());
        }
        else
        {
            // default value if no user list file
            _users = new Dictionary<string, User>();
            AddUser("admin", "admin123", UserRole.Admin);
            AddUser("user", "user123", UserRole.User);
            SaveUsers();
        }
    }

    private bool AddUser(string inUserName, string inPassword, UserRole inRole)
    {
        string lowerUsername = inUserName.ToLower();
        
        if (_users.ContainsKey(lowerUsername)) return false;
            
        string passwordHash = HashPassword(inPassword);
        var user = new User(inUserName, passwordHash, inRole);
        
        _users[lowerUsername] = user;
        SaveUsers();
        
        return true;
    }

    public bool DeleteUser(string username)
    {
        string lowerUsername = username.ToLower();
            
        if (!_users.ContainsKey(lowerUsername)) return false;
            
        _users.Remove(lowerUsername);
        SaveUsers();
            
        return true;
    }
    
    private void SaveUsers()
    {
        //Only save user object because when convert to Dictionary, use username as Key
        var userList = _users.Values.ToList();
        string usersJsonFormat = JsonSerializer.Serialize(userList, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_userFilePath, usersJsonFormat);
    }

    private string HashPassword(string inPassword)
    {
        using SHA256 sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inPassword));
        StringBuilder builder = new StringBuilder();
        foreach (byte b in bytes) builder.Append(b.ToString("x"));
        return builder.ToString();
    }
    
    public List<User> GetAllUsers()
    {
        return _users.Values.ToList();
    }
    
    public bool ChangePassword(string username, string newPassword)
    {
        string lowerUsername = username.ToLower();
            
        if (!_users.ContainsKey(lowerUsername)) return false;
            
        _users[lowerUsername].PasswordHash = HashPassword(newPassword);
        SaveUsers();
            
        return true;
    }
    
    public User? AuthenticateUser(string username, string password)
    {
        string lowerUsername = username.ToLower();
            
        if (!_users.ContainsKey(lowerUsername)) return null;
            
        var user = _users[lowerUsername];
            
        if (!user.IsActive) return null;
            
        string passwordHash = HashPassword(password);
            
        if (user.PasswordHash == passwordHash) return user;
            
        return null;
    }
}