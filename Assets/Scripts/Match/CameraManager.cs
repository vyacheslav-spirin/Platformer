using UnityEngine;

namespace Assets.Scripts.Match
{
    public sealed class CameraManager
    {
        private readonly MatchManager matchManager;

        private readonly Transform backCameraTransform;

        private readonly Transform forwardCameraTransform;

        private Vector2 lastCamTarget;

        public CameraManager(MatchManager matchManager)
        {
            this.matchManager = matchManager;

            backCameraTransform = GameObject.Find("BackCamera").transform;

            forwardCameraTransform = GameObject.Find("ForwardCamera").transform;
        }

        public void Update()
        {
            var localActor = matchManager.actorManager.TryGetActor(matchManager.gameModeManager.LocalPlayerControlledActorPointer);
            if (localActor != null)
            {
                lastCamTarget = localActor.Pos;
            }

            var camPos = forwardCameraTransform.position;
            camPos = Vector3.Lerp(camPos, new Vector3(lastCamTarget.x, lastCamTarget.y, camPos.z), Time.deltaTime * 7f);
            forwardCameraTransform.position = camPos;

            camPos = backCameraTransform.position;
            camPos = Vector3.Lerp(camPos, new Vector3(lastCamTarget.x / 4, lastCamTarget.y / 4, camPos.z), Time.deltaTime * 7f);
            backCameraTransform.position = camPos;
        }
    }
}