using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public enum EPacketType
{
    UNDEFINED,
    MESSAGE,
    MOVEMENTS,

}

[Serializable]
class PacketHeader
{
    public EPacketType type = EPacketType.UNDEFINED;
    public int size = 0;
}

[Serializable]
class Packet
{
    public PacketHeader header = new PacketHeader();
    public byte[] datas;

    public byte[] Serialize(EPacketType type, object obj)
    {
        header.type = type;

        BinaryFormatter formatter = new BinaryFormatter();
        using (var stream = new MemoryStream())
        {
            formatter.Serialize(stream, obj);

            datas = stream.ToArray();
            header.size = datas.Length;
        }

        using (var stream = new MemoryStream())
        {
            formatter.Serialize(stream, this);

            return stream.ToArray();
        }
    }

    public static Packet DeserializeHeader(byte[] packetAsByte, int size)
    {
        Packet packet = new Packet();

        BinaryFormatter formatter = new BinaryFormatter();

        using (var stream = new MemoryStream())
        {
            byte[] headerDatas = new byte[size];

            stream.Write(headerDatas, 0, size);

            packet.header = (PacketHeader)formatter.Deserialize(stream);
        }

        return packet;
    }

    public object FillObject()
    {
        var formatter = new BinaryFormatter();

        using (var stream = new MemoryStream())
        {
            stream.Write(datas, 0, header.size);
            return formatter.Deserialize(stream);
        }
    }
}
