using Assets.Scripts.Match.Multiplayer.Protocol;

namespace Assets.Scripts.Match.Actors
{
    //ActorPointer used for fast access to specified actor
    public struct ActorPointer
    {
        public static readonly ActorPointer Null = new ActorPointer(0, 0);

        public readonly int id;

        public readonly ushort allocationId;

        public ActorPointer(int id, ushort allocationId)
        {
            this.id = id;

            this.allocationId = allocationId;
        }

        public ActorPointer(ActorPointer other)
        {
            this = other;
        }

        public bool Equals(ActorPointer obj)
        {
            return id == obj.id && allocationId == obj.allocationId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ActorPointer pointer && Equals(pointer);
        }

        //Generated. Not used
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = id.GetHashCode();
                hashCode = (hashCode * 397) ^ allocationId.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator !=(ActorPointer pointer1, ActorPointer pointer2)
        {
            return pointer1.id != pointer2.id || pointer1.allocationId != pointer2.allocationId;
        }

        public static bool operator ==(ActorPointer pointer1, ActorPointer pointer2)
        {
            return pointer1.id == pointer2.id && pointer1.allocationId == pointer2.allocationId;
        }

        public void Save(PacketWriter writer)
        {
            writer.Write3BytesInt(id);
            writer.WriteUShort(allocationId);
        }

        public static ActorPointer Load(PacketReader reader)
        {
            var id = reader.Read3BytesInt();
            var allocationId = reader.ReadUShort();
            
            return new ActorPointer(id, allocationId);
        }
    }
}