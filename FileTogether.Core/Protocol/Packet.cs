using System.Text;

namespace FileTogether.Core.Protocol;
/// <summary>
/// Packet contain: command + data 
/// </summary>
[Serializable]
public class Packet
{
    public Command Command { get; set; }
    public byte[] Data { get; set; }
    public string SessionToken { get; set; }
    
    public Packet(Command command, byte[]? data = null, string sessionToken = null)
    {
        Command = command;
        Data = data ?? Array.Empty<byte>();
        SessionToken = sessionToken;
    }
    
    // Chuyển packet thành byte[] để gửi qua socket
    public byte[] ToBytes()
    {
        var tokenBytes = string.IsNullOrEmpty(SessionToken) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(SessionToken);
        var tokenBytesLength = tokenBytes.Length;//Calculate the token length to allow for flexible changes or integration with different token generation methods.
        // [Command: 1 byte][Token length: 4 byte][36 bytes token][DataLength: 4 bytes][Data: n bytes]
        byte[] result = new byte[1 + 4 + tokenBytesLength + 4 + Data.Length];
            
        result[0] = (byte)Command;
  
        byte[] tokenLengthBytes = BitConverter.GetBytes(tokenBytesLength);
        Array.Copy(tokenLengthBytes, 0, result, 1, 4);
        if (tokenBytes.Length > 0)
            Array.Copy(tokenBytes, 0, result, 5, tokenBytesLength);
        
        int offset = 5 + tokenBytesLength;
        byte[] dataLengthBytes = BitConverter.GetBytes(Data.Length);
        Array.Copy(dataLengthBytes, 0, result, offset, 4);
        
        offset += 4;
        if (Data.Length > 0)
            Array.Copy(Data, 0, result, offset, Data.Length);
        Console.WriteLine($"[Packet.ToBytes] Command Index: [{result[0]}]");
        return result;
    }
    
    // Parse byte[] nhận từ socket thành Packet
    public static Packet FromBytes(byte[] bytes)
    {
        if (bytes.Length < 9)
            throw new ArgumentException("Invalid packet: too short");
        Console.WriteLine($"[Packet.FromBytes] Command Index: [{bytes[0]}]");
        Command command = (Command)bytes[0];
        int tokenLength = BitConverter.ToInt32(bytes, 1);
        byte[] tokenBytes = new byte[tokenLength];
        // Token
        string sessionToken = null;
        if (tokenLength > 0) sessionToken = Encoding.UTF8.GetString(bytes, 5, tokenLength);
        
        int offset = 5 + tokenLength;
        int dataLength = BitConverter.ToInt32(bytes, offset);
        byte[] data = new byte[dataLength];
        
        if (dataLength > 0) 
            Array.Copy(bytes, offset + 4, data, 0, dataLength);
            
        return new Packet(command, data, sessionToken);
    }
}