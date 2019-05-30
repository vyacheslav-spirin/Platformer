namespace Assets.Scripts.Match.Multiplayer.Protocol
{
    public enum PacketType : byte
    {
        MatchInfo,
        GetPlayerSlot,
        
        MatchState,

        CharacterState,

        TouchBlockDamage
    }
}