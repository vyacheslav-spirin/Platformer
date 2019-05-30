using Assets.Scripts.Match.Actors;
using Assets.Scripts.Match.Map;
using UnityEngine;

namespace Assets.Scripts.Match
{
    public class GameModeManager
    {
        protected readonly MatchManager matchManager;

        public ActorPointer LocalPlayerControlledActorPointer { get; protected set; }

        public GameModeManager(MatchManager matchManager)
        {
            this.matchManager = matchManager;
        }

        public virtual void Update()
        {
            UpdateBlockTouchDamage();
        }

        public virtual void ProcessActorDeath(CharacterActor actor)
        {
            matchManager.actorManager.DestroyActor(actor.actorPointer);
        }

        protected virtual void ProcessBlockTouchDamage(CharacterActor characterActor)
        {
            ProcessActorDeath(characterActor);
        }

        protected void UpdateBlockTouchDamage()
        {
            if (Time.frameCount % 2 == 0)
            {
                var characterActor = (CharacterActor) matchManager.actorManager.firstTypeActors[(byte) ActorType.Character];
                if (characterActor == null) return;

                do
                {
                    var mapManager = matchManager.mapManager;

                    characterActor.actorCollisionDetector.CleanUpDestroyedTouchColliders();

                    foreach (var touchCollider in characterActor.actorCollisionDetector.touchColliders)
                    {
                        //check collider parent is block

                        var parent = touchCollider.transform.parent;
                        if(parent == null) continue;

                        var pos = parent.transform.position;

                        var block = mapManager.GetBlock((int) pos.x, (int) pos.y);
                        if(block == null || block.collider != touchCollider) continue;

                        var blockDescription = mapManager.GetBlockDescriptionByTypeId(block.typeId);

                        if(blockDescription.blockTouchBehavior != BlockDescription.BlockTouchBehavior.Damage) continue;

                        ProcessBlockTouchDamage(characterActor);

                        return; //max one damage
                    }

                } while ((characterActor = (CharacterActor) characterActor.nextSameTypeActor) != null);
            }
        }

        public virtual void ProcessLifeTimeDestroy(Actor actor)
        {
            matchManager.actorManager.DestroyActor(actor.actorPointer);
        }

        public virtual void ProcessBulletHit(BulletActor actor, Collider collider)
        {
            var actorManager = matchManager.actorManager;

            var explosion = (ExplosionActor) actorManager.CreateActor(ActorType.Explosion);

            explosion.Pos = actor.Pos;
            explosion.UpdateDirection(actor.Direction);

            actorManager.DestroyActor(actor.actorPointer);

            var characterActor = (CharacterActor) matchManager.actorManager.firstTypeActors[(byte) ActorType.Character];
            if (characterActor == null) return;

            do
            {
                if (characterActor.collider == collider)
                {
                    const int bulletDamage = 30;

                    if(characterActor.health <= bulletDamage) actorManager.DestroyActor(characterActor.actorPointer);
                    else
                    {
                        characterActor.health -= bulletDamage;
                    }

                    return;
                }

            } while ((characterActor = (CharacterActor)characterActor.nextSameTypeActor) != null);


            var blockActor = (BlockActor) matchManager.actorManager.firstTypeActors[(byte) ActorType.Block];
            if (blockActor == null) return;

            do
            {
                if (blockActor.Collider == collider)
                {
                    actorManager.DestroyActor(blockActor.actorPointer);

                    return;
                }

            } while ((blockActor = (BlockActor) blockActor.nextSameTypeActor) != null);
        }

        public virtual void ProcessKickAction(CharacterActor characterActor)
        {
            var bullet = (BulletActor) matchManager.actorManager.CreateActor(ActorType.Bullet);

            bullet.UpdateDirection(characterActor.lookDirection);

            bullet.Pos = characterActor.Pos + new Vector2(characterActor.lookDirection > 0 ? 0.7f : -0.7f, 0.5f);

            Physics.IgnoreCollision(characterActor.collider, bullet.collider, true);
        }
    }
}