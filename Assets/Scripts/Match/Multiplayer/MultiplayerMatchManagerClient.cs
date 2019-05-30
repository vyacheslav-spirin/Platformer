using System;
using System.Net;
using Assets.Scripts.Match.Actors;
using Assets.Scripts.Match.Multiplayer.Protocol;
using Assets.Scripts.Network;
using UnityEngine;

namespace Assets.Scripts.Match.Multiplayer
{
    public partial class MultiplayerMatchManager
    {
        private float lastInitPacketSendTime;

        private int receivedStatePartCount;

        //Max state fragments is 64
        private ulong receivedStateMask;

        private float lastPacketReceiveTime;

        private int lastOutgoingPacketNumber;

        private void ClientProcessIncomingPacket(PacketType packetType, int length)
        {
            lastPacketReceiveTime = Time.time;

            //REQUIRED DATA VALIDATION !!! (not implemented for this example application)

            if (packetType == PacketType.GetPlayerSlot)
            {
                if (IsInitializationComplete) return;

                var freeSlot = packetReader.ReadInt();

                if (freeSlot == -1)
                {
                    serverIsFullWindow.SetActive(true);

                    return;
                }

                LocalPlayerSlot = freeSlot;

                return;
            }

            if (packetType == PacketType.MatchState)
            {
                ReadState(length);

                return;
            }
        }

        private void SendInitPacket()
        {
            lastInitPacketSendTime = Time.unscaledTime;

            packetWriter.Reset();
            packetWriter.WriteHeader(PacketType.GetPlayerSlot);
            udpNetwork.Send(packetWriter.buffer, 0, packetWriter.pos);
        }

        private void UpdateClient()
        {
            if (Time.time - lastPacketReceiveTime > ProtocolConfig.TimeoutSeconds)
            {
                serverTimeoutWindow.SetActive(true);
                serverIsFullWindow.SetActive(false);
            }
            else serverTimeoutWindow.SetActive(false);

            if (!IsInitializationComplete)
            {
                if (Time.unscaledTime - lastInitPacketSendTime > 0.5f) SendInitPacket();

                return;
            }

            if (Time.unscaledTime - lastStateSendTime > ProtocolConfig.SendStateDelaySeconds)
            {
                SendClientState();
            }
        }

        public void SendClientState()
        {
            lastStateSendTime = Time.unscaledTime;

            var actor = actorManager.TryGetActor(gameModeManager.LocalPlayerControlledActorPointer);
            if (actor == null) return;

            var multiplayerActorManager = (MultiplayerActorManager) actorManager;

            packetWriter.Reset();
            packetWriter.WriteHeader(PacketType.CharacterState);

            packetWriter.Write3BytesInt(++lastOutgoingPacketNumber);

            actor.actorPointer.Save(packetWriter);
            multiplayerActorManager.PackActor(packetWriter, actor);

            udpNetwork.Send(packetWriter.buffer, 0, packetWriter.pos);
        }

        //В идеале нужно гарантировать доставку пакета
        public void SendBlockTouchPacket(ActorPointer actorPointer)
        {
            packetWriter.Reset();

            packetWriter.WriteHeader(PacketType.TouchBlockDamage);

            packetWriter.Write3BytesInt(++lastOutgoingPacketNumber);

            actorPointer.Save(packetWriter);

            udpNetwork.Send(packetWriter.buffer, 0, packetWriter.pos);
        }

        private void ReadState(int length)
        {
            StateHeaderUtils.ReadHeader(packetReader, out var uniqueStateNumber, out var currentStatePartIndex, out var totalStatePartsCount);

            //ignore old packets
            if (uniqueStateNumber < lastStateNumber) return;

            //drop preview received data
            // For best user experience, can wait for the old buffer to fill up if there are 1-2 packets not received.
            // Then new state packets must store in another location
            if (uniqueStateNumber > lastStateNumber)
            {
                receivedStateMask = 0;

                lastStateNumber = uniqueStateNumber;
            }
            //ignore duplicated packet
            else if ((receivedStateMask & (1ul << currentStatePartIndex)) != 0) return;

            receivedStateMask |= 1ul << currentStatePartIndex;

            Buffer.BlockCopy(
                packetReader.buffer, packetReader.pos,
                stateBuffer, (UdpNetwork.Mtu - (ProtocolConfig.PacketHeaderSize + StateHeaderUtils.StateHeaderSize)) * currentStatePartIndex,
                length - StateHeaderUtils.StateHeaderSize);

            receivedStatePartCount++;

            if (receivedStatePartCount == totalStatePartsCount)
            {
                lastStateNumber++;

                receivedStatePartCount = 0;

                receivedStateMask = 0;

                ParseServerState();
            }
        }

        private void ParseServerState()
        {
            statePacketReader.Reset();

            //Read players

            for (byte i = 0; i < players.Length; i++)
            {
                var isPlayerExists = statePacketReader.ReadByte() == 1;

                if (isPlayerExists)
                {
                    var player = players[i];
                    if (player == null) player = players[i] = new Player(i, new IPEndPoint(0, 0));

                    player.Load(statePacketReader);
                }
                else
                {
                    players[i] = null;
                }
            }

            //Read game mode

            ((MultiplayerGameModeManager) gameModeManager).Unpack(statePacketReader);

            //Read actors

            ((MultiplayerActorManager) actorManager).UnpackActors(statePacketReader);

            //Read test data

            var testDataSize = statePacketReader.ReadInt();

            if (testDataSize != 7193)
            {
                Debug.Log("INVALID STATE LEN " + testDataSize);

                return;
            }

            for (var i = 0; i < testDataSize; i++)
            {
                var value = statePacketReader.ReadByte();

                if (value != (i % 255))
                {
                    Debug.LogError("INVALID STATE " + i);

                    return;
                }
            }
        }
    }
}