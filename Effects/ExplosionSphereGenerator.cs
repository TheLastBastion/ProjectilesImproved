using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    public class ExplosionShapeGenerator
    {
        public static ExplosionShapeGenerator Instance;

        public Dictionary<MyStringHash, Paring[]> ShapeLookup = new Dictionary<MyStringHash, Paring[]>();

        /// <summary>
        /// Pregenerates all the explosions for each ammo type
        /// Note: should be ran once on game ready. (after all mods are loaded)
        /// </summary>
        public ExplosionShapeGenerator()
        {
            //MyAmmoDefinition  MyDefinitionManager.Static.GetAllDefinitions<MyAmmoDefinition>();

            writer(MyStringHash.GetOrCompute("OKI230mmAmmoPars"), 5, 0.5f, 180);
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="radius"></param>
        /// <param name="resolution"></param>
        /// <param name="maxAngle">0 to 180</param>
        private void writer(MyStringHash id, float radius, float resolution, float maxAngle)
        {
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
                    parings.Add(new Paring(new Vector3D(x, y, z), new List<BlockDesc>()));
                }
            }

            if (!ShapeLookup.ContainsKey(id))
            {
                ShapeLookup.Add(id, parings.ToArray());
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
            public Vector3D Point;
            public List<BlockDesc> BlockList;

            public Paring(Vector3D point, List<BlockDesc> list)
            {
                BlockList = list;
                Point = point;
            }
        }
    }
}
