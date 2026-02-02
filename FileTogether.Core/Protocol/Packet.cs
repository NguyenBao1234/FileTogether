namespace FileTogether.Core.Protocol;

[Serializable]
public class Packet
{
    public Command Command { get; set; }
    public byte[] Data { get; set; }
    
    public Packet(Command command, byte[] data = null)
    {
        Command = command;
        Data = data ?? Array.Empty<byte>();
    }
    
    // Chuyển packet thành byte[] để gửi qua socket
    public byte[] ToBytes()
    {
        // [Command: 1 byte][DataLength: 4 bytes][Data: n bytes]
        byte[] result = new byte[1 + 4 + Data.Length];
            
        result[0] = (byte)Command;
            
        byte[] lengthBytes = BitConverter.GetBytes(Data.Length);
        Array.Copy(lengthBytes, 0, result, 1, 4);
            
        if (Data.Length > 0) 
            Array.Copy(Data, 0, result, 5, Data.Length);
            
        return result;
    }
    
    // Parse byte[] nhận từ socket thành Packet
    public static Packet FromBytes(byte[] bytes)
    {
        if (bytes.Length < 5)
            throw new ArgumentException("Invalid packet: too short");
            
        Command command = (Command)bytes[0];
        int dataLength = BitConverter.ToInt32(bytes, 1);
            
        byte[] data = new byte[dataLength];
        
        if (dataLength > 0) 
            Array.Copy(bytes, 5, data, 0, dataLength);
            
        return new Packet(command, data);
    }
}