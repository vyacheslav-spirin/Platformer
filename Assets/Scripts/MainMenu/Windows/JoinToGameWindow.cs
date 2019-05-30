using System.Net;
using System.Net.Sockets;
using Assets.Scripts.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.MainMenu.Windows
{
    public sealed class JoinToGameWindow : Window
    {
        private readonly InputField addressField;

        private readonly Button joinButton;

        private UdpNetwork udpNetwork;

        public JoinToGameWindow(MainMenuManager mainMenuManager, GameObject windowRoot) : base(mainMenuManager, "JoinToGame", windowRoot)
        {
            var windowContentRoot = rootObject.transform.Find("Window/Body");

            addressField = windowContentRoot.Find<InputField>("AddressField");

            joinButton = windowContentRoot.Find<Button>("JoinButton");
            joinButton.onClick.AddListener(OnJoinButtonClick);

            joinButton.interactable = false;
        }

        public override void Update()
        {
            if (!IsVisible) return;

            joinButton.interactable = IPAddress.TryParse(addressField.text, out var addr) && addr.AddressFamily == AddressFamily.InterNetwork;
        }

        private void OnJoinButtonClick()
        {
            mainMenuManager.ShowWindowAsTop("JoiningToGame");

            var window = (JoiningToGameWindow) mainMenuManager.GetWindowByName("JoiningToGame");

            window.SetServerAddress(addressField.text);
        }
    }
}