using System;
using VRage.Utils;
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

            MyLog.Default.Info($"--- Start Box ---");

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

        public static bool[] GetOctants(this BoundingBoxD box, Vector3D epicenter, MatrixD matrix)
        {
            bool[] octants = new bool[8];

            Vector3D min = Vector3D.Transform(box.Min, matrix) - epicenter;
            Vector3D max = Vector3D.Transform(box.Max, matrix) - epicenter;
            Vector3D pos = min;

            MyLog.Default.Info($"--- Start Box ---");

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

        public static bool GaugeIntersects(this BoundingBoxD box, RayD ray)
        {
            if (!Vector3D.IsUnit(ref ray.Direction))
            {
                ray.Direction.Normalize();
            }

            // r.dir is unit direction vector of ray
            Vector3D dirfrac = new Vector3D(1.0f / ray.Direction.X, 1.0f / ray.Direction.Y, 1.0f / ray.Direction.Z);

            // lb is the corner of AABB with minimal coordinates - left bottom, rt is maximal corner
            // r.org is origin of ray
            double t1 = (box.Min.X - ray.Position.X) * dirfrac.X;
            double t2 = (box.Max.X - ray.Position.X) * dirfrac.X;
            double t3 = (box.Min.Y - ray.Position.Y) * dirfrac.Y;
            double t4 = (box.Max.Y - ray.Position.Y) * dirfrac.Y;
            double t5 = (box.Min.Z - ray.Position.Z) * dirfrac.Z;
            double t6 = (box.Max.Z - ray.Position.Z) * dirfrac.Z;



            double tmin = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
            double tmax = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

            // if tmax < 0, ray (line) is intersecting AABB, but the whole AABB is behind us
            //if (tmax < 0)
            //{
            //    t = tmax;
            //    return false;
            //}

            // if tmin > tmax, ray doesn't intersect AABB
            if (tmin > tmax)
            {
                //t = tmax;
                return false;
            }
            //t = tmin;
            return true;
        }

        public static void Set(this Vector3I v, int x, int y, int z)
        {
            v.X = x;
            v.Y = y;
            v.Z = z;
        }
    }
}
