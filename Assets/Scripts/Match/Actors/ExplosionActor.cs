using UnityEngine;

namespace Assets.Scripts.Match.Actors
{
    public sealed class ExplosionActor : Actor
    {
        private const float LifeTime = 10f;

        public override ActorType ActorType => ActorType.Explosion;

        private readonly float createTime;

        public ExplosionActor(ActorManager actorManager, ActorPointer actorPointer) : base(actorManager, actorPointer)
        {
            //Add pool system for best performance

            GameObject = Object.Instantiate(Resources.Load<GameObject>("Explosion"), Vector3.zero, Quaternion.Euler(0, 90, 0));
            GameObject.name = "BulletActor " + actorPointer.id;

            createTime = Time.time;
        }

        public void UpdateDirection(int newDirection)
        {
            GameObject.transform.rotation = Quaternion.Euler(0, newDirection > 0 ? 90 : 270, 0);
        }

        public override void Update()
        {
            if (Time.time - createTime > LifeTime) actorManager.matchManager.gameModeManager.ProcessLifeTimeDestroy(this);
        }
    }
}