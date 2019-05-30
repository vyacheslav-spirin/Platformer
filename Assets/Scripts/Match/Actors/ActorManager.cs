using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Scripts.Match.Actors
{
    public class ActorManager
    {
        protected const int MaxActors = 300;

        public readonly MatchManager matchManager;

        protected readonly FastBag<Actor> actors = new FastBag<Actor>(MaxActors);
        protected readonly Actor[] actorsFastAccessBuffer = new Actor[MaxActors];
        private readonly Stack<ushort> actorsFastAccessArrayFreeIndexes = new Stack<ushort>(MaxActors);

        public readonly Actor[] firstTypeActors;

        private int actorIdSequence;

        private ushort allocationIdSequence;

        public ActorManager(MatchManager matchManager)
        {
            this.matchManager = matchManager;

            firstTypeActors = new Actor[Enum.GetValues(typeof(ActorType)).Length];
        }

        public virtual void Update()
        {
            for(var i=0;i<actors.Count;i++)
            {
                actors[i].Update();
            }
        }

        public virtual void FixedUpdate()
        {
            for (var i = 0; i < actors.Count; i++)
            {
                actors[i].FixedUpdate();
            }
        }

        public Actor CreateActor(ActorType actorType)
        {
            ushort allocationId;

            if (actorsFastAccessArrayFreeIndexes.Count > 0) allocationId = actorsFastAccessArrayFreeIndexes.Pop();
            else
            {
                allocationId = allocationIdSequence++;

                if(allocationId >= MaxActors) throw new Exception($"Maximum number of actors exceeded! Max: {MaxActors}");
            }

            return CreateActor(new ActorPointer(++actorIdSequence, allocationId), actorType);
        }

        protected Actor CreateActor(ActorPointer actorPointer, ActorType actorType)
        {
            if(actorPointer.allocationId >= MaxActors) throw new Exception(
                $"Invalid allocation id! Value: {actorPointer.allocationId} Max value: {MaxActors}");

            if(actorsFastAccessBuffer[actorPointer.allocationId] != null) throw new Exception(
                $"Actor with allocation id {actorPointer.allocationId} already exists!");

            Actor createdActor;

            switch (actorType)
            {
                case ActorType.Character:

                    createdActor = new CharacterActor(this, actorPointer);

                    break;
                case ActorType.Bullet:

                    createdActor = new BulletActor(this, actorPointer);

                    break;
                case ActorType.Explosion:

                    createdActor = new ExplosionActor(this, actorPointer);

                    break;
                case ActorType.Block:

                    createdActor = new BlockActor(this, actorPointer);

                    break;
                default:
                    throw new Exception($"Invalid {nameof(ActorType)}!");
            }

            createdActor.indexInActorsArr = actors.Add(createdActor);

            actorsFastAccessBuffer[actorPointer.allocationId] = createdActor;

            var firstActor = firstTypeActors[(byte) actorType];
            if (firstActor == null) firstTypeActors[(byte) actorType] = createdActor;
            else
            {
                var lastTypeActor = firstActor;

                while (lastTypeActor.nextSameTypeActor != null)
                {
                    lastTypeActor = lastTypeActor.nextSameTypeActor;
                }

                lastTypeActor.nextSameTypeActor = createdActor;
                createdActor.prevSameTypeActor = lastTypeActor;
            }

            return createdActor;
        }

        public void DestroyActor(ActorPointer actorPointer)
        {
            if (actorPointer.allocationId >= MaxActors) throw new Exception(
                $"Invalid allocation id! Value: {actorPointer.allocationId} Max value: {MaxActors}");

            var actor = actorsFastAccessBuffer[actorPointer.allocationId];

            if (actor == null)
            {
                Debug.LogWarning("Could not destroy actor! Reason: not found");

                return;
            }

            actorsFastAccessBuffer[actorPointer.allocationId] = null;
            actorsFastAccessArrayFreeIndexes.Push(actorPointer.allocationId);

            if (actors.TryReplaceByLastElement(actor.indexInActorsArr)) actors[actor.indexInActorsArr].indexInActorsArr = actor.indexInActorsArr;
            actor.indexInActorsArr = -1;

            if (actor.prevSameTypeActor == null) firstTypeActors[(byte) actor.ActorType] = actor.nextSameTypeActor;
            else
            {
                actor.prevSameTypeActor.nextSameTypeActor = actor.nextSameTypeActor;
            }

            if (actor.nextSameTypeActor != null)
            {
                actor.nextSameTypeActor.prevSameTypeActor = actor.prevSameTypeActor;
            }

            actor.OnDestroy();

            if (actor.GameObject != null) Object.Destroy(actor.GameObject);
        }

        public Actor TryGetActor(ActorPointer actorPointer)
        {
            if (actorPointer.allocationId >= MaxActors) return null;

            var actor = actorsFastAccessBuffer[actorPointer.allocationId];
            if (actor == null) return null;

            if (actor.actorPointer != actorPointer) return null;

            return actor;
        }
    }
}