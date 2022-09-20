using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Assets.Scripts
{
    public enum EPacketType
    {
        UNDEFINED,
        PARTY_READY,
        MOVEMENTS
    }

    class Packet
    {
        EPacketType packetType = EPacketType.UNDEFINED;
        int size = 0;
        byte[] datas;

        public byte[] Serialize()
        {
            return null;
        }
    }
}
