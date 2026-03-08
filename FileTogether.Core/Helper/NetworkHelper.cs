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
            Console.WriteLine($"[NetworkHelper/SendPacket] Sending {data.Length} bytes...");
            int totalSent = 0;
            while (totalSent < data.Length)
            {
                int sent = socket.Send(data, totalSent, data.Length - totalSent, SocketFlags.None);
            
                Console.WriteLine($"[NetworkHelper/SendPacket] Sent {sent} bytes (total: {totalSent + sent}/{data.Length})");
            
                if (sent == 0)
                {
                    Console.WriteLine("[NetworkHelper/SendPacket] ERROR: socket.Send() returned 0!");
                    return false;
                }
            
                totalSent += sent;
            }
        
            Console.WriteLine($"[NetworkHelper/SendPacket] Success! Sent all {totalSent} bytes");
            return true;
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[NetworkHelper/SendPacket] Exception: {ex}");
            return false;
        }
    }

    public static Packet? ReceivePacket(Socket socket)
    {
        try
        {
            byte[] header = new byte[5]; //Command enum (1b) + file size value (4b)
            int received = ReceiveExactly(socket, header, 5, 0);
            Console.WriteLine($"[NetworkHelper/ReceivePacket] Received {received} bytes of Header");
            if (received != 5) return null;
            
            int dataLength = BitConverter.ToInt32(header, 1);//Parse length from 1->4
            
            byte[] fullPacket = new byte[5 + dataLength];
            Array.Copy(header, 0, fullPacket, 0, 5);
            if(dataLength == 0) return PacketBuilder.CreateEmptyPacket((Command)header[0]);
            received = ReceiveExactly(socket, fullPacket, dataLength,5);
            if (received != dataLength) return null;
            Console.WriteLine($"[NetworkHelper/ReceivePacket] Received {received} bytes of full packet");
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
        Console.WriteLine($"[NetworkHelper/ReceiveExactly] Success Receive {receivedCount} bytes, Buffer = {BitConverter.ToString(buffer)}");
        return receivedCount;
    }

    public static bool SendFile(Socket socket, string filePath, IProgress<TransferProgress> progress = null)
    {
        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            int fileLength = fileData.Length;
            int chunkSize = 8192; // 8KB per chunk
            
            //Estimated Time of Arrival
            var lastUpdateTime = DateTime.Now;
            var lastSentBytes = 0;
            
            int totalSent = 0;
            while (totalSent < fileLength)
            {
                int remaining = fileLength - totalSent;
                int sizeToSend = Math.Min(chunkSize, remaining);
                socket.Send(fileData, totalSent, sizeToSend, SocketFlags.None);
                totalSent += sizeToSend;
            
                var now = DateTime.Now;
                var timeSinceLastUpdate = now.Subtract(lastUpdateTime).TotalSeconds;
                if (timeSinceLastUpdate >= 0.1 || totalSent == fileLength)
                {
                    long bytesSinceLastUpdate = totalSent - lastSentBytes;
                    double speedBytesPerSecond = bytesSinceLastUpdate / timeSinceLastUpdate;
                    int remainingByte = fileLength - totalSent;
                    TimeSpan eta = speedBytesPerSecond > 0 ?  TimeSpan.FromSeconds((double)remainingByte / speedBytesPerSecond) : TimeSpan.Zero;
                    
                    progress?.Report(new TransferProgress
                    {
                        TotalBytes = fileLength,
                        TransferredBytes = totalSent,
                        Percentage = (int)(totalSent * 100 / fileLength),
                        SpeedBytesPerSecond = speedBytesPerSecond,
                        EstimatedTimeRemaining = eta
                    });
                    
                    lastUpdateTime = now;
                    lastSentBytes = totalSent;
                }
                
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
    public static bool ReceiveFile(Socket socket, string savePath, long fileSize, IProgress<TransferProgress> progress = null)
    {
        try
        {
            byte[] buffer = new byte[8192];//8KB per chunk
            long totalReceived = 0;
            
            var lastUpdateTime = DateTime.Now;
            long lastReceiveBytes = 0;
                
            using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
            {
                //Estimated Time of Arrival

                
                while (totalReceived < fileSize)
                {
                    long remaining = fileSize - totalReceived;
                    int toReceive = (int)Math.Min(buffer.Length, remaining);
                        
                    int received = socket.Receive(buffer, 0, toReceive, SocketFlags.None);
                    if (received == 0) return false; // Connection lost
                    
                        
                    fs.Write(buffer, 0, received);//do có biến đánh dấu của OS, nên luôn ghi bytes mới từ buffer vào 
                    totalReceived += received;
                    var now = DateTime.Now;
                    var speedBytesPerSecond = (totalReceived - lastReceiveBytes) / now.Subtract(lastUpdateTime).TotalSeconds;
                    var eta = speedBytesPerSecond > 0 ? TimeSpan.FromSeconds((fileSize - totalReceived)/speedBytesPerSecond) : TimeSpan.Zero;
                    
                    lastUpdateTime = now;
                    lastReceiveBytes = totalReceived;
                    progress?.Report(new TransferProgress
                    {
                        TotalBytes = fileSize,
                        TransferredBytes = totalReceived,
                        Percentage = (int)(totalReceived * 100 / fileSize),
                        SpeedBytesPerSecond = speedBytesPerSecond,
                        EstimatedTimeRemaining = eta
                    });
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