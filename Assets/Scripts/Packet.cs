using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

[Serializable]
public enum EPacketType
{
    UNDEFINED,
    MOVEMENTS,
    SPECTATORS_MOVEMENTS,
    MOVE_VALIDITY,
    UNITY_MESSAGE,
    CHAT_MESSAGE,
    TEAM_SWITCH,
    VERIFICATION,
    TEAM_TURN
}

[Serializable]
public class PacketHeader
{
    public EPacketType type = EPacketType.UNDEFINED;
    public int size = 0;
}

[Serializable]
public class Packet
{
    public PacketHeader header = new PacketHeader();
    public byte[] datas;

    public byte[] Serialize(EPacketType type, object obj)
    {
        header.type = type;

        datas = ObjectToByteArray(obj);
        header.size = datas.Length;

        byte[] headerBytes = ObjectToByteArray(header);

        using (MemoryStream packetStream = new MemoryStream())
        {
            packetStream.Write(headerBytes);
            packetStream.Write(datas);

            return packetStream.ToArray();
        }
    }

    public static byte[] SerializePacket(EPacketType type, object obj) => new Packet().Serialize(type, obj);

    public static Packet DeserializeHeader(byte[] packetAsByte)
    {
        Packet packet = new Packet();

        packet.header = (PacketHeader)ByteArrayToObject(packetAsByte);

        return packet;
    }
        
    public static int PacketSize() => ObjectToByteArray(new PacketHeader()).Length;

    public T FillObject<T>() => (T)ByteArrayToObject(datas);

    // Convert an object to a byte array
    public static byte[] ObjectToByteArray(object obj)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream memstream = new MemoryStream())
        {
            formatter.Serialize(memstream, obj);
            return memstream.ToArray();
        }
    }

    // Convert a byte array to an Object
    public static object ByteArrayToObject(byte[] arrBytes)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream memStream = new MemoryStream(arrBytes))
        {
            memStream.Seek(0, SeekOrigin.Begin);
            return formatter.Deserialize(memStream);
        }
    }
}

public delegate void ExecutePacket(Packet toExecute);
