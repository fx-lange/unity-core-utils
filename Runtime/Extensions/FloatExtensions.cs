using UnityEngine;

namespace CoreFx.Extensions
{
    public static class FloatExtensions
    {
        public static float RoundToBase(this float value, float baseValue)
        {
            return Mathf.Round(value / baseValue) * baseValue;
        }
        
        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            var t = Mathf.InverseLerp(from1, to1, value);
            return Mathf.Lerp(from2, to2, t);
        }
        
        public static float SmoothMap(this float value, float from1, float to1, float from2, float to2)
        {
            var t = Mathf.InverseLerp(from1, to1, value);
            return Mathf.SmoothStep(from2, to2, t);
        }
    }
}