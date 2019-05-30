using System;
using System.Linq;
using System.Net;
using Assets.Scripts.Match.Actors;
using Assets.Scripts.Match.Multiplayer.Protocol;
using Assets.Scripts.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Match.Multiplayer
{
    public sealed partial class MultiplayerMatchManager : MatchManager
    {
        private readonly GameObject serverTimeoutWindow;
        private readonly GameObject serverIsFullWindow;

        private readonly Text killsText;
        private readonly Text deathsText;
        private readonly Text playerHealthText;
        private readonly Text playersOnlineText;

        private readonly UdpNetwork udpNetwork;

        private readonly PacketType maxPacketTypeValue;

        private readonly byte[] stateBuffer = new byte[ProtocolConfig.MaxStateSize];
        private readonly PacketWriter statePacketWriter;
        private readonly PacketReader statePacketReader;

        public readonly Player[] players = new Player[ProtocolConfig.MaxPlayers];

        public readonly bool isHost;

        private readonly PacketWriter packetWriter;
        private readonly PacketReader packetReader;

        public bool IsInitializationComplete => LocalPlayerSlot >= 0;

        public int LocalPlayerSlot { get; private set; } = -1;

        private int lastStateNumber;

        private float lastStateSendTime;

        public MultiplayerMatchManager(MatchCreationParams matchCreationParams, string serverAddress) : base(matchCreationParams, MultiplayerManagerCreator)
        {
            //Simple HUD

            var canvasObject = GameObject.Find("Canvas");

            serverIsFullWindow = canvasObject.transform.Find("ServerIsFullWindow").gameObject;
            serverIsFullWindow.transform.Find<Button>("Window/Body/ToMainMenuButton").onClick.AddListener(Main.LoadMainMenu);
            serverIsFullWindow.gameObject.SetActive(false);

            serverTimeoutWindow = canvasObject.transform.Find("ServerTimeoutWindow").gameObject;
            serverTimeoutWindow.transform.Find<Button>("Window/Body/ToMainMenuButton").onClick.AddListener(Main.LoadMainMenu);
            serverTimeoutWindow.gameObject.SetActive(false);

            killsText = canvasObject.transform.Find<Text>("KillsText");
            deathsText = canvasObject.transform.Find<Text>("DeathsText");
            playerHealthText = canvasObject.transform.Find<Text>("PlayerHealthText");
            playersOnlineText = canvasObject.transform.Find<Text>("PlayersOnlineText");


            if (serverAddress == null)
            {
                isHost = true;

                udpNetwork = new UdpNetwork(ProtocolConfig.MaxStateSize * ProtocolConfig.MaxPlayers * 2, 1024 * ProtocolConfig.MaxPlayers);

                udpNetwork.InitAsServer(ProtocolConfig.Port);

                statePacketWriter = new PacketWriter
                {
                    buffer = stateBuffer
                };
            }
            else
            {
                udpNetwork = new UdpNetwork(1024, ProtocolConfig.MaxStateSize * 2);

                udpNetwork.lastWriteRemoteIpEndPoint.Address = IPAddress.Parse(serverAddress);
                udpNetwork.lastWriteRemoteIpEndPoint.Port = ProtocolConfig.Port;

                udpNetwork.InitAsClient();

                statePacketReader = new PacketReader
                {
                    buffer = stateBuffer
                };
            }

            var packetTypes = Enum.GetValues(typeof(PacketType));
            maxPacketTypeValue = (PacketType) packetTypes.GetValue(packetTypes.Length - 1);

            packetWriter = new PacketWriter
            {
                buffer = new byte[UdpNetwork.Mtu]
            };

            packetReader = new PacketReader
            {
                buffer = new byte[UdpNetwork.Mtu]
            };

            if (!isHost) SendInitPacket();
            else
            {
                //Init local player with infinity timeout

                LocalPlayerSlot = 0;

                var localPlayer = new Player((byte) LocalPlayerSlot, new IPEndPoint(0, 0))
                {
                    lastPacketReceiveTime = -1
                };

                players[LocalPlayerSlot] = localPlayer;

                OnPlayerConnect(localPlayer);
            }
        }

        private static object MultiplayerManagerCreator(MatchManager matchManager, Type type)
        {
            if(type == typeof(ActorManager)) return new MultiplayerActorManager(matchManager);
            if (type == typeof(GameModeManager)) return new MultiplayerGameModeManager(matchManager);

            return DefaultManagerCreator(matchManager, type);
        }

        public override void OnDestroy()
        {
            udpNetwork.Dispose();
        }

        public override void Update()
        {
            while (udpNetwork.TryRead(packetReader.buffer, 0, out var length))
            {
                packetReader.Reset();

                packetReader.ReadHeader(out var packetType);

                if (packetType <= maxPacketTypeValue)
                {
                    if(isHost) ServerProcessIncomingPacket(packetType, length - ProtocolConfig.PacketHeaderSize);
                    else ClientProcessIncomingPacket(packetType, length - ProtocolConfig.PacketHeaderSize);
                }
            }

            if(isHost) UpdateServer();
            else UpdateClient();

            if (IsInitializationComplete)
            {
                var localPlayer = players[LocalPlayerSlot];
                if (localPlayer != null)
                {
                    //example update text per frame

                    killsText.text = "Kills: " + localPlayer.kills;
                    deathsText.text = "Deaths: " + localPlayer.deaths;

                    var characterActor = actorManager.TryGetActor(gameModeManager.LocalPlayerControlledActorPointer) as CharacterActor;
                    if (characterActor == null) playerHealthText.text = "Waiting for spawn...";
                    else playerHealthText.text = "Health: " + characterActor.health;
                }

                playersOnlineText.text = "Players online: " + players.Count(p => p != null);

                base.Update();
            }
        }

        private void OnPlayerConnect(Player player)
        {
            var multiplayerGameModeManager = (MultiplayerGameModeManager) gameModeManager;

            multiplayerGameModeManager.OnPlayerConnect(player);
        }

        private void OnPlayerDisconnect(Player player)
        {
            var multiplayerGameModeManager = (MultiplayerGameModeManager) gameModeManager;

            multiplayerGameModeManager.OnPlayerDisconnect(player);
        }

        public override void FixedUpdate()
        {
            if (IsInitializationComplete) base.FixedUpdate();
        }

        public Player GetLastIncomingPacketPlayerAndUpdateReceiveTime()
        {
            if(!isHost) throw new Exception("Allow on host only!");

            foreach (var player in players)
            {
                if(player == null) continue;

                if (player.remoteIpEndPoint.Equals(udpNetwork.lastReadRemoteIpEndPoint))
                {
                    player.lastPacketReceiveTime = Time.unscaledTime;

                    return player;
                }
            }

            return null;
        }
    }
}