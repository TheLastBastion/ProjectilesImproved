using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace ProjectilesImproved
{
    public class ExplosionTools
    {
        private static void Swap<T>(ref T x, ref T y)
        {
            T tmp = y;
            y = x;
            x = tmp;
        }

        public static List<Vector3I> GetBlocksBetweenPoint(Vector3I v0, Vector3I v1)
        {
            List<Vector3I> blocks = new List<Vector3I>();

            bool steepXY = Math.Abs(v1.Y - v0.Y) > Math.Abs(v1.X - v0.X);
            if (steepXY)
            {
                Swap(ref v0.X, ref v0.Y);
                Swap(ref v1.X, ref v1.Y);
            }

            bool steepXZ = Math.Abs(v1.Z - v0.Z) > Math.Abs(v1.X - v0.X);
            if (steepXZ)
            {
                Swap(ref v0.X, ref v0.Z);
                Swap(ref v1.X, ref v1.Z);
            }

            int deltaX = Math.Abs(v1.X - v0.X);
            int deltaY = Math.Abs(v1.Y - v0.Y);
            int deltaZ = Math.Abs(v1.Z - v0.Z);

            int errorXY = deltaX / 2, errorXZ = deltaX / 2;

            int stepX = (v0.X > v1.X) ? -1 : 1;
            int stepY = (v0.Y > v1.Y) ? -1 : 1;
            int stepZ = (v0.Z > v1.Z) ? -1 : 1;

            int y = v0.Y, z = v0.Z;

            // Check if the end of the line hasn't been reached.
            for (int x = v0.X; x != v1.X; x += stepX)
            {
                Vector3I copy = new Vector3I(x, y, z);

                if (steepXZ) Swap(ref copy.X, ref copy.Z);
                if (steepXY) Swap(ref copy.X, ref copy.Y);

                // Replace the WriteLine with your call to DrawOneBlock

                blocks.Add(copy);

                errorXY -= deltaY;
                errorXZ -= deltaZ;

                if (errorXY < 0)
                {
                    y += stepY;
                    errorXY += deltaX;
                }

                if (errorXZ < 0)
                {
                    z += stepZ;
                    errorXZ += deltaX;
                }
            }

            return blocks;
        }
    }
}
