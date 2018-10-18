using ProjectilesImproved.Bullets;
using ProtoBuf;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using static ProjectilesImproved.Effects.ExplosionShapeGenerator;

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

        Paring[] parings;
        Dictionary<IMySlimBlock, float> AccumulatedDamage = new Dictionary<IMySlimBlock, float>();
        MatrixD hitPositionMatrix;

        public override void Execute(IHitInfo hit, BulletBase bullet)
        {
            // these are temp
            Epicenter = hit.Position;
            parings = ExplosionShapeGenerator.Instance.ShapeLookup[bullet.AmmoId.SubtypeId];

            hitPositionMatrix = new MatrixD(bullet.PositionMatrix);
            hitPositionMatrix.Translation = hit.Position;

            BoundingSphereD sphere = new BoundingSphereD(hit.Position, Radius);
            List<IMyEntity> effectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

            foreach (IMyEntity ent in effectedEntities)
            {
                if (ent is IMyCubeGrid)
                {
                    IMyCubeGrid grid = ent as IMyCubeGrid;

                    List<IMySlimBlock> blocks = new List<IMySlimBlock>(); //I'm like, 80% sure this will work right
                    grid.GetBlocks(blocks);

                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    foreach (IMySlimBlock block in blocks)
                    {
                        BlockEater(block);
                    }
                    watch.Stop();
                    MyAPIGateway.Utilities.ShowNotification($"Block Eater Time: {watch.ElapsedTicks} Ticks, {Stopwatch.Frequency} Frequency", 10000);
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

        private void BlockEater(IMySlimBlock block)
        {
            //LineD checkLine;
            BoundingBoxD bounds;
            block.GetWorldBoundingBox(out bounds);

            double distance = (block.CubeGrid.GridIntegerToWorld(block.Position) - Epicenter).LengthSquared();
            if (distance > Radius * Radius)
            {
                return;
            }

            BlockDesc desc = new BlockDesc(block, distance);

            foreach (Paring pair in parings)
            {
                Vector3D translatedPoint = Vector3D.Transform(pair.Point, hitPositionMatrix);
                LineD line = new LineD(Epicenter, translatedPoint);

                if (bounds.Intersects(ref line))
                {
                    pair.BlockList.Add(desc);
                }
            }
        }

        private void SortLists()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < parings.Length; i++)
            {
                Paring pair = parings[i];
                parings[i] = new Paring(parings[i].Point, pair.BlockList.OrderBy(p => p.DistanceSqud).ToList());
            }
            watch.Stop();
            MyAPIGateway.Utilities.ShowNotification($"Sorting Time: {watch.ElapsedTicks} Ticks, {Stopwatch.Frequency} Frequency", 10000);

        }

        private void DamageBlocks(float damage, MyStringHash ammoId, long shooter) //ok, so there's a problem with the specific way this is implemented, but if nobody notices... forget I said anything ;)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

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

                    //MyLog.Default.Info($"Integrity: {pair.BlockList[i].Block.Integrity}, Damage Done: {AccumulatedDamage[block]}, OverKill: {tempDmg}");
                }
            }

            watch.Stop();
            MyAPIGateway.Utilities.ShowNotification($"Sorting Time: {watch.ElapsedTicks} Ticks, {Stopwatch.Frequency} Frequency", 10000);
        }
    }
}