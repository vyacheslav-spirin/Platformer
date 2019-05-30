using System;
using Assets.Scripts.Match.Actors;
using Assets.Scripts.Match.Map;
using UnityEngine;

namespace Assets.Scripts.Match
{
    public class MatchManager
    {
        protected readonly MatchCreationParams matchCreationParams;

        internal readonly MapManager mapManager;

        internal readonly ActorManager actorManager;

        internal readonly CameraManager cameraManager;

        internal readonly GameModeManager gameModeManager;

        internal readonly InputManager inputManager;

        public float MatchTime { get; private set; }

        public MatchManager(MatchCreationParams matchCreationParams, Func<MatchManager, Type, object> customManagerCreator = null)
        {
            this.matchCreationParams = matchCreationParams;

            if (customManagerCreator == null) customManagerCreator = DefaultManagerCreator;

            mapManager = (MapManager) customManagerCreator(this, typeof(MapManager));
            actorManager = (ActorManager) customManagerCreator(this, typeof(ActorManager));
            cameraManager = (CameraManager) customManagerCreator(this, typeof(CameraManager));
            gameModeManager = (GameModeManager) customManagerCreator(this, typeof(GameModeManager));
            inputManager = (InputManager) customManagerCreator(this, typeof(InputManager));

            mapManager.CreateBlockActors();
        }

        protected static object DefaultManagerCreator(MatchManager matchManager, Type type)
        {
            if (type == typeof(MapManager)) return new MapManager(matchManager, matchManager.matchCreationParams.mapName);
            if (type == typeof(ActorManager)) return new ActorManager(matchManager);
            if (type == typeof(CameraManager)) return new CameraManager(matchManager);
            if (type == typeof(GameModeManager)) return new GameModeManager(matchManager);
            if (type == typeof(InputManager)) return new InputManager(matchManager);

            throw new Exception("Invalid manager type!");
        }

        public virtual void OnDestroy()
        {
        }

        public virtual void Update()
        {
            MatchTime += Time.deltaTime;

            actorManager.Update();

            gameModeManager.Update();

            inputManager.Update();

            cameraManager.Update();
        }

        public virtual void FixedUpdate()
        {
            actorManager.FixedUpdate();
        }
    }
}