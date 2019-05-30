using System.Collections.Generic;
using Assets.Scripts.Match.Map;
using UnityEngine;

namespace Assets.Scripts.Match.Actors
{
    public class CharacterActor : Actor
    {
        public enum CharacterAction
        {
            No,
            Kick
        }

        private const float Deceleration = 30f;
        private const float Acceleration = 12f;
        private const float MaxMoveSpeed = 2.5f;

        private const float JumpCooldown = 0.2f;

        private const float KickTime = 0.25f;
        private const float KickCooldown = 0.7f;

        public override ActorType ActorType => ActorType.Character;

        public readonly Collider collider;
        private readonly Rigidbody rigidBody;

        public readonly ActorCollisionDetector actorCollisionDetector;

        private readonly Transform visualTransform;
        private readonly Transform rigidBodyTransform;

//Disabled, as hashcode and equals not needed
#pragma warning disable 660, 661
        private struct BlockPos
#pragma warning restore 660,661
        {
            public readonly int x;
            public readonly int y;

            public BlockPos(int x, int y)
            {
                this.x = x;

                this.y = y;
            }

            public static bool operator == (BlockPos obj1, BlockPos obj2)
            {
                return obj1.x == obj2.x && obj1.y == obj2.y;
            }

            public static bool operator !=(BlockPos obj1, BlockPos obj2)
            {
                return obj1.x != obj2.x || obj1.y != obj2.y;
            }

            public static BlockPos operator -(BlockPos obj1, BlockPos obj2)
            {
                return new BlockPos(obj1.x - obj2.x, obj1.y - obj2.y);
            }

            public static BlockPos operator +(BlockPos obj1, BlockPos obj2)
            {
                return new BlockPos(obj1.x + obj2.x, obj1.y + obj2.y);
            }
        }

        private readonly List<BlockPos> ignoredBlocksCollision = new List<BlockPos>(6);

        private readonly Animator animator;

        public Vector2 Velocity
        {
            get => rigidBody.velocity;
            set => rigidBody.velocity = value;
        }

        private BlockPos ignoredBlocksCollisionMinPos;
        private BlockPos ignoredBlocksCollisionMaxPos;

        public CharacterAction currentAction = CharacterAction.No;
        private CharacterAction prevAction = CharacterAction.No;
        public float actionStartTime;

        private float actionCooldownStartTime;

        //public for multiplayer actor manager
        public int lookDirection;
        public int lastMoveDirection;

        private bool requireJump;

        private bool isGround;

        private float lastJumpTime = -JumpCooldown;

        private Vector2 bodyPos1, bodyPos2;

        private float horVelocity;

        public byte health = 100;

        public CharacterActor(ActorManager actorManager, ActorPointer actorPointer) : base(actorManager, actorPointer)
        {
            GameObject = Object.Instantiate(Resources.Load<GameObject>("Characters/CharacterVisual"), Vector3.zero, Quaternion.Euler(0, 180, 0));
            GameObject.name = "CharacterVisual " + actorPointer.id;

            var characterBody = Object.Instantiate(Resources.Load<GameObject>("Characters/CharacterBody"), Vector3.zero, Quaternion.Euler(0, 180, 0));
            characterBody.name = "CharacterBody " + actorPointer.id;

            collider = characterBody.GetComponent<Collider>();
            actorCollisionDetector = characterBody.GetComponent<ActorCollisionDetector>();
            rigidBody = characterBody.GetComponent<Rigidbody>();

            visualTransform = GameObject.transform;
            rigidBodyTransform = rigidBody.transform;

            animator = GameObject.GetComponent<Animator>();

            bodyPos1 = bodyPos2 = rigidBodyTransform.position;

            GetBlocksCollisionPos(out ignoredBlocksCollisionMinPos, out ignoredBlocksCollisionMaxPos);

            IgnoreBlocksCollision(ignoredBlocksCollisionMinPos, ignoredBlocksCollisionMaxPos);
        }

        private void IgnoreBlocksCollision(BlockPos min, BlockPos max)
        {
            var mapManager = actorManager.matchManager.mapManager;

            for (var i = min.x; i <= max.x; i++)
            {
                for (var j = min.y; j <= max.y; j++)
                {
                    var block = mapManager.GetBlock(i, j);

                    if (block != null &&
                        mapManager.GetBlockDescriptionByTypeId(block.typeId).blockCollisionBehaviour == BlockDescription.BlockCollisionBehaviour.TopCollision)
                    {
                        ignoredBlocksCollision.Add(new BlockPos(i, j));

                        Physics.IgnoreCollision(collider, block.collider, true);
                    }
                }
            }
        }

        private void GetBlocksCollisionPos(out BlockPos min, out BlockPos max)
        {
            var minX = (int) (collider.bounds.min.x - 0.3f);
            var maxX = (int) (collider.bounds.max.x + 0.3f);

            var minY = (int) (collider.bounds.min.y + 0.3f);
            var maxY = (int) (collider.bounds.max.y + 0.3f);

            min = new BlockPos(minX, minY);
            max = new BlockPos(maxX, maxY);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            Object.Destroy(rigidBody.gameObject);
        }

