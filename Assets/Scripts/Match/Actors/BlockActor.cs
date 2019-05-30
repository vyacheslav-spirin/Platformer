using System;
using UnityEngine;

namespace Assets.Scripts.Match.Actors
{
    public sealed class BlockActor : Actor
    {
        public override ActorType ActorType => ActorType.Block;

        public int BlockTypeId { get; private set; } = -1;

        public Collider Collider { get; private set; }

        private int blockPosX;
        private int blockPosY;

        public BlockActor(ActorManager actorManager, ActorPointer actorPointer) : base(actorManager, actorPointer)
        {
        }

        public void InitBlock(int typeId, int posX, int posY)
        {
            if(BlockTypeId >= 0) throw new Exception("Block actor is already initialized!");

            BlockTypeId = typeId;

            blockPosX = posX;
            blockPosY = posY;

            var mapManager = actorManager.matchManager.mapManager;

            GameObject = mapManager.CreateBlockGameObject(typeId, posX, posY);
            GameObject.transform.position = new Vector3(posX, posY, 0);

            Collider = GameObject.GetComponentInChildren<Collider>();

            mapManager.AddBlockActor(this);
        }

        public override void OnDestroy()
        {
            actorManager.matchManager.mapManager.RemoveBlockActor(this);
        }

        protected override void PosChanged(Vector2 newPos)
        {
            if (BlockTypeId < 0) return;

            if(blockPosX != (int) newPos.x || blockPosY != (int) newPos.y)
                throw new Exception("Could not change block actor pos after his initialization!");
        }
    }
}