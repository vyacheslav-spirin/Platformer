using System.Net;
using System.Net.Sockets;
using Assets.Scripts.Match;
using Assets.Scripts.Match.Multiplayer.Protocol;
using Assets.Scripts.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.MainMenu.Windows
{
    public sealed class JoiningToGameWindow : Window
    {
        private readonly Text stateText;

        private readonly PacketReader packetReader = new PacketReader
        {
            buffer = new byte[300]
        };

        private readonly PacketWriter packetWriter = new PacketWriter
        {
            buffer = new byte[100]
        };

        private UdpNetwork udpNetwork;


        private float lastSendTime;

        private int currentTry;

        public JoiningToGameWindow(MainMenuManager mainMenuManager, GameObject rootObject) : base(mainMenuManager, "JoiningToGame", rootObject)
        {
            var windowContentRoot = rootObject.transform.Find("Window/Body");

            stateText = windowContentRoot.Find<Text>("StateText");

            windowContentRoot.Find<Button>("CancelButton").onClick.AddListener(CancelButtonClicked);
        }

        private void CancelButtonClicked()
        {
            if(udpNetwork != null)
            {
                udpNetwork.Dispose();
                udpNetwork = null;
            }

            currentTry = 0;

            mainMenuManager.HideTopWindow();
        }

        public override void OnDestroy()
        {
            if (udpNetwork != null)
            {
                udpNetwork.Dispose();
                udpNetwork = null;
            }
        }

        public void SetServerAddress(string address)
        {
            if (!IsVisible) return;

            udpNetwork = new UdpNetwork(2000, 2000);
            udpNetwork.lastWriteRemoteIpEndPoint.Address = IPAddress.Parse(address);
            udpNetwork.lastWriteRemoteIpEndPoint.Port = ProtocolConfig.Port;

            udpNetwork.InitAsClient();

            SendMessage();
        }

        private void SendMessage()
        {
            currentTry++;

            if (currentTry > 4)
            {
                stateText.text = udpNetwork.lastWriteRemoteIpEndPoint.Address + " Try " + currentTry;
            }
            else
            {
                stateText.text = udpNetwork.lastWriteRemoteIpEndPoint.Address.ToString();
            }

            lastSendTime = Time.time;

            packetWriter.Reset();

            packetWriter.WriteHeader(PacketType.MatchInfo);

            udpNetwork.Send(packetWriter.buffer, 0, packetWriter.pos);
        }

        public override void Update()
        {
            if (!IsVisible) return;

            if(udpNetwork != null)
            {
                if (udpNetwork.SendError != SocketError.Success)
                {
                    stateText.text = "Send error: " + udpNetwork.SendError;

                    udpNetwork.Dispose();
                    udpNetwork = null;
                }
                else if (udpNetwork.ReceiveError != SocketError.Success)
                {
                    stateText.text = "Receive error: " + udpNetwork.ReceiveError;

                    udpNetwork.Dispose();
                    udpNetwork = null;
                }
                else
                {
                    while (udpNetwork.TryRead(packetReader.buffer, 0, out var length))
                    {
                        packetReader.Reset();

                        packetReader.ReadHeader(out var packetType);

                        if (packetType == PacketType.MatchInfo)
                        {
                            var receivedMatchCreationParams = new MatchCreationParams();
                            receivedMatchCreationParams.Load(packetReader);

                            var serverAddress = udpNetwork.lastWriteRemoteIpEndPoint.Address.ToString();

                            udpNetwork.Dispose();
                            udpNetwork = null;

                            Main.LoadMultiplayerMatch(receivedMatchCreationParams, serverAddress);

                            return;
                        }
                    }
                }

                if (Time.time - lastSendTime > 0.5f)
                {
                    SendMessage();
                }
            }
        }

        public override bool IsCloseEnabled()
        {
            return udpNetwork == null;
        }
    }
}