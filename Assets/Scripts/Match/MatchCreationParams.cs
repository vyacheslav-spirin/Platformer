using Assets.Scripts.Match.Multiplayer.Protocol;

namespace Assets.Scripts.Match
{
    public struct MatchCreationParams
    {
        public string mapName;

        public void Save(PacketWriter packetWriter)
        {
            packetWriter.WriteString(mapName);
        }

        public void Load(PacketReader packetReader)
        {
            mapName = packetReader.ReadString();
        }
    }
}