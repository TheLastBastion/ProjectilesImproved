//using ProjectilesImproved.Definitions;
//using Sandbox.Game;
//using System;
//using System.Collections.Generic;
//using VRage.Game.ModAPI.Interfaces;
//using VRageMath;

//namespace ProjectilesImproved.Effects.Collision
//{
//    public class ExplosionShapeGenerator
//    {
//        public static ExplosionShapeGenerator Instance;

//        public static void Initialize()
//        {
//            if (Instance == null)
//            {
//                Instance = new ExplosionShapeGenerator();
//            }
//        }

//        public Dictionary<string, RayE[]> ShapeLookup = new Dictionary<string, RayE[]>();

//        public ExplosionShapeGenerator()
//        {
//            foreach (KeyValuePair<string, ProjectileDefinition> set in Settings.ProjectileDefinitionLookup)
//            {
//                if (set.Value.Explosive == null) continue;
//                Generate(set.Key, set.Value.Explosive.Radius, set.Value.Explosive.Resolution, set.Value.Explosive.Angle);
//            }
//        }

//        /// <summary>
//        /// Generates the explosion shape for a given ammo type
//        /// </summary>
//        /// <param name="id">Ammo Type</param>
//        /// <param name="radius">Explosion Size</param>
//        /// <param name="resolution">0.25 to less than 10</param>
//        /// <param name="maxAngle">0 to 180</param>
//        //private void Generate(string id, float radius, float resolution, float maxAngle)
//        //{
//        //    if (resolution < 0.25f)
//        //    {
//        //        resolution = 0.25f; // this will only need to change if a micro grid size is made
//        //    }

//        //    List<RayE> rays = new List<RayE>();
//        //    double x = 0;
//        //    double y = 0;
//        //    double z = 0;
//        //    double calcRange = 0;
//        //    float radiusSquared = radius * radius;

//        //    double step = resolution / radius;
//        //    int steps = (int)Math.Ceiling(maxAngle * 2 * Math.PI / 360 / step);

//        //    for (int h = 0; h < steps; h++)
//        //    {
//        //        z = (1 - Math.Cos(h * step)) * radius;
//        //        calcRange = Math.Sin(h * step) * radius;
//        //        float step2 = (float)(resolution / calcRange);
//        //        int steps2 = (int)Math.Ceiling(2 * Math.PI / step2);

//        //        for (int i = 0; i < steps2; i++)
//        //        {
//        //            x = Math.Sin(i * step2) * calcRange;
//        //            y = Math.Cos(i * step2) * calcRange;

//        //            Vector3D position = new Vector3D(x, y, z-radius);
//        //            Vector3D direction = Vector3D.Normalize(position);
//        //            rays.Add(new RayE(position, direction));
//        //        }
//        //    }

//        //    if (!ShapeLookup.ContainsKey(id))
//        //    {
//        //        ShapeLookup.Add(id, rays.ToArray());
//        //    }
//        //    else
//        //    {
//        //        ShapeLookup[id] = rays.ToArray();
//        //    }
//        //}

//        public void Generate(string id, float radius, float resolution, float maxAngle)
//        {
//            double x;
//            double y;
//            double z;
//            double sqrtFive = Math.Sqrt(5);
//            int raycount = (int)((4 * Math.PI * radius * radius) / (resolution * resolution));
//            RayE[] rays = new RayE[raycount];

//            for (int i = 0; i < raycount; i++)
//            {
//                double t = -1d + ((2d * i) / raycount);

//                x = Math.Cos(Math.Asin(t)) * Math.Cos(((sqrtFive + 1d) / 2d - 1d) * Math.PI * i);
//                y = Math.Cos(Math.Asin(t)) * Math.Sin(((sqrtFive + 1d) / 2d - 1d) * Math.PI * i);
//                z = t;
//                Vector3D vector = new Vector3D(x, y, z);
//                rays[i] = new RayE(vector*radius, vector);

//            }

//            if (!ShapeLookup.ContainsKey(id))
//            {
//                ShapeLookup.Add(id, rays);
//            }
//            else
//            {
//                ShapeLookup[id] = rays;
//            }
//        }

//        public static RayE[][] GetExplosionRays(string id, MatrixD transformMatrix, Vector3D epicenter, float Radius, float damagePool)
//        {
//            RayE[] baseRays = Instance.ShapeLookup[id];
//            List<List<RayE>> rays = new List<List<RayE>>
//            {
//                new List<RayE>(),
//                new List<RayE>(),
//                new List<RayE>(),
//                new List<RayE>(),
//                new List<RayE>(),
//                new List<RayE>(),
//                new List<RayE>(),
//                new List<RayE>()
//            };

//            float RayDamage = damagePool / baseRays.Length + 1;

//            RayE baseRay;
//            for (int i = 0; i < baseRays.Length; i++)
//            {
//                baseRay = baseRays[i];

//                RayE ray = new RayE
//                {
//                    Position = Vector3D.Transform(baseRay.Position, transformMatrix),
//                    Damage = RayDamage
//                };
//                ray.Direction = (ray.Position - transformMatrix.Translation) / Radius;

//                int octant = ray.Direction.GetOctant();
//                rays[octant].Add(ray);
//                if (Settings.DebugMode_ShowSphereOctants)
//                {
//                    MyVisualScriptLogicProvider.AddGPS("", "", ray.Position, Settings.DebugOctantColors[octant]);
//                    //MyVisualScriptLogicProvider.AddGPS("", "", ray.Position + (ray.Direction * 0.25f), Settings.DebugOctantColors[octant]);
//                }
//            }

//            return new RayE[8][]
//            {
//                //(rays[1].Count == 0) ? null :
//                rays[0].ToArray(),
//                rays[1].ToArray(),
//                rays[2].ToArray(),
//                rays[3].ToArray(),
//                rays[4].ToArray(),
//                rays[5].ToArray(),
//                rays[6].ToArray(),
//                rays[7].ToArray(),
//            };
//        }
//    }

//    /// <summary>
//    /// Holds extended block details for easy explosion compuations
//    /// </summary>
//    public class EntityDesc
//    {
//        public float AccumulatedDamage;
//        public double DistanceSquared;
//        public IMyDestroyableObject Object;
//        public List<RayLookup> Rays = new List<RayLookup>();

//        public EntityDesc(IMyDestroyableObject obj, double dist)
//        {
//            AccumulatedDamage = 0;
//            DistanceSquared = dist;
//            Object = obj;
//        }
//    }

//    public struct RayLookup
//    {
//        public int Octant;
//        public int Index;

//        public RayLookup(int o, int i)
//        {
//            Octant = o;
//            Index = i;
//        }
//    }

//    /// <summary>
//    /// Holds all blocks that intersect a line
//    /// </summary>
//    public struct RayE
//    {
//        public Vector3D Position;
//        public Vector3D Direction;
//        public float Damage;

//        public RayE(Vector3D point, Vector3D direction)
//        {
//            Position = point;
//            Direction = direction;
//            Damage = 0;
//        }

//        public RayE(Vector3D point, Vector3D direction, float damage)
//        {
//            Position = point;
//            Direction = direction;
//            Damage = damage;
//        }

//        public RayE(RayE ray, float damage)
//        {
//            Position = ray.Position;
//            Direction = ray.Direction;
//            Damage = damage;
//        }

//    }
//}
