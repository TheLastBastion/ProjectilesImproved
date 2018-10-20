using ProjectilesImproved.Bullets;
using ProtoBuf;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    [ProtoContract]
    public class Explosive : IEffect
    {
        [ProtoMember(1)]
        public float Radius { get; set; }

        /// <summary>
        /// The blast density (default 0.5)
        /// less than 0.5 is super dense
        /// higher than 0.5 is will start missing blocks
        /// </summary>
        [ProtoMember(2)]
        public float Resolution { get; set; }

        /// <summary>
        /// 180 is a sphere. Less is a cone.
        /// </summary>
        [ProtoMember(3)]
        public float Angle { get; set; }

        [ProtoMember(4)]
        public float Offset { get; set; }

        [ProtoMember(5)]
        public bool AffectVoxels { get; set; }

        Dictionary<IMySlimBlock, float> AccumulatedDamage = new Dictionary<IMySlimBlock, float>();

        Paring[] parings;
        MatrixD transformationMatrix;
        float radiusSquared;
        Vector3D epicenter;
        BoundingSphereD sphere;
        private Stopwatch watch = new Stopwatch();

        public void Execute(IHitInfo hit, BulletBase bullet)
        {
            radiusSquared = Radius * Radius;
            epicenter = hit.Position - (bullet.PositionMatrix.Forward * Offset);
            transformationMatrix = new MatrixD(bullet.PositionMatrix);
            transformationMatrix.Translation = epicenter + (transformationMatrix.Forward * Radius);

            watch.Restart();
            parings = ExplosionShapeGenerator.GetParings(bullet.AmmoId.SubtypeId, transformationMatrix, epicenter);
            watch.Stop();
            MyLog.Default.Info($"Verify Block: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");

            sphere = new BoundingSphereD(hit.Position, Radius);
            List<IMyEntity> effectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            List<IMySlimBlock> temp = new List<IMySlimBlock>(); // this is only needed to get around keens function


            foreach (IMyEntity ent in effectedEntities)
            {
                if (ent is IMyCubeGrid)
                {
                    watch.Restart();

                    IMyCubeGrid grid = ent as IMyCubeGrid;

                    //grid.GetBlocks(temp, BlockEater);
                    //watch.Stop();


                    IMySlimBlock[] slims = GetBlocks(grid);

                    foreach (IMySlimBlock slim in slims)
                    {
                        if (slim != null)
                        {
                            BlockEater(slim);
                        }
                    }

                    MyLog.Default.Info($"Verify + Eater: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");
                }
                else if (ent is IMyDestroyableObject)
                {
                    // add to eater
                }
            }

            SortLists();
            DamageBlocks((bullet.ProjectileMassDamage / parings.Length), bullet.AmmoId.SubtypeId, bullet.BlockId);

            bullet.HasExpired = true;
        }

        private IMySlimBlock[] GetBlocks(IMyCubeGrid grid)
        {
            Vector3I iEpicenter = grid.WorldToGridInteger(epicenter);

            int iRadius = (int)(Radius / grid.GridSize);
            int iDiameter = iRadius * 2;
            int iVolume = iDiameter * iDiameter * iDiameter;

            IMySlimBlock[] slims = new IMySlimBlock[iVolume];

            int xMin = iEpicenter.X - iRadius;
            int xMax = iEpicenter.X + iRadius;
            int yMin = iEpicenter.Y - iRadius;
            int yMax = iEpicenter.Y + iRadius;
            int zMin = iEpicenter.Z - iRadius;
            int zMax = iEpicenter.Z + iRadius;

            Vector3I loc = Vector3I.Zero;

            int index = 0;
            for (loc.X = xMin; loc.X < xMax; loc.X++)
            {
                for (loc.Y = yMin; loc.Y < yMax; loc.Y++)
                {
                    for (loc.Z = zMin; loc.Z < zMax; loc.Z++)
                    {
                        IMySlimBlock slim = grid.GetCubeBlock(loc);

                        slims[index] = slim;
                        index++;
                    }
                }
            }

            return slims;
        }

        private bool BlockEater(IMySlimBlock block)
        {
            BoundingBoxD bounds;
            block.GetWorldBoundingBox(out bounds);

            double distance = (bounds.Center - epicenter).LengthSquared();
            if (distance > radiusSquared)
            {
                return false;
            }

            foreach (Paring pair in parings)
            {
                if (bounds.Intersects(pair.Ray).HasValue)
                {
                    pair.BlockList.Add(new BlockDesc(block, distance));
                }
            }

            return false;
        }

        private void SortLists()
        {
            watch.Restart();

            MyAPIGateway.Parallel.For(0, parings.Length, (i) =>
            {
                Paring pair = parings[i];
                pair.BlockList = pair.BlockList.OrderBy(p => p.DistanceSqud).ToList();
            });

            //for (int i = 0; i < parings.Length; i++)
            //{
            //    Paring pair = parings[i];
            //    pair.BlockList = pair.BlockList.OrderBy(p => p.DistanceSqud).ToList();
            //}
            watch.Stop();
            MyLog.Default.Info($"Sort Hit Objects: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");

        }

        private void DamageBlocks(float damage, MyStringHash ammoId, long shooter)
        {
            watch.Restart();
            foreach (Paring pair in parings)
            {
                float tempDmg = damage;
                for (int i = 0; i < pair.BlockList.Count && tempDmg > 0; i++)
                {
                    IMySlimBlock block = pair.BlockList[i].Block;

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
                }
            }

            watch.Stop();
            MyLog.Default.Info($"Damage Time: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");
        }
    }
}