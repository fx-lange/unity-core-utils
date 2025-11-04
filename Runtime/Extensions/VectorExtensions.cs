using UnityEngine;

namespace CoreFx.Extensions
{
    public static class Vector3Extensions
    {
        public static Vector3 With(this Vector3 vector, float? x = 0, float? y = 0, float? z = 0)
        {
            return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
        }
        
        public static float Volume(this Vector3 vec)
        {
            return vec.x * vec.y * vec.z;
        }
        
        public static Vector3 Abs(this Vector3 vec)
        {
            return new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
        }
    }
    
    public static class Vector2Extensions
    {
        public static Vector2 With(this Vector2 vector, float? x = 0, float? y = 0)
        {
            return new Vector2(x ?? vector.x, y ?? vector.y);
        }
        
        public static Vector2 Abs(this Vector2 vec)
        {
            return new Vector2(Mathf.Abs(vec.x), Mathf.Abs(vec.y));
        }
    }
}