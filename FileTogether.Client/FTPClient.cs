using System.Net;
using System.Net.Sockets;
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

    public bool  Connect(int inIpAdress, int inPort)
    {
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endpoint = new IPEndPoint(inIpAdress, inPort);
            _socket.Connect(endpoint);
        
            bConnected = true;
            OnConnectionChanged?.Invoke(true);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Log("Failed" + e.Message);
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
        try
        {
            if (!bConnected)
            {
                Log("Not connected to Server");
                return null;
            }
            var packet = PacketBuilder.CreateEmptyPacket(Command.LIST);
            NetworkHelper.SendPacket(_socket, packet);
        
            var responseResult = NetworkHelper.ReceivePacket(_socket);
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

            if (responseResult.Command == Command.OK)
            {
                var files = PacketBuilder.GetObjectFromPacket<List<FileInfo>>(responseResult);
                Log($"Found {files.Count} files");
                return files;
            }
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
            var Packet = PacketBuilder.CreateEmptyPacket(Command.DOWNLOAD);
            NetworkHelper.SendPacket(_socket, Packet);
            var responseResult = NetworkHelper.ReceivePacket(_socket);
        
            if (responseResult == null || responseResult.Command != Command.OK)
            {
                Log("Server rejected upload");
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
            var Packet = PacketBuilder.CreateObjectPacket<UploadRequest>(Command.UPLOAD, upRequest);
            NetworkHelper.SendPacket(_socket, Packet);
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
            if (responsePacket != null && responsePacket?.Command == Command.OK)
            {
                Log($"Successfully Uploaded {filePath} bytes");
                return true;
            }
        
            Log("Server not reply or rejected upload");
            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Log("upload failed as exception");
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
        OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
    
}