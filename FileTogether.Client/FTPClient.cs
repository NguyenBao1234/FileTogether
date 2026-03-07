using System.Net;
using System.Net.Sockets;
using System.Windows;
using FileTogether.Core;
using FileTogether.Core.Helper;
using FileTogether.Core.Protocol;


namespace FileTogether.Client;

public class FTPClient
{
    private Socket _socket;
    private bool bConnected;
    
    private bool bAuthenticated;
    private string? _sessionToken;
    private User? _currentUser;
    
    public event Action<string> OnLog;
    public event Action<bool> OnConnectionChanged;
    
    public bool IsConnected => bConnected;
    public bool IsAuthenticated => bAuthenticated; 
    public User CurrentUser => _currentUser;

    public bool  Connect(string inIpAdress, int inPort)
    {
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endpoint = new IPEndPoint(IPAddress.Parse(inIpAdress), inPort);
            _socket.Connect(endpoint);
        
            bConnected = true;
            OnConnectionChanged?.Invoke(true);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Log("Failed" + e.Message);
            MessageBox.Show("Error: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    public LoginResponse Login(string username, string password)
    {
        if (!bConnected)
        {
            Log("Not connected to server");
            return new LoginResponse(false, "Not connected to server");
        }
        // make LoginRequest
        var loginRequest = new LoginRequest(username, password);
        var packet = PacketBuilder.CreateObjectPacket(Command.LOGIN, loginRequest);
        Log($"Sending login request for user: {username}");
        NetworkHelper.SendPacket(_socket, packet);
        
        var response = NetworkHelper.ReceivePacket(_socket);
        if (response == null)
        {
            Log("Connection lost or Server decline connect");
            Disconnect();
            return new LoginResponse(false, "Connection lost or Server decline connect");
        }
        if (response.Command == Command.LOGIN_RESPONSE)
        {
            Log("Start Parse Login Response");
            var loginResponse = PacketBuilder.GetObjectFromPacket<LoginResponse>(response);//parse packet
            var bSuccess = loginResponse.Success;

            bAuthenticated = bSuccess;
            _sessionToken = bSuccess ? loginResponse.SessionToken : null;
            _currentUser =  bSuccess ? new User(username, "", loginResponse.Role) : null;
            
            string msg = loginResponse.Message;
            Log($"Login successful. {msg}");
            
            return loginResponse;
        }
        if (response.Command == Command.ERROR)
        {
            string error = PacketBuilder.GetTextFromPacket(response);
            Log($"Login error: {error}");
            return new LoginResponse(false, error);
        }
        
        return new LoginResponse(false, "Unexpected response from server");
    }

    public void Disconnect()
    {
        if (!bConnected) return;
        try
        {
            _socket.Close();
            bConnected = false;
            OnConnectionChanged?.Invoke(false);
            bAuthenticated = false;
            _sessionToken = null;
            _currentUser = null;
            Log("Disconnected");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Log("Error: Failed To disconnect");
        }
    }
    
    public bool Logout()
    {
        if (!bConnected || !bAuthenticated) return false;
    
        try
        {
            var packet = PacketBuilder.CreateEmptyPacket(Command.LOGOUT);
            NetworkHelper.SendPacket(_socket, packet);
        
            var response = NetworkHelper.ReceivePacket(_socket);
        
            bAuthenticated = false;
            _sessionToken = null;
            _currentUser = null;
        
            Log("Logged out successfully");
        
            return response != null && response.Command == Command.OK;
        }
        catch (Exception ex)
        {
            Log($"Logout error: {ex.Message}");
            return false;
        }
        
    }
    
    public List<FileInfo>? GetFileList()
    {
        if (!bConnected||!bAuthenticated)
        {
            Log("/GetFileList: Not authenticated or connected to Server");
            return null;
        }
        
        try
        {
            var packet = PacketBuilder.CreateEmptyPacket(Command.LIST);
            Log("Send command to " + _socket.RemoteEndPoint);

            NetworkHelper.SendPacket(_socket, packet);


            Log(" Waiting response of " + _socket.RemoteEndPoint);
            var responseResult = NetworkHelper.ReceivePacket(_socket);
            Log(" Start resolve response of " + _socket.RemoteEndPoint);
            //Check if ex error
            if (responseResult == null)
            {
                Log("No Packet Received");
                Disconnect();
                return null;
            }
            //Check if unauthorized
            if (responseResult.Command == Command.UNAUTHORIZED)
            {
                string msg = PacketBuilder.GetTextFromPacket(responseResult);
                Log($"Unauthorized: {msg}");
                bAuthenticated = false;
                return null;
            }
            
            if (responseResult.Command == Command.ERROR)
            {
                string error = PacketBuilder.GetTextFromPacket(responseResult);
                Log($"Download failed: {error}");
                return null;
            }

            if (responseResult.Command == Command.FILE_LIST)
            {
                Console.WriteLine(_socket.RemoteEndPoint + "'s response is OK, now getting list of files");
                var files = PacketBuilder.GetObjectFromPacket<List<FileInfo>>(responseResult);
                Log($"Found {files.Count} files");
                return files;
            }
            Log("response's command: "+responseResult.Command);
            return null;
        }
        catch (Exception e)
        {
            Log("Error: " + e.Message);
            Console.WriteLine(e);
            return null;
        }
    }

    public bool DownloadFile(string fileName, string savePath, IProgress<int> progress = null )
    {
        if (!bConnected||!bAuthenticated)
        {
            Log("/GetFileList: Not authenticated or connected to Server");
            return false;
        }

        try
        {
            var packet = PacketBuilder.CreateTextPacket(Command.DOWNLOAD, fileName);
            NetworkHelper.SendPacket(_socket, packet);
            var responseResult = NetworkHelper.ReceivePacket(_socket);
        
            if (responseResult == null || responseResult.Command != Command.OK)
            {
                Log("Server rejected upload");
                if (responseResult is { Command: Command.ERROR }) Log(PacketBuilder.GetTextFromPacket(responseResult));
                return false;
            }

            long fileSize = long.Parse(PacketBuilder.GetTextFromPacket(responseResult));
            Log($"Downloading {fileSize} bytes");
        
            bool bReceiveSuccess =  NetworkHelper.ReceiveFile(_socket, savePath, fileSize, progress);
            if (bReceiveSuccess)
            {
                Log($"Downloaded Successfully {fileName} ({fileSize} bytes)");
                return true;
            }
            else
            {
                Log($"Download failed: {fileName} ({fileSize} bytes)");
                return false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Log("Client Download Error: " + e.Message);
            return false;
        }
        
    }

    public bool UploadFile(string filePath, IProgress<int> progress = null)
    {
        if (!bConnected||!bAuthenticated)
        {
            Log("/GetFileList: Not authenticated or connected to Server");
            return false;
        }
        try
        {
            var file = new System.IO.FileInfo(filePath);
            var upRequest = new UploadRequest(file.Name, file.Length);
            var packet = PacketBuilder.CreateObjectPacket<UploadRequest>(Command.UPLOAD, upRequest);
            NetworkHelper.SendPacket(_socket, packet);
            var responseResult = NetworkHelper.ReceivePacket(_socket);

            if (responseResult == null || responseResult.Command != Command.OK)
            {
                Log("Server rejected upload");
                return false;
            }

            Log($"Uploading {filePath} bytes");

            var bSendResult = NetworkHelper.SendFile(_socket, filePath, progress);
            if (!bSendResult)
            {
                Log($"Uploaded failed, maybe connection error");
                return false;
            }

            var responsePacket = NetworkHelper.ReceivePacket(_socket);
            if (responsePacket is { Command: Command.OK })
            {
                Log($"Successfully Uploaded {filePath} bytes");
                return true;
            }
        
            Log("Server not reply or rejected upload");
            return false;
        }
        catch (Exception e)
        {
            Log("upload failed as exception");
            MessageBox.Show(e.Message);
            Console.WriteLine(e);
            return false;
        }
    }

    public bool DeleteFile(string fileName)
    {
        if (!bConnected||!bAuthenticated)
        {
            Log("/GetFileList: Not authenticated or connected to Server");
            return false;
        }
        try
        {
            var packet = PacketBuilder.CreateTextPacket(Command.DELETE, fileName);
            NetworkHelper.SendPacket(_socket, packet);
                
            var response = NetworkHelper.ReceivePacket(_socket);
                
            if (response == null)
            {
                Log("Connection lost");
                Disconnect();
                return false;
            }
                
            if (response.Command == Command.OK)
            {
                Log($"Deleted: {fileName}");
                return true;
            }
            else if (response.Command == Command.ERROR)
            {
                string error = PacketBuilder.GetTextFromPacket(response);
                Log($"Delete failed: {error}");
                return false;
            }
                
            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Log("Delete failed as exception");
            return false;
        }
    }


    private void Log(string message)
    {
        OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}-FTP Client] {message}");
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}-FTP Client] {message}");
    }
    
}