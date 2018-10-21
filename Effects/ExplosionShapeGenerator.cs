using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
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

        public Dictionary<MyStringHash, ExplosionRay[][]> ShapeLookup = new Dictionary<MyStringHash, ExplosionRay[][]>();

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

            List<List<ExplosionRay>> rays = new List<List<ExplosionRay>>();
            rays.Add(new List<ExplosionRay>());
            rays.Add(new List<ExplosionRay>());
            rays.Add(new List<ExplosionRay>());
            rays.Add(new List<ExplosionRay>());
            rays.Add(new List<ExplosionRay>());
            rays.Add(new List<ExplosionRay>());
            rays.Add(new List<ExplosionRay>());
            rays.Add(new List<ExplosionRay>());

            double x = 0;
            double y = 0;
            double z = 0;
            double calcRange = 0;
            float radiusSquared = radius * radius;

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

                    Vector3D position = new Vector3D(x, y, z);
                    Vector3D direction = Vector3D.Normalize(position);
                    int octant = direction.GetOctant();

                    rays[octant].Add(new ExplosionRay(position, direction));
                }
            }

            ExplosionRay[][] rayArray = new ExplosionRay[8][];

            for (int i = 0; i < 8; i++)
            {
                rayArray[i] = rays[i].ToArray();
            }

            if (!ShapeLookup.ContainsKey(id))
            {
                ShapeLookup.Add(id, rayArray);
            }
            else
            {
                ShapeLookup[id] = rayArray;
            }
        }

        public static ExplosionRay[][] GetExplosionRays(MyStringHash id, MatrixD transformMatrix, Vector3D epicenter)
        {
            ExplosionRay[][] octants = Instance.ShapeLookup[id];
            ExplosionRay[][] values = new ExplosionRay[8][];

            for (int i = 0; i < 8; i++)
            {
                values[i] = new ExplosionRay[octants[i].Length];

                for (int j = 0; j < octants[i].Length; j++)
                {
                    ExplosionRay ray = new ExplosionRay(octants[i][j]);
                    ray.Position = Vector3D.Transform(ray.Position, transformMatrix);
                    ray.Direction = Vector3D.Transform(ray.Direction, transformMatrix);

                    values[i][j] = ray;
                }
            }

            return values;
        }
    }

    /// <summary>
    /// Holds extended block details for easy explosion compuations
    /// </summary>
    public struct EntityDesc
    {
        public bool Destroyed;
        public float AccumulatedDamage;
        public double DistanceSquared;
        public IMyDestroyableObject Object;

        public EntityDesc(IMyDestroyableObject obj, double dist)
        {
            DistanceSquared = dist;
            Object = obj;
            AccumulatedDamage = 0;
            Destroyed = false;
        }
    }

    /// <summary>
    /// Holds all blocks that intersect a line
    /// </summary>
    public class ExplosionRay
    {
        public Vector3D Position;
        public Vector3D Direction;
        public List<EntityDesc> BlockList;

        public ExplosionRay(Vector3D point, Vector3D direction)
        {
            Position = point;
            Direction = direction;
            BlockList = new List<EntityDesc>();
        }

        public ExplosionRay(Vector3D point, Vector3D direction, List<EntityDesc> blocks)
        {
            Position = point;
            Direction = direction;
            BlockList = blocks;
        }

        public ExplosionRay(ExplosionRay ray)
        {
            Position = ray.Position;
            Direction = ray.Direction;
            BlockList = ray.BlockList;
        }

    }
}
