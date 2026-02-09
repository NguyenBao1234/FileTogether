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
    private Thread _thread;
    
    public event Action<string> OnLog;
    
    public ClientHandler(Socket clientSocket, string sharedFolder)
    {
        _clientSocket = clientSocket;
        _sharedFolder = sharedFolder;
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
        
        while (true)
        {
            Packet packet = NetworkHelper.ReceivePacket(_clientSocket);
            if (packet == null) break; // Client disconnect
            
            HandleCommand(packet);
        }
    }

    //Handle Client's Command
    private void HandleCommand(Packet packet)
    {
        Log("/Handle Command]start Handle Command");
        switch (packet.Command)
        {
            case Command.LIST:
                HandleListFiles();
                break;
            case Command.UPLOAD:
                HandleUpload(packet);
                break;
            case Command.DOWNLOAD:
                HandleDownload(packet);
                break;
            case Command.DELETE:
                HandleDelete(packet);
                break;
            default:
                SendError("Unknown command");
                break;

        }
    }
    
    private void HandleListFiles()
    {
        Log("[/HandleListFiles]:Start");
        try
        {
            var files = Directory.GetFiles(_sharedFolder)
                .Select(f => new System.IO.FileInfo(f))
                .Select(fi => new FileTogether.Core.FileInfo(fi.Name, fi.Length, fi.LastWriteTime))
                .ToList();
                
            var fileListPacket = PacketBuilder.CreateObjectPacket(Command.FILE_LIST, files);
            NetworkHelper.SendPacket(_clientSocket, fileListPacket);
                
            Log($"[/HandleListFiles] Sent file list: {files.Count} files");
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
        string savePath = Path.Combine(_sharedFolder, fileName);
        
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
            string filePath = Path.Combine(_sharedFolder, fileName);
                
            if (!File.Exists(filePath))
            {
                SendError($"No File found has filepath: {filePath}");
                return;
            }
                
            var fileInfo = new System.IO.FileInfo(filePath);
                
            // Gửi OK + file size
            var okPacket = PacketBuilder.CreateTextPacket(Command.OK, 
                fileInfo.Length.ToString());
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
    private void SendError(string message)
    {
        Log($"Error sent: {message}");
        var errorPacket = PacketBuilder.CreateTextPacket(Command.ERROR, message);
        NetworkHelper.SendPacket(_clientSocket, errorPacket);
    }

    private void Log(string message)
    {
        OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}-ClientHandler] {message}");
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}-ClientHandler] {message}");
    }
}