using CoreFx.Controls;
using UnityEditor;
using UnityEngine;

namespace CoreUtilsFx.Controls
{
    [CustomEditor(typeof(LayeredSphere))]
    public class LayeredSphereEditor : UnityEditor.Editor
    {
        //TODO how is freya doing it?
        private void OnSceneGUI()
        {
            var sphere = target as LayeredSphere;
            Handles.color = Color.cyan;
            EditorGUI.BeginChangeCheck();
            var innerRadius = Handles.RadiusHandle(sphere.transform.rotation, sphere.transform.position, sphere.InnerRadius);
            if (EditorGUI.EndChangeCheck())
            {
                if (innerRadius < sphere.OuterRadius)
                {
                    Undo.RecordObject(sphere, "Layered Sphere Inner Radius");
                    sphere.InnerRadius = innerRadius;
                }
            }

            Handles.color = Color.magenta;
            EditorGUI.BeginChangeCheck();
            var outerRadius =
                Handles.RadiusHandle(sphere.transform.rotation, sphere.transform.position, sphere.OuterRadius);
            if (EditorGUI.EndChangeCheck())
            {
                if (outerRadius > sphere.InnerRadius)
                {
                    Undo.RecordObject(sphere, "Layered Sphere Outer Radius");
                    sphere.OuterRadius = outerRadius;
                }
            }
        }
    }
}