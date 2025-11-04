using System.Collections.Generic;
using UnityEngine;

namespace CoreFx.Extensions
{
    public static class TransformExtensions
    {
        public static bool InRangeOf(this Transform source, Transform target, float maxDistance, float maxAngle = 360f)
        {
            Vector3 directionToTarget = (target.position - source.position).With(y: 0);
            return directionToTarget.magnitude <= maxDistance &&
                   Vector3.Angle(source.forward, directionToTarget) <= maxAngle / 2;
        }

        public static IEnumerable<Transform> Children(this Transform parent)
        {
            foreach (Transform child in parent)
            {
                yield return child;
            }
        }

        public static void Reset(this Transform transform)
        {
            transform.position = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public static void DestroyChildren(this Transform parent)
        {
            parent.ForEveryChild(child => Object.Destroy(child.gameObject));
        }

        public static void DestroyChildrenImmediate(this Transform parent)
        {
            parent.ForEveryChild(child => Object.DestroyImmediate(child.gameObject));
        }

        public static void EnableChildren(this Transform parent)
        {
            parent.ForEveryChild(child => child.gameObject.SetActive(true));
        }

        public static void DisableChildren(this Transform parent)
        {
            parent.ForEveryChild(child => child.gameObject.SetActive(false));
        }

        public static void ForEveryChild(this Transform parent, System.Action<Transform> action)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                action(parent.GetChild(i));
            }
        }
    }
}