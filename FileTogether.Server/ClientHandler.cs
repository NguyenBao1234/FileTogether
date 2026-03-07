using System.IO;
using System.Net.Sockets;
using FileTogether.Core;
using FileTogether.Core.Helper;
using FileTogether.Core.Protocol;

namespace FileTogether.Server;

public class ClientHandler
{
    private Socket _clientSocket;
    private string _sharedFolder;
    private string _currentDirectory;
    private Thread _thread;
    
    private FTPServer _ftpServer;
    private SessionManager _sessionManager;
    private UserManager _userManager;
    private Session? _currentSession;
    public event Action<string> OnLog;
    
    public ClientHandler(Socket clientSocket, string sharedFolder, SessionManager sessionManager,
        UserManager userManager, FTPServer ftpServer)
    {
        _clientSocket = clientSocket;
        _sharedFolder = sharedFolder;
        _sessionManager = sessionManager;
        _userManager = userManager;
        _ftpServer = ftpServer;
        _currentSession = null; //not login yet
        _currentDirectory = ""; //root
    }

    public void Start()
    {
        _thread = new Thread(HandleClient);
        _thread.IsBackground = true;
        _thread.Start();
    }

    private void HandleClient()
    {
        string clientIP = _clientSocket.RemoteEndPoint.ToString();
        Log($"[/HandleClient] Thread started for {clientIP}");
        Log($"[HandleClient] {clientIP} connected");

        try
        {
            while (true)
            {
                Packet packet = NetworkHelper.ReceivePacket(_clientSocket);
                if (packet == null) break; // Client disconnect
            
                HandleCommand(packet);
            }
        }
        catch (Exception ex)
        {
            Log($"Error with {clientIP}: {ex.Message}");
        }
        finally
        {
            if (_currentSession != null)
            {
                Log($"Cleaning up session for user '{_currentSession.User.Username}'");
                _sessionManager.RemoveSession(_currentSession.Token);
                _currentSession = null;
            }
        
            _clientSocket.Close();
            Log($"Client disconnected: {clientIP}");
        }
    }

    //Handle Client's Command
    private void HandleCommand(Packet packet)
    {
        Log("/Handle Command]start Handle Command");

        if (packet.Command == Command.REGISTER)
        {
            HandleRegister(packet);
            return;
        }

        if (packet.Command == Command.LOGIN)
        {
            HandleLogin(packet);
            return;
        }
        
        if (packet.Command == Command.LOGOUT)
        {
            HandleLogout();
            return;
        }

        if (_currentSession == null)
        {
            SendUnauthorized("Not logged in");
            return;
        }
        
        HandleAuthorizedCommand(packet);
    }

    private void HandleRegister(Packet packet)
    {
        try
        {
            var registerRequest = PacketBuilder.GetObjectFromPacket<AccountCredentialRequest>(packet);
            Log($"Registration attempt: {registerRequest.Username}");
        
            // Validate username
            if (string.IsNullOrWhiteSpace(registerRequest.Username))
            {
                SendRegisterResponse(false, "Username cannot be empty");
                return;
            }
            
            if (registerRequest.Username.Length is < 3 or > 20)
            {
                SendRegisterResponse(false, "Username must be at least 3 characters and not exceed 20 characters");
                return;
            }
        
            // Validate password
            if (string.IsNullOrWhiteSpace(registerRequest.Password))
            {
                SendRegisterResponse(false, "Password cannot be empty");
                return;
            }
        
            if (registerRequest.Password.Length < 6)
            {
                SendRegisterResponse(false, "Password must be at least 6 characters");
                return;
            }
        
            // Tạo user mới vào hệ thống
            bool success = _userManager.AddUser(registerRequest.Username, registerRequest.Password, UserRole.User);
            if (success)
            {
                SendRegisterResponse(true, "Registration successful! You can now login.");
                Log($"User '{registerRequest.Username}' registered successfully");
            }
            else
            {
                SendRegisterResponse(false, "Username already exists");
                Log($"Registration failed: Username '{registerRequest.Username}' already exists");
            }
        }
        catch (Exception e)
        {
            Log("Registration error: "+ e.Message);
            SendRegisterResponse(false, e.Message);
        }
    }

