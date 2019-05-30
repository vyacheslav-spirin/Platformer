namespace Assets.Scripts.Match.Multiplayer.Protocol
{
    public static class ProtocolConfig
    {
        public const ushort Port = 5151;

        public const byte MaxPlayers = 8;

        public const float TimeoutSeconds = 3f;

        public const int SendStatesFrequency = 10;
        public const float SendStateDelaySeconds = 1f / SendStatesFrequency;

        public const int PacketHeaderSize = 1;

        //Max state size is: (MTU-4) * 63. See state header and client state parser for details 
        public const int MaxStateSize = 1024 * 10;
    }
}