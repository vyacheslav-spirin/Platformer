using System;
using Assets.Scripts.Match.Actors;
using Assets.Scripts.Match.Multiplayer.Protocol;
using UnityEngine;

namespace Assets.Scripts.Match.Multiplayer
{
    public sealed class MultiplayerActorManager : ActorManager
    {
        private struct ActorSyncData
        {
            //for memory saving, data can be split to separate classes

            public float lastReceiveTime;

            //general
            public Vector2 pos1;
            public Vector2 pos2;

            //character
            public Vector2 lastVelocity;
            public int direction;
            public int moveDirection;
        }

        private readonly ActorSyncData[] actorsSyncData = new ActorSyncData[MaxActors];

        public MultiplayerActorManager(MatchManager matchManager) : base(matchManager)
        {
        }

        public void PackActors(PacketWriter packetWriter)
        {
            int currentWritePos;

            var actorTypesCountWriterPos = packetWriter.pos++;

            byte actorTypeCount = 0;

            for (var i = 0; i < firstTypeActors.Length; i++)
            {
                var actor = firstTypeActors[i];
                if (actor == null) continue;

                actorTypeCount++;

                packetWriter.WriteByte((byte)actor.ActorType);

                var actorCountWriterPos = packetWriter.pos;
                packetWriter.pos += 2;

                ushort actorCount = 0;

                do
                {
                    actorCount++;

                    actor.actorPointer.Save(packetWriter);

                    PackActor(packetWriter, actor);

                } while ((actor = actor.nextSameTypeActor) != null);

                currentWritePos = packetWriter.pos;
                packetWriter.pos = actorCountWriterPos;
                packetWriter.WriteUShort(actorCount);
                packetWriter.pos = currentWritePos;
            }

            currentWritePos = packetWriter.pos;
            packetWriter.pos = actorTypesCountWriterPos;
            packetWriter.WriteByte(actorTypeCount);
            packetWriter.pos = currentWritePos;
        }

        public void UnpackActors(PacketReader packetReader)
        {
            for (var i = 0; i < actors.Count; i++)
            {
                actors[i].isDeferredDestroying = true;
            }

            var actorTypeCount = packetReader.ReadByte();

            for (var i = 0; i < actorTypeCount; i++)
            {
                var actorType = (ActorType) packetReader.ReadByte();

                var actorCount = packetReader.ReadUShort();

                for (var j = 0; j < actorCount; j++)
                {
                    var actorPointer = ActorPointer.Load(packetReader);

                    UnpackActor(packetReader, actorType, actorPointer);
                }
            }

            for (var i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];

                if (actor.isDeferredDestroying)
                {
                    DestroyActor(actor.actorPointer);

                    //repeat destroying actor on this index. See actors.TryReplaceByLastElement(actor.indexInActorsArr) in DestroyActor
                    i--;
                }
            }
        }

        public void PackActor(PacketWriter packetWriter, Actor actor)
        {
            packetWriter.WriteVector2(actor.Pos);

            if (actor is CharacterActor characterActor)
            {
                if (actor.IsSimulatedLocally)
                {
                    packetWriter.WriteVector2(characterActor.Velocity);

                    packetWriter.WriteByte((byte)(characterActor.lookDirection + 1));

                    packetWriter.WriteByte((byte)(characterActor.lastMoveDirection + 1));
                }
                else
                {
                    var actorSyncData = actorsSyncData[actor.actorPointer.allocationId];

                    packetWriter.WriteVector2(actorSyncData.lastVelocity);

                    packetWriter.WriteByte((byte) (actorSyncData.direction + 1));

                    packetWriter.WriteByte((byte) (actorSyncData.moveDirection + 1));
                }

                packetWriter.WriteByte(characterActor.health);

                packetWriter.WriteByte((byte) characterActor.currentAction);
            }
        }

        public void UnpackActor(PacketReader packetReader, ActorType actorType, ActorPointer actorPointer)
        {
            var actor = TryGetActor(actorPointer);

            bool isCreated;

            if (actor == null)
            {
                isCreated = true;

                actor = actorsFastAccessBuffer[actorPointer.allocationId];
                if (actor != null)
                {
                    DestroyActor(actor.actorPointer);

                    actorsSyncData[actor.actorPointer.allocationId] = default;
                }

                actor = CreateActor(actorPointer, actorType);

                actor.IsSimulatedLocally = matchManager.gameModeManager.LocalPlayerControlledActorPointer == actorPointer;
            }
            else
            {
                isCreated = false;

                actor.isDeferredDestroying = false;

                if (actor.IsSimulatedLocally && actor.actorPointer != matchManager.gameModeManager.LocalPlayerControlledActorPointer)
                    actor.IsSimulatedLocally = false;
            }

            var actorSyncData = actorsSyncData[actorPointer.allocationId];

            actorSyncData.lastReceiveTime = Time.unscaledTime;

            var packetActorPos = packetReader.ReadVector2();

            if (isCreated) actor.Pos = packetActorPos;

            actorSyncData.pos1 = actor.Pos;
            actorSyncData.pos2 = packetActorPos;

            if (actorType == ActorType.Character)
            {
                actorSyncData.lastVelocity = packetReader.ReadVector2();

                actorSyncData.direction = packetReader.ReadByte() - 1;

                actorSyncData.moveDirection = packetReader.ReadByte() - 1;

                var characterActor = (CharacterActor) actor;

                characterActor.health = packetReader.ReadByte();

                var action = (CharacterActor.CharacterAction) packetReader.ReadByte();

                if (matchManager.gameModeManager.LocalPlayerControlledActorPointer != characterActor.actorPointer && characterActor.currentAction != action)
                {
                    characterActor.actionStartTime = Time.time;
                    characterActor.currentAction = action;
                }
            }

            actorsSyncData[actorPointer.allocationId] = actorSyncData;
        }

        public override void Update()
        {
            //Simple linear interpolation and prediction

            for (var i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];

                if (actor.actorPointer == matchManager.gameModeManager.LocalPlayerControlledActorPointer) continue;

                var actorSyncData = actorsSyncData[actor.actorPointer.allocationId];

                var elapsedTimeAfterReceiveLastState = Time.unscaledTime - actorSyncData.lastReceiveTime;
                var t = elapsedTimeAfterReceiveLastState / (ProtocolConfig.SendStateDelaySeconds * 1.1f);

                if (t > 1)
                {
                    if(!actor.IsSimulatedLocally) actor.IsSimulatedLocally = true;

                    actorSyncData.pos1 = actor.Pos;
                    actorSyncData.pos2 = actor.Pos;
                    actorsSyncData[actor.actorPointer.allocationId] = actorSyncData;

                    if (actor is CharacterActor characterActor)
                    {
                        characterActor.Velocity = actorSyncData.lastVelocity;

                        characterActor.lastMoveDirection = actorSyncData.moveDirection;
                    }
                }
                else
                {
                    if (actor.IsSimulatedLocally) actor.IsSimulatedLocally = false;

                    actor.Pos = Vector2.LerpUnclamped(actorSyncData.pos1, actorSyncData.pos2, t);

                    if (actor is CharacterActor characterActor)
                    {
                        characterActor.lookDirection = actorSyncData.direction;

                        characterActor.Velocity = actorSyncData.lastVelocity;

                        characterActor.lastMoveDirection = actorSyncData.moveDirection;
                    }
                }
            }

            base.Update();
        }
    }
}