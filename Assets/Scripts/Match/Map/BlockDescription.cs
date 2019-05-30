using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts.Match.Map
{
    [CreateAssetMenu(fileName = "id", menuName = "Block Description", order = 8)]
    [UsedImplicitly]
    public sealed class BlockDescription : ScriptableObject
    {
        public enum BlockBehaviour
        {
            Static,
            Destructible
        }

        public enum BlockCollisionBehaviour
        {
            Default,
            TopCollision
        }

        public enum BlockTouchBehavior
        {
            Default,
            Damage
        }

        public BlockBehaviour blockBehaviour;

        public BlockCollisionBehaviour blockCollisionBehaviour;

        public BlockTouchBehavior blockTouchBehavior;
    }
}