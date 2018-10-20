using ProjectilesImproved.Bullets;
using ProtoBuf;
using Sandbox.Game.Entities;
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

        Paring[] parings;
        Dictionary<IMySlimBlock, float> AccumulatedDamage = new Dictionary<IMySlimBlock, float>();
        MatrixD transformationMatrix;
        float radiusSquared;
        Vector3D epicenter;
        private Stopwatch watch = new Stopwatch();

        //public Explosive()
        //{
        //    if (ExplosionShapeGenerator.Instance == null)
        //    {
        //        ExplosionShapeGenerator.Instance = new ExplosionShapeGenerator();
        //    }

        //    ExplosionShapeGenerator.Instance.Generate()
        //}

        public void Execute(IHitInfo hit, BulletBase bullet)
        {
            radiusSquared = Radius * Radius;
            epicenter = hit.Position - (bullet.PositionMatrix.Forward * Offset);
            transformationMatrix = new MatrixD(bullet.PositionMatrix);
            transformationMatrix.Translation = epicenter + (transformationMatrix.Forward * Radius);

            parings = ExplosionShapeGenerator.GetParings(bullet.AmmoId.SubtypeId, transformationMatrix, epicenter);

            BoundingSphereD sphere = new BoundingSphereD(hit.Position, Radius);
            List<IMyEntity> effectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);


            foreach (IMyEntity ent in effectedEntities)
            {
                if (ent is IMyCubeGrid)
                {
                    watch.Restart();
                    IMyCubeGrid grid = ent as IMyCubeGrid;
                    List<IMySlimBlock> blocks = new List<IMySlimBlock>(); // this is only needed to get around keens function

                    grid.GetBlocks(blocks);
                    watch.Stop();
                    MyLog.Default.Info($"Verify Block: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");


                    watch.Restart();
                    foreach (IMySlimBlock block in blocks)
                    {
                        BlockEater(block);
                    }
                    watch.Stop();
                    MyLog.Default.Info($"Block Eater: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");
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
            for (int i = 0; i < parings.Length; i++)
            {
                Paring pair = parings[i];
                pair.BlockList = pair.BlockList.OrderBy(p => p.DistanceSqud).ToList();
                //parings[i] = new Paring(parings[i].Point, pair.BlockList.OrderBy(p => p.DistanceSqud).ToList());
            }
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