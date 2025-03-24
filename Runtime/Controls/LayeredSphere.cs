using System;
using UnityEngine;

namespace CoreFx.Controls
{
    public class LayeredSphere : MonoBehaviour
    {
        [SerializeField] private float _innerRadius;
        [HideInInspector][SerializeField] private float _outerRadius;
        [SerializeField] private float _outerDistance;

        public float InnerRadius
        {
            get => _innerRadius;
            set => _innerRadius = value;
        }

        public float OuterRadius => _innerRadius + _outerDistance;

        public float OuterDistance
        {
            get => _outerDistance;
            set => _outerDistance = value;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            UnityEditor.Handles.color = new Color(1, 1, 1, 0.4f);
            UnityEditor.Handles.RadiusHandle(transform.rotation, transform.position, InnerRadius);
            UnityEditor.Handles.color = new Color(1, 1, 1, 0.2f);
            UnityEditor.Handles.RadiusHandle(transform.rotation, transform.position, OuterRadius);
        }
#endif
    }
}