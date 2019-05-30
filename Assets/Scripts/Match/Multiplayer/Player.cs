using System.Net;
using Assets.Scripts.Match.Multiplayer.Protocol;
using UnityEngine;

namespace Assets.Scripts.Match.Multiplayer
{
    public sealed class Player
    {
        public readonly byte slotId;

        public readonly IPEndPoint remoteIpEndPoint;

        public float lastPacketReceiveTime;

        public int lastReceivedPacketNumber;

        public float lastActorLifeTime;

        public ushort kills;
        public ushort deaths;

        public Player(byte slotId, IPEndPoint remoteIpEndPoint)
        {
            this.slotId = slotId;

            this.remoteIpEndPoint = remoteIpEndPoint;

            lastPacketReceiveTime = Time.unscaledTime;
        }

        public void Save(PacketWriter writer)
        {
            writer.WriteUShort(kills);

            writer.WriteUShort(deaths);
        }

        public void Load(PacketReader reader)
        {
            kills = reader.ReadUShort();

            deaths = reader.ReadUShort();
        }
    }
}