    private void HandleLogin(Packet packet)
    {
        try
        {
            // Parse LoginRequest từ packet
            var loginRequest = PacketBuilder.GetObjectFromPacket<AccountCredentialRequest>(packet);
        
            Log($"Login attempt: {loginRequest.Username}");
        
            // Authenticate
            var user = _userManager.AuthenticateUser(loginRequest.Username, loginRequest.Password);
            Log($"Finish Authenticate");
            if (user != null)
            {
                Log($"Start session for {loginRequest.Username}");
                // Tạo session
                string clientIP = _clientSocket.RemoteEndPoint.ToString();
                string token = _sessionManager.CreateSession(user, clientIP);
            
                _currentSession = _sessionManager.GetSession(token);
            
                // Gửi response thành công
                var response = new LoginResponse(true, "Login successful", token, user.Role);
                var responsePacket = PacketBuilder.CreateObjectPacket(Command.LOGIN_RESPONSE, response);
                NetworkHelper.SendPacket(_clientSocket, responsePacket);
            
                Log($"User '{user.Username}' (Role: {user.Role}) logged in successfully");
            }
            else
            {
                // Gửi response thất bại
                var response = new LoginResponse(false, "Invalid username or password");
                var responsePacket = PacketBuilder.CreateObjectPacket(Command.LOGIN_RESPONSE, response);
                NetworkHelper.SendPacket(_clientSocket, responsePacket);
            
                Log($"Failed login attempt for: {loginRequest.Username}");
            }
        }
        catch (Exception ex)
        {
            SendError($"Login error: {ex.Message}");
            Log($"Login error: {ex.Message}");
        }
    }
    
    private void HandleLogout()
    {
        if (_currentSession != null)
        {
            Log($"User '{_currentSession.User.Username}' logging out");
        
            _sessionManager.RemoveSession(_currentSession.Token);
            _currentSession = null;
        
            NetworkHelper.SendPacket(_clientSocket, PacketBuilder.CreateEmptyPacket(Command.OK));
            _ftpServer.NotifyClientCountChanged(-1);
            Log("User logged out successfully");
        }
    }

    private void HandleAuthorizedCommand(Packet packet)
    {
        switch (packet.Command)
        {
            case Command.LIST:
                HandleListItems();
                break;
            case Command.UPLOAD:
                if(_currentSession.User.Role >= UserRole.PowerUser) HandleUpload(packet);
                else SendUnauthorized("You don't have permission to upload");
                break;
            case Command.DOWNLOAD:
                HandleDownload(packet);
                break;
            case Command.DELETE:
                if(_currentSession.User.Role >= UserRole.PowerUser) HandleDelete(packet);
                else SendUnauthorized("You don't have permission to delete");
                break;
            
            //Directory commands
            case Command.CREATE_DIR:
                HandleCreateDirectory(packet);
                break;
            
            case Command.DELETE_DIR:
                HandleDeleteDirectory(packet);
                break;
            
            case Command.CHANGE_DIR:
                HandleChangeDirectory(packet);
                break;
            
            case Command.GET_CURRENT_DIR:
                HandleGetCurrentDirectory();
                break;
            default:
                SendError("Unknown command");
                break;
        }
    }

