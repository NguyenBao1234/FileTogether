using System.Text;
using System.Text.Json;

namespace FileTogether.Core.Protocol;

public class PacketBuilder
{
    public static Packet CreateTextPacket(Command inCommand, string inText, string? sessionToken = null)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(inText);
        return new Packet(inCommand, bytes, sessionToken);
    }

    public static Packet CreateObjectPacket<T>(Command inCommand, T inJSonObj, string? sessionToken = null)
    {
        string jsonSerializedString = JsonSerializer.Serialize(inJSonObj);
        return CreateTextPacket(inCommand, jsonSerializedString,  sessionToken);
    }
    
    // Parse packet thành string
    public static string GetTextFromPacket(Packet packet)
    {
        return Encoding.UTF8.GetString(packet.Data);
    }

    public static T GetObjectFromPacket<T>(Packet packet)
    {
        var json =  Encoding.UTF8.GetString(packet.Data);
        return JsonSerializer.Deserialize<T>(json);
    }
    
    public static Packet CreateEmptyPacket(Command command, string sessionToken = null)
    {
        return new Packet(command, null, sessionToken);
    }
}