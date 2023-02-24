using System;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace FirstPersonController
{
    public static class Util
    {
        // Convenience function to get a tuple out of a transform
        public static (Vector3 pos, Quaternion rot) GetTransformData(Transform t) => (t.position, t.rotation);

        public static float MapRange(float value, float valueMin, float valueMax, float a, float b)
        {
            return Mathf.Lerp(a, b, Mathf.InverseLerp(valueMin, valueMax, value));
        }

        public static float MapRangeClamped(float value, float valueMin, float valueMax, float a, float b)
        {
            return Mathf.Clamp(MapRange(value, valueMin, valueMax, a, b), a, b);
        }

        static string[] propertyIgnorelist = new string[]
        {
            "bounds",
            "localBounds"
        };

        public static Component CopyComponent(Component original, GameObject destination)
        {
            Type type = original.GetType();
            Component copy = destination.AddComponent(type);

            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            var pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos.Where(
                p => p.CanRead && p.CanWrite && Array.IndexOf(propertyIgnorelist, p.Name) == -1
            ))
            {
                try
                {
                    pinfo.SetValue(copy, pinfo.GetValue(original));
                }
                catch { }
            }

            var fields = type.GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }

            return copy;
        }

        public static Quaternion SnapToNearestRightAngle(Quaternion rotation)
        {
            Vector3 closestToForward = SnappedToNearestAxis(rotation * Vector3.forward);
            Vector3 closestToUp = SnappedToNearestAxis(rotation * Vector3.up);
            return Quaternion.LookRotation(closestToForward, closestToUp);
        }

        public static Vector3 SnappedToNearestAxis(Vector3 direction)
        {
            var x = Mathf.Abs(direction.x);
            var y = Mathf.Abs(direction.y);
            var z = Mathf.Abs(direction.z);
            if (x > y && x > z)
                return new Vector3(Mathf.Sign(direction.x), 0, 0);
            else if (y > x && y > z)
                return new Vector3(0, Mathf.Sign(direction.y), 0);
            else
                return new Vector3(0, 0, Mathf.Sign(direction.z));
        }

        public static Vector3 ExtractEulersFromQuaternion(Quaternion q)
        {
            var eulers = q.eulerAngles;
            for (var i = 0; i < 3; ++i)
            {
                var angle = Mathf.Repeat(eulers[i], 360f);
                eulers[i] = angle > 180f ? angle - 360f : angle;
            }
            return eulers;
        }
    }
}