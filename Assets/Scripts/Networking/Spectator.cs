using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

using UnityEngine;
using UnityEngine.Events;


public class Spectator : Client
{
    protected override void InterpretPacket(Packet toInterpret)
    {
        switch (toInterpret.header.type)
        {
            case EPacketType.MOVE_VALIDITY:
                break;

            default:
                base.InterpretPacket(toInterpret);
                break;
        }
    }
}
