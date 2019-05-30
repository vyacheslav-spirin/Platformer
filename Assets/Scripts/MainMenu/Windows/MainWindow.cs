using Assets.Scripts.Match;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.MainMenu.Windows
{
    public sealed class MainWindow : Window
    {
        public MainWindow(MainMenuManager mainMenuManager, GameObject windowRoot) : base(mainMenuManager, "Main", windowRoot)
        {
            rootObject.transform.Find<Button>("HostGameButton").onClick.AddListener(OnHostButtonClick);

            rootObject.transform.Find<Button>("JoinToGameButton").onClick.AddListener(OnJoinButtonClick);

            rootObject.transform.Find<Button>("QuitButton").onClick.AddListener(() =>
            {
                Application.Quit(0);
            });
        }

        private void OnHostButtonClick()
        {
            Main.LoadMultiplayerMatch(new MatchCreationParams
            {
                mapName = "Map1"
            }, null);
        }

        private void OnJoinButtonClick()
        {
            mainMenuManager.ShowWindowAsTop("JoinToGame");
        }
    }
}