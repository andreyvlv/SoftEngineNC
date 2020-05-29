using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SoftEngineNC
{
    public class Utils
    {
        public static float Clamp(float value, float min = 0, float max = 1)
        {
            return MathF.Max(min, MathF.Min(value, max));
        }

        public static float Interpolate(float min, float max, float gradient)
        {
            return min + (max - min) * Clamp(gradient);
        }

        public static float ComputeNDotL(Vector3 vertex, Vector3 normal, Vector3 lightPosition)
        {
            var lightDirection = lightPosition - vertex;

            normal = Vector3.Normalize(normal);
            lightDirection = Vector3.Normalize(lightDirection);

            return MathF.Max(0, -Vector3.Dot(normal, lightDirection));
        }

        public static Vector3 TransformCoordinate(Vector3 coordinate, Matrix4x4 transform)
        {
            var vector = Vector4.Transform(coordinate, transform);
            var w = 1f / vector.W;
            return new Vector3(vector.X * w, vector.Y * w, vector.Z * w);
        }
    }
}
