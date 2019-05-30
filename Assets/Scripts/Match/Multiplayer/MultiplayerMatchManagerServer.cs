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
        

        private void ServerProcessIncomingPacket(PacketType packetType, int length)
        {
            //REQUIRED DATA VALIDATION !!! (not implemented for this example application)

            if (packetType == PacketType.CharacterState)
            {
                var player = GetLastIncomingPacketPlayerAndUpdateReceiveTime();
                if (player == null) return;

                var incomingStateNumber = packetReader.Read3BytesInt();

                if (player.lastReceivedPacketNumber >= incomingStateNumber) return;

                player.lastReceivedPacketNumber = incomingStateNumber;

                var actorPointer = ActorPointer.Load(packetReader);

                var multiplayerGameModeManager = (MultiplayerGameModeManager) gameModeManager;

                if (multiplayerGameModeManager.GetPlayerActorPointer(player) != actorPointer) return;

                var actor = actorManager.TryGetActor(actorPointer);
                if (actor == null) return;

                var multiplayerActorManager = (MultiplayerActorManager) actorManager;

                multiplayerActorManager.UnpackActor(packetReader, actor.ActorType, actorPointer);
            }

            if (packetType == PacketType.TouchBlockDamage)
            {
                var player = GetLastIncomingPacketPlayerAndUpdateReceiveTime();
                if (player == null) return;

                var incomingStateNumber = packetReader.Read3BytesInt();

                if (player.lastReceivedPacketNumber >= incomingStateNumber) return;

                player.lastReceivedPacketNumber = incomingStateNumber;

                var actorPointer = ActorPointer.Load(packetReader);

                var multiplayerGameModeManager = (MultiplayerGameModeManager)gameModeManager;

                if (multiplayerGameModeManager.GetPlayerActorPointer(player) != actorPointer) return;

                var actor = actorManager.TryGetActor(actorPointer) as CharacterActor;
                if (actor == null) return;

                gameModeManager.ProcessActorDeath(actor);
            }

            if (packetType == PacketType.MatchInfo)
            {
                packetWriter.Reset();
                packetWriter.WriteHeader(PacketType.MatchInfo);
                matchCreationParams.Save(packetWriter);
                udpNetwork.SendToLastClient(packetWriter.buffer, 0, packetWriter.pos);

                return;
            }

            if (packetType == PacketType.GetPlayerSlot)
            {
                var freeSlotIndex = -1;

                for (var i = 0; i < players.Length; i++)
                {
                    var player = players[i];

                    if (player == null)
                    {
                        if(freeSlotIndex == -1) freeSlotIndex = i;
                        
                        continue;
                    }

                    //Simple address check to player validation

                    if (player.remoteIpEndPoint.Equals(udpNetwork.lastReadRemoteIpEndPoint))
                    {
                        packetWriter.Reset();
                        packetWriter.WriteHeader(PacketType.GetPlayerSlot);
                        packetWriter.WriteInt(i);
                        udpNetwork.SendToLastClient(packetWriter.buffer, 0, packetWriter.pos);

                        return;
                    }
                }

                Player newPlayer = null;

                if (freeSlotIndex > -1)
                {
                    players[freeSlotIndex] = newPlayer = new Player((byte) freeSlotIndex,
                        new IPEndPoint(udpNetwork.lastReadRemoteIpEndPoint.Address, udpNetwork.lastReadRemoteIpEndPoint.Port));
                }

                packetWriter.Reset();
                packetWriter.WriteHeader(PacketType.GetPlayerSlot);
                packetWriter.WriteInt(freeSlotIndex);
                udpNetwork.SendToLastClient(packetWriter.buffer, 0, packetWriter.pos);

                if(newPlayer != null) OnPlayerConnect(newPlayer);

                return;
            }
        }

        private void UpdateServer()
        {
            for (var i = 0; i < players.Length; i++)
            {
                var player = players[i];
                if (player == null) continue;

                if (player.lastPacketReceiveTime >= 0 && Time.unscaledTime - player.lastPacketReceiveTime > ProtocolConfig.TimeoutSeconds)
                {
                    players[i] = null;

                    OnPlayerDisconnect(player);
                }
            }

            if (Time.unscaledTime - lastStateSendTime > ProtocolConfig.SendStateDelaySeconds)
            {
                SendServerState();
            }
        }

        private void SendServerState()
        {
            lastStateSendTime = Time.unscaledTime;

            statePacketWriter.Reset();

            //Pack players
            
            foreach (var player in players)
            {
                if (player == null)
                {
                    statePacketWriter.WriteByte(0);

                    continue;
                }

                statePacketWriter.WriteByte(1);

                player.Save(statePacketWriter);
            }

            //Pack game mode

            ((MultiplayerGameModeManager) gameModeManager).Pack(statePacketWriter);
            
            //Pack actors

            ((MultiplayerActorManager) actorManager).PackActors(statePacketWriter);

            //Pack test data

            const int testDataSize = 7193;

            statePacketWriter.WriteInt(testDataSize);

            var testArr = new byte[testDataSize];

            for (var i = 0; i < testArr.Length; i++)
            {
                testArr[i] = (byte)(i % 255);
            }

            Buffer.BlockCopy(testArr, 0, statePacketWriter.buffer, statePacketWriter.pos, testArr.Length);

            statePacketWriter.pos += testArr.Length;

            //Here can add state compression (and decompression on client side)

            //Insert packet types and state headers

            //Add check currentStateNumber! Max is 20 bit value. Force restart match if overflow
            var currentStateNumber = ++lastStateNumber;

            var statePartCount = statePacketWriter.pos / UdpNetwork.Mtu;
            if (statePacketWriter.pos % UdpNetwork.Mtu > 0) statePartCount++;

            const int insertFragmentSize = ProtocolConfig.PacketHeaderSize + StateHeaderUtils.StateHeaderSize;

            var statePartHeadersInsertCount = statePartCount;
            var totalStateOffset = statePartHeadersInsertCount * insertFragmentSize;

            var fullStateEndPos = statePacketWriter.pos + totalStateOffset;

            statePartCount = fullStateEndPos / UdpNetwork.Mtu;
            if (fullStateEndPos % UdpNetwork.Mtu > 0) statePartCount++;

            var statePartIndex = 0;

            //Can optimize with reverse inserting
            for (var i = 0; i < statePartCount; i++)
            {
                var startPos = i * UdpNetwork.Mtu;

                Buffer.BlockCopy(stateBuffer, startPos, stateBuffer, startPos + insertFragmentSize, fullStateEndPos - startPos - insertFragmentSize);

                statePacketWriter.pos = startPos;
                statePacketWriter.WriteHeader(PacketType.MatchState);
                StateHeaderUtils.WriteHeader(statePacketWriter, currentStateNumber, statePartIndex++, statePartCount);
            }

            //Send state to all

            foreach (var player in players)
            {
                if(player == null || player.lastPacketReceiveTime < 0) continue;
            
                udpNetwork.ApplyEndPointToWriter(player.remoteIpEndPoint);
            
                var bytesWasWritten = 0;
                var bytesToWriteRemaining = fullStateEndPos;
            
                do
                {
                    var lengthToWrite = bytesToWriteRemaining > UdpNetwork.Mtu ? UdpNetwork.Mtu : bytesToWriteRemaining;

                    udpNetwork.Send(stateBuffer, bytesWasWritten, lengthToWrite);
            
                    bytesToWriteRemaining -= lengthToWrite;
                    bytesWasWritten += lengthToWrite;
            
                } while (bytesToWriteRemaining > 0);
            }
        }
    }
}