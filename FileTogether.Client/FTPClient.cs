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
    
    public event Action<string> OnLog;
    public event Action<bool> OnConnectionChanged;
    
    public bool IsConnected => bConnected;

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

    public void Disconnect()
    {
        if (!bConnected) return;
        try
        {
            _socket.Close();
            bConnected = false;
            OnConnectionChanged?.Invoke(false);
            Log("Disconnected");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Log("Error: Failed To disconnect");
        }
    }
    
    public List<FileInfo>? GetFileList()
    {
        if (!bConnected)
        {
            Log("/GetFileList: Not connected to Server");
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
            
            if (responseResult == null)
            {
                Log("No Packet Received");
                Disconnect();
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
        if (!bConnected) return false;

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
        if (!bConnected) return false;
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
        if (!bConnected) return false;
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