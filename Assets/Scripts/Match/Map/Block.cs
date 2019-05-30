using UnityEngine;

namespace Assets.Scripts.Match.Map
{
    public class Block
    {
        public readonly GameObject gameObject;

        public readonly Collider collider;

        public readonly int typeId;

        public Block(int typeId, GameObject gameObject)
        {
            this.typeId = typeId;

            this.gameObject = gameObject;

            collider = gameObject.GetComponentInChildren<Collider>();
        }
    }
}