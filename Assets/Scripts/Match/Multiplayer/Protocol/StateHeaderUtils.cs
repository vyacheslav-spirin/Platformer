namespace Assets.Scripts.Match.Multiplayer.Protocol
{
    public static class StateHeaderUtils
    {
        // State header bits description (4 bytes):
        // IIIIIICC CCCCUUUU UUUUUUUU UUUUUUUU
        // I - current state part index
        // C - total state parts count
        // U - unique state number

        public const int StateHeaderSize = 4;

        private const int StatePartMask = 63; //6 bits
        private const int UniqueStateNumberMask = 1048575; //20 bits

        public static void WriteHeader(PacketWriter writer, int uniqueStateNumber, int currentStatePartIndex, int totalStatePartsCount)
        {
            writer.WriteByte((byte) ((currentStatePartIndex << 2) | (totalStatePartsCount >> 4)));
            writer.WriteByte((byte) ((totalStatePartsCount << 4) | (uniqueStateNumber >> 16)));
            writer.WriteByte((byte) (uniqueStateNumber >> 8));
            writer.WriteByte((byte) uniqueStateNumber);
        }

        public static void ReadHeader(PacketReader reader, out int uniqueStateNumber, out int currentStatePartIndex, out int totalStatePartsCount)
        {
            var b1 = reader.ReadByte();
            var b2 = reader.ReadByte();
            var b3 = reader.ReadByte();
            var b4 = reader.ReadByte();

            currentStatePartIndex = b1 >> 2;
            totalStatePartsCount = (b1 << 4) | (b2 >> 4);
            totalStatePartsCount &= StatePartMask;
            uniqueStateNumber = (b2 << 16) | (b3 << 8) | b4;
            uniqueStateNumber &= UniqueStateNumberMask;
        }
    }
}