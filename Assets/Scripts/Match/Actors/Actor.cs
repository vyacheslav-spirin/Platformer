using UnityEngine;

namespace Assets.Scripts.Match.Actors
{
    public abstract class Actor
    {
        public readonly ActorManager actorManager;

        public readonly ActorPointer actorPointer;

        //Simple solutions for fast destroying in ActorManager, fast search actors with same type (preferably make it private)
        public int indexInActorsArr;
        //can replaced to ActorPointer;
        public Actor nextSameTypeActor;
        public Actor prevSameTypeActor;

        //Used in internal actor manager logic (preferably make it private)
        public bool isDeferredDestroying;

        public bool IsSimulatedLocally
        {
            get => isSimulatedLocally;
            set
            {
                if (isSimulatedLocally == value) return;

                isSimulatedLocally = value;

                OnLocalSimulationChange(isSimulatedLocally);
            }
        }

        private bool isSimulatedLocally = true;

        protected Actor(ActorManager actorManager, ActorPointer actorPointer)
        {
            this.actorManager = actorManager;

            this.actorPointer = actorPointer;
        }

        public GameObject GameObject { get; protected set; }

        public abstract ActorType ActorType { get; }

        public Vector2 Pos
        {
            get
            {
                var pos = GameObject.transform.position;

                return new Vector2(pos.x, pos.y);
            }
            set
            {
                GameObject.transform.position = new Vector3(value.x, value.y, 0);

                PosChanged(value);
            }
        }

        protected virtual void PosChanged(Vector2 newPos)
        {
        }

        public virtual void OnDestroy()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void FixedUpdate()
        {
        }

        protected virtual void OnLocalSimulationChange(bool isEnabled)
        {
        }
    }
}