using UnityEngine;

namespace Assets.Scripts.Match.Actors
{
    public sealed class BulletActor : Actor
    {
        private const float MaxLifeTime = 5f;

        private const float MoveSpeed = 5f;

        public override ActorType ActorType => ActorType.Bullet;

        public readonly Collider collider;

        public readonly ActorCollisionDetector actorCollisionDetector;

        private readonly Rigidbody rigidBody;

        private readonly float createTime;

        //temp solution
        public int owner;

        public int Direction { get; private set; }

        public BulletActor(ActorManager actorManager, ActorPointer actorPointer) : base(actorManager, actorPointer)
        {
            //Add pool system for best performance

            GameObject = Object.Instantiate(Resources.Load<GameObject>("Fireball"), Vector3.zero, Quaternion.Euler(0, 90, 0));
            GameObject.name = "BulletActor " + actorPointer.id;

            collider = GameObject.GetComponent<Collider>();

            actorCollisionDetector = GameObject.GetComponent<ActorCollisionDetector>();

            rigidBody = GameObject.GetComponent<Rigidbody>();

            createTime = Time.time;
        }

        protected override void OnLocalSimulationChange(bool isEnabled)
        {
            rigidBody.isKinematic = !isEnabled;

            collider.enabled = isEnabled;
        }

        public void UpdateDirection(int newDirection)
        {
            Direction = newDirection;

            GameObject.transform.rotation = Quaternion.Euler(0, Direction > 0 ? 90 : 270, 0);
        }

        public override void Update()
        {
            if(IsSimulatedLocally)
            {
                var t = GameObject.transform;

                t.position = t.position + new Vector3(Direction * MoveSpeed * Time.deltaTime, 0, 0);

                if (Time.time - createTime > MaxLifeTime) actorManager.matchManager.gameModeManager.ProcessLifeTimeDestroy(this);

                actorCollisionDetector.CleanUpDestroyedTouchColliders();

                foreach (var touchCollider in actorCollisionDetector.touchColliders)
                {
                    actorManager.matchManager.gameModeManager.ProcessBulletHit(this, touchCollider);
                }
            }
        }
    }
}