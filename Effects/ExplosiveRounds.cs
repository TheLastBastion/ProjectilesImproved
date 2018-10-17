using ProjectilesImproved.Bullets;
using ProtoBuf;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    [ProtoContract]
    public class ExplosiveRounds : EffectBase
    {
        [ProtoMember(2)]
        public Vector3D Epicenter { get; set; }

        [ProtoMember(3)]
        public float Radius { get; set; }

        [ProtoMember(4)]
        public bool AffectVoxels { get; set; }

        List<LinePair> pairList;
        Dictionary<IMySlimBlock, float> AccumulatedDamage = new Dictionary<IMySlimBlock, float>();

        public override void Execute(IHitInfo hit, BulletBase bullet)
        {
            // these are temp
            Epicenter = hit.Position;
            LineWriter();

            BoundingSphereD sphere = new BoundingSphereD(hit.Position, Radius);
            List<IMyEntity> effectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

            foreach (IMyEntity ent in effectedEntities)
            {
                if (ent is IMyCubeGrid)
                {
                    IMyCubeGrid grid = ent as IMyCubeGrid;

                    List<IMySlimBlock> blocks = new List<IMySlimBlock>(); //I'm like, 80% sure this will work right
                    grid.GetBlocks(blocks);
                    foreach (IMySlimBlock block in blocks)
                    {
                        BlockEater(block, grid);
                    }
                }
                else if (ent is IMyDestroyableObject)
                {
                    // add to eater
                }
            }

            SortLists();
            DamageBlocks((bullet.ProjectileMassDamage / pairList.Count), bullet.AmmoId.SubtypeId, bullet.BlockId);

            bullet.HasExpired = true;
        }

        public void LineWriter()
        {
            List<LineD> lineList = new List<LineD>();
            pairList = new List<LinePair>();

            double x = 0;
            double y = 0;
            double z = 0;
            double calcRange = 0;

            double step = Math.Atan(0.4 / Radius);
            int steps = (int)Math.Ceiling(Math.PI / step);

            for (int h = 0; h < steps; h++)
            {
                z = (1 - Math.Cos(h * step)) * Radius;
                calcRange = Math.Sin(h * step) * Radius;
                float step2 = (float)Math.Atan(0.4 / calcRange);
                int steps2 = (int)Math.Ceiling(2 * Math.PI / step2);

                for (int i = 0; i < steps2; i++)
                {
                    x = Math.Sin(i * step2) * calcRange;
                    y = Math.Cos(i * step2) * calcRange;
                    Vector3D point = new Vector3D(x, y, z);

                    //MyVisualScriptLogicProvider.AddGPS("", "", Epicenter + point, Color.Red);
                    lineList.Add(new LineD(Epicenter, Epicenter+point));
                }
            }

            foreach (LineD line in lineList)
            {
                LinePair pair = new LinePair(line, new List<BlockDesc>());
                pairList.Add(pair);
            }
        }

        private void SortLists()
        {
            for (int i = 0; i < pairList.Count; i++)
            {
                LinePair pair = pairList[i];
                List<BlockDesc> blockList = pair.blockList;
                pairList[i] = new LinePair(pairList[i].line, blockList.OrderBy(p => p.distance).ToList());
            }

        }

        private void DamageBlocks(float damage, MyStringHash ammoId, long shooter) //ok, so there's a problem with the specific way this is implemented, but if nobody notices... forget I said anything ;)
        {
            MyLog.Default.Info($"Count: {pairList.Count}, Damage: {damage}");
            foreach (LinePair pair in pairList)
            {
                float tempDmg = damage;
                //double tempDmg = lineDmg;
                for (int i = 0; i < pair.blockList.Count && tempDmg > 0; i++)
                {
                    IMySlimBlock block = pair.blockList[i].block;

                    if (!AccumulatedDamage.ContainsKey(block))
                    {
                        AccumulatedDamage.Add(block, 0);
                    }

                    float current = AccumulatedDamage[block] + tempDmg;

                    if (current >= block.Integrity)
                    {
                        tempDmg = current - block.Integrity;
                        AccumulatedDamage[block] = block.Integrity;
                        block.DoDamage(block.Integrity, ammoId, true, null, shooter);
                    }
                    else
                    {
                        AccumulatedDamage[block] += tempDmg;
                        tempDmg = 0;
                    }

                    MyLog.Default.Info($"Integrity: {pair.blockList[i].block.Integrity}, Damage Done: {AccumulatedDamage[block]}, OverKill: {tempDmg}");
                }
            }
        }

        private void BlockEater(IMySlimBlock block, IMyCubeGrid grid)
        {
            LineD checkLine;
            BoundingBoxD bounds;
            block.GetWorldBoundingBox(out bounds);

            double distance = (grid.GridIntegerToWorld(block.Position) - Epicenter).LengthSquared();
            if (distance > Radius * Radius)
            {
                return;
            }

            BlockDesc desc = new BlockDesc(block, distance);

            foreach (LinePair pair in pairList)
            {
                checkLine = pair.line;
                if (bounds.Intersects(ref checkLine))
                {
                    pair.blockList.Add(desc);
                }
            }
        }

        public struct BlockDesc
        {
            public double distance;
            public IMySlimBlock block;

            public BlockDesc(IMySlimBlock blovk, double dist)
            {
                distance = dist;
                block = blovk;
            }
        }

        public struct LinePair
        {
            public List<BlockDesc> blockList;
            public LineD line;

            public LinePair(LineD pairedLine, List<BlockDesc> list)
            {
                blockList = list;
                line = pairedLine;
            }
        }
    }
}