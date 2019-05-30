using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts.Match.Map
{
    [CreateAssetMenu(fileName = "MapDescription", menuName = "Map Description", order = 8)]
    [UsedImplicitly]
    public sealed class MapDescription : ScriptableObject
    {
        [Serializable]
        public struct Block
        {
            public int typeId;

            public int posX;
            public int posY;
        }

        public Block[] blocks;

        public Vector2[] spawns;
    }
}