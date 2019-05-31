using System;
using System.Collections;
using Assets.Scripts.MainMenu;
using Assets.Scripts.Match;
using Assets.Scripts.Match.Multiplayer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts
{
    public sealed class Main : MonoBehaviour
    {
        private static Main instance;

        [UsedImplicitly]
        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);

                return;
            }

            DontDestroyOnLoad(gameObject);

            instance = this;
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance.DestroyCurrentObjects();

                instance = null;
            }
        }


        public GameState GameState { get; private set; } = GameState.Init;

        public static float FixedTimeLerpValue { get; private set; }

        private float callFixedUpdateTime;

        private MainMenuManager mainMenuManager;

        private MatchManager matchManager;

        private void DestroyCurrentObjects()
        {
            if (mainMenuManager != null)
            {
                mainMenuManager.OnDestroy();

                mainMenuManager = null;
            }

            if (matchManager != null)
            {
                matchManager.OnDestroy();

                matchManager = null;
            }
        }

        public static void LoadMainMenu()
        {
            if((instance.GameState & GameState.Loading) != 0) throw new Exception("Other loading process already in progress!");

            instance.StartCoroutine(instance.LoadingMainMenuProcess());
        }

        private IEnumerator LoadingMainMenuProcess()
        {
            DestroyCurrentObjects();

            GameState = GameState.Loading | GameState.MainMenu;

            SceneManager.LoadScene("Loading", LoadSceneMode.Single);

            //Чтобы предыдущая сцена зарендерилась
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            SceneManager.LoadScene("Background", LoadSceneMode.Single);
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);

            yield return new WaitForEndOfFrame();

            mainMenuManager = new MainMenuManager();

            GameState &= ~GameState.Loading;
        }

        public static void LoadMatch(MatchCreationParams matchCreationParams)
        {
            if ((instance.GameState & GameState.Loading) != 0) throw new Exception("Other loading process already in progress!");

            instance.StartCoroutine(instance.LoadingMatchProcess(matchCreationParams, MatchType.SinglePlayer, null));
        }

        public static void LoadMultiplayerMatch(MatchCreationParams matchCreationParams, string serverAddress)
        {
            if ((instance.GameState & GameState.Loading) != 0) throw new Exception("Other loading process already in progress!");

            instance.StartCoroutine(instance.LoadingMatchProcess(
                matchCreationParams, serverAddress == null ? MatchType.MultiplayerServer : MatchType.MultiplayerClient, serverAddress));
        }

        private enum MatchType
        {
            SinglePlayer,
            MultiplayerServer,
            MultiplayerClient
        }

        private IEnumerator LoadingMatchProcess(MatchCreationParams matchCreationParams, MatchType matchType, string serverAddress)
        {
            DestroyCurrentObjects();

            GameState = GameState.Loading | GameState.Match;

            SceneManager.LoadScene("Loading", LoadSceneMode.Single);

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            SceneManager.LoadScene("Background", LoadSceneMode.Single);
            SceneManager.LoadScene("HUD", LoadSceneMode.Additive);
            SceneManager.LoadScene("Match", LoadSceneMode.Additive);

            yield return new WaitForEndOfFrame();

            switch (matchType)
            {
                case MatchType.SinglePlayer:
                    matchManager = new MatchManager(matchCreationParams);
                    break;
                case MatchType.MultiplayerServer:
                    matchManager = new MultiplayerMatchManager(matchCreationParams, null);
                    break;
                case MatchType.MultiplayerClient:
                    matchManager = new MultiplayerMatchManager(matchCreationParams, serverAddress);
                    break;
                default:
                    throw new Exception("Unknown match type!");
            }

            GameState &= ~GameState.Loading;
        }

        [UsedImplicitly]
        private void Update()
        {
            Application.targetFrameRate = 60;

            callFixedUpdateTime += Time.deltaTime;

            FixedTimeLerpValue = callFixedUpdateTime / Time.fixedDeltaTime;

            if (GameState == GameState.Init)
            {
                LoadMainMenu();

                return;
            }

            matchManager?.Update();

            mainMenuManager?.Update();
        }

        [UsedImplicitly]
        private void FixedUpdate()
        {
            callFixedUpdateTime = 0;

            matchManager?.FixedUpdate();
        }
    }
}