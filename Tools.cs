using VRageMath;

namespace ProjectilesImproved
{
    public static class Tools
    {
        /// <summary>
        /// Gets the octant this vector occupies 
        /// </summary>
        /// <param name="dir">localized vector</param>
        /// <returns>a linear representation of its octant</returns>
        public static int GetOctant(this Vector3D dir)
        {
            //int quadrant = 0;
            //quadrant += (dir.X > 0) ? 0 : 4;
            //quadrant += (dir.Y > 0) ? 0 : 2;
            //quadrant += (dir.Z > 0) ? 0 : 1;
            //return quadrant;

            return ((dir.X > 0) ? 0 : 4) + ((dir.Y > 0) ? 0 : 2) + ((dir.Z > 0) ? 0 : 1);
        }

        public static bool[] GetOctants(this BoundingBoxD box, Vector3D epicenter)
        {
            bool[] octants = new bool[8];

            Vector3D min = box.Min - epicenter;
            Vector3D max = box.Max - epicenter;
            Vector3D pos = min;

            octants[min.GetOctant()] = true; // -1 -1 -1
            octants[max.GetOctant()] = true; // 1 1 1

            pos.X = max.X; // 1 -1 -1
            octants[pos.GetOctant()] = true;

            pos.Y = max.Y; // 1 1 -1
            octants[pos.GetOctant()] = true;

            pos.X = min.X; // -1 1 -1
            octants[pos.GetOctant()] = true;

            pos.Z = max.Z; // -1 1 1
            octants[pos.GetOctant()] = true;

            pos.Y = min.Y; // -1 -1 1
            octants[pos.GetOctant()] = true;

            pos.X = max.X; // 1 -1 1
            octants[pos.GetOctant()] = true;

            return octants;
        }

        public static void Set(this Vector3I v, int x, int y, int z)
        {
            v.X = x;
            v.Y = y;
            v.Z = z;
        }
    }
}
