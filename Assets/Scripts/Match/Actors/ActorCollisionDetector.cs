using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts.Match.Actors
{
    [UsedImplicitly]
    public sealed class ActorCollisionDetector : MonoBehaviour
    {
        public readonly List<Collider> touchColliders = new List<Collider>(10);

        [UsedImplicitly]
        private void OnCollisionEnter(Collision other)
        {
            touchColliders.Add(other.collider);
        }

        [UsedImplicitly]
        private void OnCollisionExit(Collision other)
        {
            touchColliders.Remove(other.collider);
        }

        public void CleanUpDestroyedTouchColliders()
        {
            for (var i = touchColliders.Count - 1; i >= 0; i--)
            {
                if(touchColliders[i] == null) touchColliders.RemoveAt(i);
            }
        }
    }
}