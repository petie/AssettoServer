﻿using System;

namespace AssettoServer.Network.Packets.Outgoing;

public readonly struct CSPPositionUpdate : IOutgoingNetworkPacket
{
    public const string CustomUpdateFormat = @"packet:
  group:
    sessionID
    pakSequenceID
    timestamp
    ping: ushort
    pos: float3
    rotation: float3
    velocity: float3
    tyreAngularSpeedFL: byte, -100, tyreAngularSpeed
    tyreAngularSpeedFR: byte, -100, tyreAngularSpeed
    tyreAngularSpeedRL: byte, -100, tyreAngularSpeed
    tyreAngularSpeedRR: byte, -100, tyreAngularSpeed
    steerAngle: byte, -127
    wheelAngle: byte, -127, /2
    engineRPM: ushort
    gear: byte
    statusBytes
    gas: byte, /255
    performanceDelta: short";
    
    public readonly ArraySegment<PositionUpdateOut> Updates;

    public CSPPositionUpdate(ArraySegment<PositionUpdateOut> updates)
    {
        Updates = updates;
    }

    public void ToWriter(ref PacketWriter writer)
    {
        writer.Write<byte>(0xAB);
        writer.Write<byte>(0x03);
        writer.Write((byte)Updates.Count);
        for (int i = 0; i < Updates.Count; i++)
        {
            Updates[i].ToWriterCustom(ref writer);
        }
    }
}
