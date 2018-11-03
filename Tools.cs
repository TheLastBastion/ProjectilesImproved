using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved
{
    public static class Tools
    {

        public const float Tick = 1f / 60f;

        /// <summary>
        /// Gets the octant this vector occupies 
        /// </summary>
        /// <param name="dir">localized vector</param>
        /// <returns>a linear representation of its octant</returns>
        public static int GetOctant(this Vector3D dir)
        {
            return ((dir.X > 0) ? 0 : 4) + ((dir.Y > 0) ? 0 : 2) + ((dir.Z > 0) ? 0 : 1);
        }

        public static bool[] GetOctants(this BoundingBoxD box, Vector3D epicenter)
        {
            return GetOctants(box.Min - epicenter, box.Max - epicenter);
        }

        public static bool[] GetOctants(this Vector3D Min, float gridSize)
        {
            return GetOctants(Min, Min + gridSize);
        }

        public static bool[] GetOctants(Vector3D Min, Vector3D Max)
        {
            bool[] octants = new bool[8];
            Vector3D pos = Min;

            octants[Min.GetOctant()] = true; // -1 -1 -1
            octants[Max.GetOctant()] = true; // 1 1 1

            pos.X = Max.X; // 1 -1 -1
            octants[pos.GetOctant()] = true;

            pos.Y = Max.Y; // 1 1 -1
            octants[pos.GetOctant()] = true;

            pos.X = Min.X; // -1 1 -1
            octants[pos.GetOctant()] = true;

            pos.Z = Max.Z; // -1 1 1
            octants[pos.GetOctant()] = true;

            pos.Y = Min.Y; // -1 -1 1
            octants[pos.GetOctant()] = true;

            pos.X = Max.X; // 1 -1 1
            octants[pos.GetOctant()] = true;

            return octants;
        }

        public static bool GaugeIntersects(this BoundingBoxD box, RayD ray)
        {
            if (!Vector3D.IsUnit(ref ray.Direction))
            {
                ray.Direction.Normalize();
            }

            // ripped this stuff off the net. it works so im not complaining
            Vector3D dirfrac = new Vector3D(1.0f / ray.Direction.X, 1.0f / ray.Direction.Y, 1.0f / ray.Direction.Z);

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

        public static void Rotate(this Vector3D v, MatrixD matrix)
        {
            double x = v.X * matrix.M11 + v.Y * matrix.M21 + v.Z * matrix.M31;
            double y = v.X * matrix.M12 + v.Y * matrix.M22 + v.Z * matrix.M32;
            double z = v.X * matrix.M13 + v.Y * matrix.M23 + v.Z * matrix.M33;
            v.X = x;
            v.Y = y;
            v.Z = z;
        }

        public static void Set(this Vector3I v, int x, int y, int z)
        {
            v.X = x;
            v.Y = y;
            v.Z = z;
        }

        private static Dictionary<string, long[]> Analitics = new Dictionary<string, long[]>();

        public const int Time = 0;
        public const int Runs = 1;
        public const int BeforeStart = 2;

        public static void Start(this Stopwatch watch, string name)
        {
            if (!Settings.DebugMode) return;
            if (!Analitics.ContainsKey(name))
            {
                Analitics.Add(name, new long[3]);
            }

            long[] data = Analitics[name];

            data[Runs]++;
            data[BeforeStart] = watch.ElapsedTicks;
            watch.Start();
        }

        public static void Stop(this Stopwatch watch, string name)
        {
            if (!Settings.DebugMode) return;
            if (!Analitics.ContainsKey(name)) return;

            long[] data = Analitics[name];
            data[Time] += watch.ElapsedTicks - data[BeforeStart];
            watch.Stop();
        }

        public static void ResetAll(this Stopwatch watch)
        {
            if (!Settings.DebugMode) return;
            watch.Reset();
            Analitics.Clear();
        }

        public static long Runtime(this Stopwatch watch, string name)
        {
            if (!Analitics.ContainsKey(name)) return 0;

            return Analitics[name][Time];
        }

        public static long RunCount(this Stopwatch watch, string name)
        {
            if (!Analitics.ContainsKey(name)) return 0;

            return Analitics[name][Runs];
        }

        public static void Write(this Stopwatch watch, string name)
        {
            if (!Settings.DebugMode) return;
            if (!Analitics.ContainsKey(name)) return;

            long[] list = Analitics[name];
            MyLog.Default.Info($"[{name}] Avg: {((((double)list[Time] / (double)list[Runs])/Stopwatch.Frequency) * 1000d).ToString("n4")}ms, Total: {(((double)list[Time] / Stopwatch.Frequency) * 1000d).ToString("n4")}ms, Runs: {list[Runs]}");
        }

        public static double AngleBetween(Vector3D norm1, Vector3D norm2)
        {
            float ratio = Vector3.Dot(norm1, norm2);

            double theta;

            if (ratio < 0)
            {
                theta = Math.PI - 2.0 * Math.Asin((-norm1 - norm2).Length() / 2.0);
            }
            else
            {
                theta = 2.0 * Math.Asin((norm1 - norm2).Length() / 2.0);
            }

            return theta * 180 / Math.PI;
        }
    }
}
