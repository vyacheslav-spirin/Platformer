using System.Text;
using UnityEngine;

namespace Assets.Scripts.Match.Multiplayer.Protocol
{
    public sealed class PacketReader
    {
        public byte[] buffer;

        public int pos;

        public void Reset()
        {
            pos = 0;
        }

        public void ReadHeader(out PacketType packetType)
        {
            packetType = (PacketType) buffer[pos++];
        }

        public byte ReadByte()
        {
            return buffer[pos++];
        }

        public ushort ReadUShort()
        {
            return (ushort) (buffer[pos++] | (buffer[pos++] << 8));
        }

        public int ReadInt()
        {
            return buffer[pos++] | (buffer[pos++] << 8) | (buffer[pos++] << 16) | (buffer[pos++] << 24);
        }

        public int Read3BytesInt()
        {
            return buffer[pos++] | (buffer[pos++] << 8) | (buffer[pos++] << 16);
        }

        public string ReadString()
        {
            var sLength = buffer[pos++];

            var s = Encoding.UTF8.GetString(buffer, pos, sLength);

            pos += sLength;

            return s;
        }

        public unsafe float ReadFloat()
        {
            var value = (uint) (buffer[pos++] | (buffer[pos++] << 8) | (buffer[pos++] << 16) | (buffer[pos++] << 24));

            return *(float*) &value;
        }

        public Vector2 ReadVector2()
        {
            var x = ReadFloat();
            var y = ReadFloat();

            return new Vector2(x, y);
        }
    }
}