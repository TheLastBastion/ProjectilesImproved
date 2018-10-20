using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    public class ExplosionShapeGenerator
    {
        public static ExplosionShapeGenerator Instance;

        public static void Initialize()
        {
            if (Instance == null)
            {
                Instance = new ExplosionShapeGenerator();
            }
        }

        public Dictionary<MyStringHash, Paring[]> ShapeLookup = new Dictionary<MyStringHash, Paring[]>();

        public ExplosionShapeGenerator()
        {
            foreach (KeyValuePair<MyStringHash, EffectBase> set in Settings.AmmoEffectLookup)
            {
                if (set.Value.Explosive == null) continue;
                Generate(set.Key, set.Value.Explosive.Radius, set.Value.Explosive.Resolution, set.Value.Explosive.Angle);
            }
        }

        /// <summary>
        /// Generates the explosion shape for a given ammo type
        /// </summary>
        /// <param name="id">Ammo Type</param>
        /// <param name="radius">Explosion Size</param>
        /// <param name="resolution">0.25 to less than 10</param>
        /// <param name="maxAngle">0 to 180</param>
        private void Generate(MyStringHash id, float radius, float resolution, float maxAngle)
        {
            if (resolution < 0.25f)
            {
                resolution = 0.25f; // this will only need to change if a micro grid size is made
            }

            List<Paring> parings = new List<Paring>();

            double x = 0;
            double y = 0;
            double z = 0;
            double calcRange = 0;
            float radiusSqud = radius * radius;

            double step = resolution / radius;
            int steps = (int)Math.Ceiling(maxAngle * 2 * Math.PI / 360 / step);

            for (int h = 0; h < steps; h++)
            {
                z = (1 - Math.Cos(h * step)) * radius;
                calcRange = Math.Sin(h * step) * radius;
                float step2 = (float)(resolution / calcRange);
                int steps2 = (int)Math.Ceiling(2 * Math.PI / step2);

                for (int i = 0; i < steps2; i++)
                {
                    x = Math.Sin(i * step2) * calcRange;
                    y = Math.Cos(i * step2) * calcRange;
                    parings.Add(new Paring(new Vector3D(x, y, z)));
                }
            }

            if (!ShapeLookup.ContainsKey(id))
            {
                ShapeLookup.Add(id, parings.ToArray());
            }
            else
            {
                ShapeLookup[id] = parings.ToArray();
            }
        }

        public static Paring[] GetParings(MyStringHash id, MatrixD transformMatrix, Vector3D epicenter)
        {
            Paring[] originals = Instance.ShapeLookup[id];
            Paring[] values = new Paring[originals.Length];

            Paring original;
            for (int i = 0; i < originals.Length; i++)
            {
                original = originals[i];
                Vector3D translatedPoint = Vector3D.Transform(original.Point, transformMatrix);
                values[i] = new Paring(translatedPoint, new RayD(epicenter, translatedPoint - epicenter));
            }

            return values;
        }
    }

    /// <summary>
    /// Holds extended block details for easy explosion compuations
    /// </summary>
    public struct BlockDesc
    {
        public double DistanceSqud;
        public IMySlimBlock Block;

        public BlockDesc(IMySlimBlock block, double dist)
        {
            DistanceSqud = dist;
            Block = block;
        }
    }

    /// <summary>
    /// Holds all blocks that intersect a line
    /// </summary>
    public struct Paring
    {
        public RayD Ray;
        public Vector3D Point;
        public List<BlockDesc> BlockList;

        public Paring(Vector3D point)
        {
            Point = point;
            BlockList = new List<BlockDesc>();
            Ray = default(RayD);
        }

        public Paring(Vector3D point, List<BlockDesc> blocks)
        {
            Point = point;
            BlockList = blocks;
            Ray = default(RayD);
        }

        public Paring(Vector3D point, RayD ray)
        {
            Point = point;
            Ray = ray;
            BlockList = new List<BlockDesc>();
        }
    }
}