        protected override void OnLocalSimulationChange(bool isEnabled)
        {
            if (isEnabled)
            {
                rigidBody.isKinematic = false;
                rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            else
            {
                rigidBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rigidBody.isKinematic = true;
            }
        }

        protected override void PosChanged(Vector2 newPos)
        {
            bodyPos1 = bodyPos2 = newPos;

            rigidBodyTransform.position = new Vector3(newPos.x, newPos.y, 0);
        }

        public override void FixedUpdate()
        {
            if (!IsSimulatedLocally) return;

            rigidBody.MovePosition(rigidBodyTransform.position + new Vector3(Time.fixedDeltaTime * horVelocity, 0, 0));

            if (requireJump)
            {
                requireJump = false;

                rigidBody.AddForce(Vector3.up * 6, ForceMode.Impulse);
            }

            bodyPos1 = visualTransform.position;
            bodyPos2 = rigidBodyTransform.position;
        }



        public override void Update()
        {
            GetBlocksCollisionPos(out var min, out var max);

            if (ignoredBlocksCollisionMinPos != min || ignoredBlocksCollisionMaxPos != max)
            {
                //can optimized, with diff remove and diff add

                var mapManager = actorManager.matchManager.mapManager;

                foreach (var blockPos in ignoredBlocksCollision)
                {
                    var block = mapManager.GetBlock(blockPos.x, blockPos.y);

                    if(block == null) continue;

                    Physics.IgnoreCollision(collider, block.collider, false);
                }
                ignoredBlocksCollision.Clear();

                ignoredBlocksCollisionMinPos = min;
                ignoredBlocksCollisionMaxPos = max;

                IgnoreBlocksCollision(ignoredBlocksCollisionMinPos, ignoredBlocksCollisionMaxPos);
            }


            var newIsGround = Physics.Raycast(rigidBodyTransform.position + new Vector3(0, 0.1f, 0), -Vector3.up, 0.13f);

            if(newIsGround && !isGround)
            {
                animator.SetTrigger("Land");
            }
            else if (!newIsGround && isGround)
            {
                animator.SetTrigger("Jump");
            }

            isGround = newIsGround;

            animator.SetBool("IsGround", isGround);

            var target = Quaternion.Euler(0, lookDirection > 0 ? 90 : 270, 0);
            visualTransform.rotation = Quaternion.Slerp(visualTransform.rotation, target, Time.deltaTime * 20f);

            UpdateVelocity();

            UpdateCurrentAction();

            if (!IsSimulatedLocally)
            {
                rigidBody.MovePosition(visualTransform.position);

                return;
            }

            visualTransform.position = Vector3.Lerp(bodyPos1, bodyPos2, Main.FixedTimeLerpValue);
        }

        private void UpdateVelocity()
        {
            if (lastMoveDirection > 0)
            {
                if (horVelocity < 0)
                {
                    horVelocity += Time.deltaTime * Deceleration;

                    if (horVelocity > 0) horVelocity = 0;
                }
                else
                {
                    horVelocity += Time.deltaTime * Acceleration;

                    if (horVelocity > MaxMoveSpeed) horVelocity = MaxMoveSpeed;
                }
            }
            else if (lastMoveDirection < 0)
            {
                if (horVelocity > 0)
                {
                    horVelocity -= Time.deltaTime * Deceleration;

                    if (horVelocity < 0) horVelocity = 0;
                }
                else
                {
                    horVelocity -= Time.deltaTime * Acceleration;

                    if (horVelocity < -MaxMoveSpeed) horVelocity = -MaxMoveSpeed;
                }
            }
            else
            {
                if (horVelocity > 0)
                {
                    horVelocity -= Time.deltaTime * Deceleration;

                    if (horVelocity < 0) horVelocity = 0;
                }
                else if (horVelocity < 0)
                {
                    horVelocity += Time.deltaTime * Deceleration;

                    if (horVelocity > 0) horVelocity = 0;
                }
            }

            animator.SetFloat("Velocity", Mathf.Abs(horVelocity) / MaxMoveSpeed);

            lastMoveDirection = 0;
        }

        private void UpdateCurrentAction()
        {
            if (IsSimulatedLocally)
            {
                if (currentAction == CharacterAction.Kick)
                {
                    if (actorManager.matchManager.MatchTime - actionStartTime > KickTime)
                    {
                        currentAction = CharacterAction.No;

                        actionCooldownStartTime = Time.time;
                    }
                }
            }

            animator.SetInteger("Action", (int) currentAction);

            if (prevAction != currentAction)
            {
                if (prevAction == CharacterAction.Kick)
                {
                    OnKick();
                }
            }

            prevAction = currentAction;
        }

        private void OnKick()
        {
            actorManager.matchManager.gameModeManager.ProcessKickAction(this);
        }


        //Controls

        public void Move(int direction)
        {
            if(currentAction == CharacterAction.No) lookDirection = direction;

            lastMoveDirection = direction;
        }

        public void Jump()
        {
            if (isGround && Time.time - lastJumpTime > JumpCooldown)
            {
                requireJump = true;

                lastJumpTime = Time.time;
            }
        }

        public void Kick()
        {
            if (currentAction != CharacterAction.No) return;

            if (Time.time - actionCooldownStartTime <= KickCooldown && actionCooldownStartTime > 0) return;

            currentAction = CharacterAction.Kick;

            actionStartTime = actorManager.matchManager.MatchTime;
        }
    }
}