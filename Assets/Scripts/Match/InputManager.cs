using Assets.Scripts.Match.Actors;
using UnityEngine;

namespace Assets.Scripts.Match
{
    public class InputManager
    {
        private readonly MatchManager matchManager;

        private float leftKeyPressTime;
        private float rightKeyPressTime;

        public InputManager(MatchManager matchManager)
        {
            this.matchManager = matchManager;
        }

        public void Update()
        {
            var localCharacterActor = matchManager.actorManager.TryGetActor(matchManager.gameModeManager.LocalPlayerControlledActorPointer) as CharacterActor;
            if (localCharacterActor == null) return;

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) leftKeyPressTime = Time.time;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) rightKeyPressTime = Time.time;

            if (leftKeyPressTime >= rightKeyPressTime)
            {
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) localCharacterActor.Move(-1);
            }
            else
            {
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) localCharacterActor.Move(1);
            }

            if(Input.GetKey(KeyCode.Space)) localCharacterActor.Jump();

            if (Input.GetKeyDown(KeyCode.LeftControl)) localCharacterActor.Kick();
        }
    }
}