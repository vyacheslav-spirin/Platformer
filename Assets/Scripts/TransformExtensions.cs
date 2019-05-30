using UnityEngine;

namespace Assets.Scripts
{
    public static class TransformExtensions
    {
        public static T Find<T>(this Transform transform, string name)
        {
            return transform.Find(name).GetComponent<T>();
        }
    }
}