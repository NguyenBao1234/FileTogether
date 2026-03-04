using FileTogether.Core;

namespace FileTogether.Server;

public class SessionManager
{
    private Dictionary<string, Session> _sessions; // Token -> Session
        
    public SessionManager()
    {
        _sessions = new Dictionary<string, Session>();
    }
        
    public string CreateSession(User user, string clientIP)
    {
        string token = Guid.NewGuid().ToString(); //Globally Unique Identifier
        var session = new Session
        {
            Token = token,
            User = user,
            ClientIP = clientIP,
            LoginTime = DateTime.Now,
            LastActivity = DateTime.Now
        };
        
        _sessions[token] = session;
            
        return token;
    }
        
    public Session GetSession(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;
            
        if (_sessions.ContainsKey(token))
        {
            var session = _sessions[token];
            session.LastActivity = DateTime.Now;
            return session;
        }
            
        return null;
    }
        
    public void RemoveSession(string token)
    {
        if (!string.IsNullOrEmpty(token) && _sessions.ContainsKey(token))
            _sessions.Remove(token);
        
    }
        
    public void CleanupExpiredSessions(int timeoutMinutes = 30)
    {
        var expired = new List<string>();
        var now = DateTime.Now;
            
        foreach (var kvp in _sessions) 
            if ((now - kvp.Value.LastActivity).TotalMinutes > timeoutMinutes)
                expired.Add(kvp.Key);
            
        foreach (var token in expired) 
            _sessions.Remove(token);
    }
    
    
}

public class Session
{
    public string Token { get; set; }
    public User User { get; set; }
    public string ClientIP { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime LastActivity { get; set; }
}