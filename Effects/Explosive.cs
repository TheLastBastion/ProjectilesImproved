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


                    List<IMySlimBlock> slims = GetBlocks(grid);

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

        private List<IMySlimBlock> GetBlocks(IMyCubeGrid grid)
        {
            Vector3I iEpicenter = grid.WorldToGridInteger(epicenter);
            int iRadius = (int)(Radius / grid.GridSize);

            //Vector3I gridMin = new Vector3I(grid.PositionComp.LocalAABB.Min / grid.GridSize);
            //Vector3I gridMax = new Vector3I(grid.PositionComp.LocalAABB.Max / grid.GridSize);

            Vector3I Min = new Vector3I(iEpicenter.X - iRadius, iEpicenter.Y - iRadius, iEpicenter.Z - iRadius);
            Vector3I Max = new Vector3I(iEpicenter.X + iRadius, iEpicenter.Y + iRadius, iEpicenter.Z + iRadius);

            //if (Min.X < gridMin.X) Min.X = gridMin.X;
            //if (Min.Y < gridMin.Y) Min.Y = gridMin.Y;
            //if (Min.Z < gridMin.Z) Min.Z = gridMin.Z;

            //if (Max.X > gridMax.X) Max.X = gridMax.X;
            //if (Max.Y > gridMax.Y) Max.Y = gridMax.Y;
            //if (Max.Z > gridMax.Z) Max.Z = gridMax.Z;

            int iVolume = (Max - Min).Volume();
            if (iVolume < 0) iVolume = 1;

            List<IMySlimBlock> slims = new List<IMySlimBlock>(iVolume);

            Vector3I loc = Vector3I.Zero;
            for (loc.X = Min.X; loc.X < Max.X; loc.X++)
            {
                for (loc.Y = Min.Y; loc.Y < Max.Y; loc.Y++)
                {
                    for (loc.Z = Min.Z; loc.Z < Max.Z; loc.Z++)
                    {
                        if ((iEpicenter-loc).RectangularLength() <= iRadius)
                        {
                            slims.Add(grid.GetCubeBlock(loc));
                        }

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