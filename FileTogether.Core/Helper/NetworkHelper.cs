using System.IO;
using System.Net.Sockets;
using FileTogether.Core.Protocol;

namespace FileTogether.Core.Helper;

public class NetworkHelper
{
    public static bool SendPacket(Socket socket, Packet packet)
    {
        try
        {
            byte[] data = packet.ToBytes();
            int sent = socket.Send(data);
            return sent == data.Length;
        }
        catch
        {
            return false;
        }
    }

    public static Packet ReceivePacket(Socket socket)
    {
        try
        {
            byte[] header = new byte[5]; //Command enum (1b) + file size value (4b)
            int received = ReceiveExactly(socket, header, 5, 0);
            if (received != 5) return null;
            
            int dataLength = BitConverter.ToInt32(header, 1);//Parse length from 1->4
            
            byte[] fullPacket = new byte[5 + dataLength];
            Array.Copy(header, 5, fullPacket, 0, dataLength);
            if(dataLength == 0) return null;
            received = ReceiveExactly(socket, fullPacket, dataLength,5);
            if (received != dataLength) return null;
            return Packet.FromBytes( fullPacket);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
            
        }
    }

    private static int ReceiveExactly(Socket socket, byte[] buffer, int size, int offset = 0)
    {
        int receivedCount = 0;
        while (receivedCount < size)
        {
            int bytesRead = socket.Receive(buffer, offset + receivedCount, size - receivedCount, SocketFlags.None);
            if (bytesRead == 0) break; // interrupt connection
            receivedCount += bytesRead;
        }
        return receivedCount;
    }

    public static bool SendFile(Socket socket, string filePath, IProgress<int> progress = null)
    {
        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            int fileLength = fileData.Length;
            int chunkSize = 8192; // 8KB per chunk
            int totalSent = 0;
            while (totalSent < fileLength)
            {
                int remaining = fileLength - totalSent;
                int sizeToSend = Math.Min(chunkSize, remaining);
                socket.Send(fileData, totalSent, sizeToSend, SocketFlags.None);
                totalSent += sizeToSend;
            
                progress?.Report(totalSent * 100 / fileData.Length);
            }
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
    
    // Nhận file từ socket
    public static bool ReceiveFile(Socket socket, string savePath, long fileSize, IProgress<int> progress = null)
    {
        try
        {
            byte[] buffer = new byte[8192];
            long totalReceived = 0;
                
            using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
            {
                while (totalReceived < fileSize)
                {
                    long remaining = fileSize - totalReceived;
                    int toReceive = (int)Math.Min(buffer.Length, remaining);
                        
                    int received = socket.Receive(buffer, 0, toReceive, SocketFlags.None);
                    if (received == 0) return false; // Connection lost
                        
                    fs.Write(buffer, 0, received);
                    totalReceived += received;
                        
                    progress?.Report((int)(totalReceived * 100 / fileSize));
                }
            }
                
            return true;
        }
        catch
        {
            return false;
        }
    }
}