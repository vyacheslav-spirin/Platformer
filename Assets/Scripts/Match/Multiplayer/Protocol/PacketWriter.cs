using System.Text;
using UnityEngine;

namespace Assets.Scripts.Match.Multiplayer.Protocol
{
    public sealed class PacketWriter
    {
        public byte[] buffer;

        public int pos;

        public void Reset()
        {
            pos = 0;
        }

        public void WriteHeader(PacketType packetType)
        {
            buffer[pos++] = (byte) packetType;
        }

        public void WriteByte(byte value)
        {
            buffer[pos++] = value;
        }

        public void WriteUShort(ushort value)
        {
            buffer[pos++] = (byte) value;
            buffer[pos++] = (byte) (value >> 8);
        }

        public void WriteInt(int value)
        {
            buffer[pos++] = (byte) value;
            buffer[pos++] = (byte) (value >> 8);
            buffer[pos++] = (byte) (value >> 16);
            buffer[pos++] = (byte) (value >> 24);
        }

        public void Write3BytesInt(int value)
        {
            buffer[pos++] = (byte) value;
            buffer[pos++] = (byte) (value >> 8);
            buffer[pos++] = (byte) (value >> 16);
        }

        public void WriteString(string s)
        {
            var sLength = s.Length;

            if (s.Length > 255)
            {
                Debug.Log("This protocol not support strings with length > 255. Pack the first 255 chars!");

                sLength = 255;
            }

            buffer[pos++] = (byte) sLength;

            pos += Encoding.UTF8.GetBytes(s, 0, sLength, buffer, pos);
        }

        public unsafe void WriteFloat(float value)
        {
            var num = *(uint*) &value;
            buffer[pos++] = (byte) num;
            buffer[pos++] = (byte) (num >> 8);
            buffer[pos++] = (byte) (num >> 16);
            buffer[pos++] = (byte) (num >> 24);
        }

        public void WriteVector2(Vector2 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
        }
    }
}