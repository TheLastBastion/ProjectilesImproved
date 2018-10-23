using Sandbox.Game;
using System;
using System.Collections.Generic;
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

        public Dictionary<MyStringHash, RayE[]> ShapeLookup = new Dictionary<MyStringHash, RayE[]>();

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

            List<RayE> rays = new List<RayE>();
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

                    Vector3D position = new Vector3D(x, y, z-radius);
                    Vector3D direction = Vector3D.Normalize(position);
                    rays.Add(new RayE(position, direction));
                }
            }

            if (!ShapeLookup.ContainsKey(id))
            {
                ShapeLookup.Add(id, rays.ToArray());
            }
            else
            {
                ShapeLookup[id] = rays.ToArray();
            }
        }

        public static RayE[][] GetExplosionRays(MyStringHash id, MatrixD transformMatrix, Vector3D epicenter, float damagePool)
        {
            RayE[] baseRays = Instance.ShapeLookup[id];
            List<List<RayE>> rays = new List<List<RayE>>
            {
                new List<RayE>(),
                new List<RayE>(),
                new List<RayE>(),
                new List<RayE>(),
                new List<RayE>(),
                new List<RayE>(),
                new List<RayE>(),
                new List<RayE>()
            };

            float RayDamage = damagePool / baseRays.Length;

            foreach (RayE baseRay in baseRays)
            {
                RayE ray = new RayE
                {
                    Position = Vector3D.Transform(baseRay.Position, transformMatrix),
                    Direction = Vector3D.Transform(baseRay.Direction, transformMatrix),
                    Damage = RayDamage
                };

                int octant = ray.Direction.GetOctant();
                rays[octant].Add(ray);
                if (Settings.DebugMode_ShowSphereOctants) MyVisualScriptLogicProvider.AddGPS("", "", ray.Direction*0.25f, Settings.DebugOctantColors[octant]);
            }

            return new RayE[8][]
            {
                rays[0].ToArray(),
                rays[1].ToArray(),
                rays[2].ToArray(),
                rays[3].ToArray(),
                rays[4].ToArray(),
                rays[5].ToArray(),
                rays[6].ToArray(),
                rays[7].ToArray(),
            };
        }
    }

    /// <summary>
    /// Holds extended block details for easy explosion compuations
    /// </summary>
    public class EntityDesc
    {
        public float AccumulatedDamage;
        public double DistanceSquared;
        public IMyDestroyableObject Object;
        public List<RayE> Rays = new List<RayE>();

        public EntityDesc(IMyDestroyableObject obj, double dist)
        {
            DistanceSquared = dist;
            Object = obj;
        }
    }

    /// <summary>
    /// Holds all blocks that intersect a line
    /// </summary>
    public struct RayE
    {
        public Vector3D Position;
        public Vector3D Direction;
        public float Damage;

        public RayE(Vector3D point, Vector3D direction)
        {
            Position = point;
            Direction = direction;
            Damage = 0;
        }

        public RayE(Vector3D point, Vector3D direction, float damage)
        {
            Position = point;
            Direction = direction;
            Damage = damage;
        }

        public RayE(RayE ray, float damage)
        {
            Position = ray.Position;
            Direction = ray.Direction;
            Damage = damage;
        }

    }
}