    private void HandleListItems()
    {
        Log("[/HandleListFiles]:Start");
        try
        {
            string path = GetAbsolutePath();
            var dirs = Directory.GetDirectories(path)
                .Select(d => new DirectoryInfo(d))
                .Select(di=> new ItemInfo(di.Name, 0, di.LastAccessTime))
                .ToList();
            
            var files = Directory.GetFiles(_sharedFolder)
                .Select(f => new System.IO.FileInfo(f))
                .Select(fi => new FileTogether.Core.ItemInfo(fi.Name, fi.Length, fi.LastWriteTime))
                .ToList();
            
            var allItems = dirs.Concat(files).ToList();//Concatenate = ghép nối
            
            var fileListPacket = PacketBuilder.CreateObjectPacket(Command.ITEM_LIST, allItems);
            NetworkHelper.SendPacket(_clientSocket, fileListPacket);
                
            Log($"[/HandleListFiles] Sent item list: {dirs.Count} folder, {files.Count} files");
        }
        catch (Exception ex)
        {
            SendError($"Error listing files: {ex.Message}");
        }
    }
    private void HandleUpload(Packet packet)
    {
        Log("[/HandleUpload]:Start");
        var uploadRequest = PacketBuilder.GetObjectFromPacket<UploadRequest>(packet);
        string fileName = uploadRequest.FileName;
        string savePath = GetAbsolutePath(fileName);
        
        // Gửi OK để client bắt đầu gửi
        NetworkHelper.SendPacket(_clientSocket, PacketBuilder.CreateEmptyPacket(Command.OK));
        
        // Nhận file
        bool success = NetworkHelper.ReceiveFile(_clientSocket, savePath, uploadRequest.FileSize);
        
        if (success)
        {
            Log($"Received file: {fileName} ");
            NetworkHelper.SendPacket(_clientSocket, PacketBuilder.CreateEmptyPacket(Command.OK));
        }
        else
        {
            SendError("Upload failed");
        }
    }

    private void HandleDownload(Packet packet)
    {
        Log("[/HandleDownload]:Start");
        try
        {
            string fileName = PacketBuilder.GetTextFromPacket(packet);
            string filePath = GetAbsolutePath(fileName);
                
            if (!File.Exists(filePath))
            {
                SendError($"No File found has filepath: {filePath}");
                return;
            }
                
            var fileInfo = new System.IO.FileInfo(filePath);
                
            // Gửi OK + file size first to client know size to pass parameter
            var okPacket = PacketBuilder.CreateTextPacket(Command.OK, fileInfo.Length.ToString());
            NetworkHelper.SendPacket(_clientSocket, okPacket);
                
            // Gửi file data
            NetworkHelper.SendFile(_clientSocket, filePath);
                
            Log($"Handle Download Finished: Sent file {fileName} ({fileInfo.Length} bytes)");
        }
        catch (Exception ex)
        {
            SendError($"Download error: {ex.Message}");
        }
    }

    private void HandleDelete(Packet packet)
    {
        Log("[/HandleDelete]:Start");
        try
        {
            string fileName = PacketBuilder.GetTextFromPacket(packet);
            string filePath = Path.Combine(_sharedFolder, fileName);
                
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                NetworkHelper.SendPacket(_clientSocket, 
                    PacketBuilder.CreateEmptyPacket(Command.OK));
                Log($"Handle Delete Finished: Deleted file0 '{fileName}'");
            }
            else
            {
                SendError("File not found");
            }
        }
        catch (Exception ex)
        {
            SendError($"Delete error: {ex.Message}");
        }
    }
    
    private void HandleCreateDirectory(Packet packet)
    {
        if (_currentSession.User.Role < UserRole.PowerUser)
        {
            SendUnauthorized("No permission to create directory");
            return;
        }

        try
        {
            var dirPath = PacketBuilder.GetTextFromPacket(packet);
            dirPath = GetAbsolutePath(dirPath);
            if (Directory.Exists(dirPath))
            {
                SendError("Directory already exists");
                return;
            }
        
            Directory.CreateDirectory(dirPath);
        
            NetworkHelper.SendPacket(_clientSocket, PacketBuilder.CreateEmptyPacket(Command.OK));
            Log($"Handle Create Directory: {dirPath} in {_currentDirectory}");
        }
        catch (Exception exception)
        {
            SendError($"Create directory exception error: {exception.Message}");
        }
    }
    
    private void HandleDeleteDirectory(Packet packet)
    {
        if (_currentSession.User.Role < UserRole.Admin)
        {
            SendUnauthorized("No permission to delete directory");
            return;
        }

        try
        {
            var requestDirPath =PacketBuilder.GetTextFromPacket(packet);
            var dirPath = GetAbsolutePath(requestDirPath);
        
            if (!Directory.Exists(dirPath))
            {
                SendError("Directory not found");
                return;
            }
        
            Directory.Delete(dirPath, recursive: true);
            NetworkHelper.SendPacket(_clientSocket, PacketBuilder.CreateEmptyPacket(Command.OK));
        
            Log($"Deleted directory: {requestDirPath} from {_currentDirectory}");
        }
        catch (Exception ex)
        {
            SendError($"Delete directory exception error: {ex.Message}");
        }
    }
    
    private void HandleChangeDirectory(Packet packet)
    {
        var requestDirPath = PacketBuilder.GetTextFromPacket(packet);

        if (requestDirPath == "..")
        {
            if (String.IsNullOrEmpty(_currentDirectory))
            {
                SendError("Already at root directory");
                return;
            }
            // Lùi về parent
            int lastSeparator = _currentDirectory.LastIndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            
            if (lastSeparator > 0) _currentDirectory = _currentDirectory.Substring(0, lastSeparator);
            else _currentDirectory = ""; // Về root
        }
        else
        {
            string newPath = GetAbsolutePath(requestDirPath);
            
            if (!Directory.Exists(newPath))
            {
                SendError("Directory not found");
                return;
            }
            
            // Update current directory
            if (string.IsNullOrEmpty(_currentDirectory))
            {
                _currentDirectory = requestDirPath;
            }
            else
            {
                _currentDirectory = Path.Combine(_currentDirectory, requestDirPath);
            }
        }
        
        var responsePacket = PacketBuilder.CreateTextPacket(Command.OK, _currentDirectory);
        NetworkHelper.SendPacket(_clientSocket, responsePacket);
    }

    
    private void HandleGetCurrentDirectory()
    {
        try
        {
            var responsePacket = PacketBuilder.CreateTextPacket(Command.OK, _currentDirectory);
            NetworkHelper.SendPacket(_clientSocket, responsePacket);
        }
        catch (Exception e)
        {
            SendError($"Get current directory exception error: {e.Message}");
        }
    }

    
    private string GetAbsolutePath(string relativePath = null)
    {
        string path;
    
        if (string.IsNullOrEmpty(relativePath))
        {
            // Lấy đường dẫn hiện tại
            path = Path.Combine(_sharedFolder, _currentDirectory);
        }
        else
        {
            path = Path.Combine(_sharedFolder, _currentDirectory, relativePath);
        }
    
        // Normalize path avoid path traversal attack (exp: ../../Windows/System32)
        path = Path.GetFullPath(path);
    
        // avoid beyond shared folder
        if (!path.StartsWith(_sharedFolder))
        {
            throw new UnauthorizedAccessException("Access denied: Path outside shared folder");
        }
    
        return path;
    }
    
    private string GetRelativePath(string absolutePath)
    {
        if (absolutePath.StartsWith(_sharedFolder))
        {
            string relative = absolutePath.Substring(_sharedFolder.Length);
            return relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);//Delete '/' or '\' char in first left side
        }
        return "";
    }
    
    private void SendError(string message)
    {
        Log($"Error sent: {message}");
        var errorPacket = PacketBuilder.CreateTextPacket(Command.ERROR, message);
        NetworkHelper.SendPacket(_clientSocket, errorPacket);
    }
    
    private void SendUnauthorized(string message)
    {
        var packet = PacketBuilder.CreateTextPacket(Command.UNAUTHORIZED, message);
        NetworkHelper.SendPacket(_clientSocket, packet);
        Log($"Unauthorized: {message}");
    }
    
    private void SendRegisterResponse(bool success, string message)
    {
        var response = new RegisterResponse(success, message);
        var packet = PacketBuilder.CreateObjectPacket(Command.REGISTER_RESPONSE, response);
        NetworkHelper.SendPacket(_clientSocket, packet);
    }

    private void Log(string message)
    {
        OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}-ClientHandler] {message}");
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}-ClientHandler] {message}");
    }
}