using ProjectilesImproved.Bullets;
using ProtoBuf;
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

        //Dictionary<IMySlimBlock, float> AccumulatedDamage = new Dictionary<IMySlimBlock, float>();

        ExplosionRay[][] explosionRays;

        List<EntityDesc> entities = new List<EntityDesc>();

        MatrixD transformationMatrix;
        float radiusSquared;
        Vector3D epicenter;
        BoundingSphereD sphere;
        private Stopwatch watch = new Stopwatch();
        MyStringHash id;
        long attackerId;

        public void Execute(IHitInfo hit, BulletBase bullet)
        {
            //MyAPIGateway.Parallel.StartBackground(() =>
            //{
                entities.Clear();
                bullet.HasExpired = true;
                id = bullet.AmmoId.SubtypeId;
                attackerId = bullet.BlockId;

                radiusSquared = Radius * Radius;
                epicenter = hit.Position - (bullet.PositionMatrix.Forward * Offset);
                transformationMatrix = new MatrixD(bullet.PositionMatrix);
                transformationMatrix.Translation = epicenter + (transformationMatrix.Forward * Radius); // cause the sphere generates funny

                watch.Restart();
                explosionRays = ExplosionShapeGenerator.GetExplosionRays(bullet.AmmoId.SubtypeId, transformationMatrix, epicenter);
                watch.Stop();
                MyLog.Default.Info($"Pull Rays: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");

                sphere = new BoundingSphereD(hit.Position, Radius);
                List<IMyEntity> effectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
                List<IMySlimBlock> temp = new List<IMySlimBlock>(); // this is only needed to get around keens function

                watch.Restart();
                foreach (IMyEntity ent in effectedEntities)
                {

                    if (ent is IMyCubeGrid)
                    {
                        List<IMySlimBlock> slims = GetBlocks(ent as IMyCubeGrid);

                        foreach (IMySlimBlock slim in slims)
                        {
                            if (slim != null)
                            {
                                BoundingBoxD bounds;
                                slim.GetWorldBoundingBox(out bounds);
                                BlockEater(slim, bounds);
                            }
                        }
                    }
                    else if (ent is IMyDestroyableObject && !ent.MarkedForClose)
                    {
                        BlockEater(ent as IMyDestroyableObject, ent.WorldAABB);
                    }
                }
                watch.Stop();
                MyLog.Default.Info($"Entity Ray Casting: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");

                SortLists();
                //DamageBlocks((bullet.ProjectileMassDamage / parings.Length), bullet.AmmoId.SubtypeId, bullet.BlockId);
            //});
        }

        private List<IMySlimBlock> GetBlocks(IMyCubeGrid grid)
        {
            Vector3I center = grid.WorldToGridInteger(epicenter);
            int iRadius = (int)Math.Ceiling(Radius / grid.GridSize);

            Vector3I Min = new Vector3I(center.X - iRadius, center.Y - iRadius, center.Z - iRadius);
            Vector3I Max = new Vector3I(center.X + iRadius, center.Y + iRadius, center.Z + iRadius);

            if (Min.X < grid.Min.X) Min.X = grid.Min.X;
            if (Min.Y < grid.Min.Y) Min.Y = grid.Min.Y;
            if (Min.Z < grid.Min.Z) Min.Z = grid.Min.Z;
            if (Max.X > grid.Max.X) Max.X = grid.Max.X;
            if (Max.Y > grid.Max.Y) Max.Y = grid.Max.Y;
            if (Max.Z > grid.Max.Z) Max.Z = grid.Max.Z;

            int iVolume = (Max - Min).Volume();

            List<IMySlimBlock> slims = new List<IMySlimBlock>(1 + iVolume);

            Vector3I loc = Vector3I.Zero;
            slims.Add(grid.GetCubeBlock(center));

            for (loc.X = Min.X; loc.X < Max.X; loc.X++)
            {
                for (loc.Y = Min.Y; loc.Y < Max.Y; loc.Y++)
                {
                    for (loc.Z = Min.Z; loc.Z < Max.Z; loc.Z++)
                    {
                        slims.Add(grid.GetCubeBlock(loc));
                    }
                }
            }

            return slims;
        }

        private void BlockEater(IMyDestroyableObject obj, BoundingBoxD bounds)
        {
            double distance = (bounds.Center - epicenter).LengthSquared();
            if (distance > radiusSquared)
            {
                return;
            }

            entities.Add(new EntityDesc(obj, distance));

            int index = entities.Count - 1;
            bool[] octants = bounds.GetOctants(epicenter);
            RayD ray = new RayD();

            for (int i = 0; i < 8; i++)
            {
                if (!octants[i]) continue;

                foreach (ExplosionRay rayData in explosionRays[i])
                {
                    ray.Position = rayData.Position;
                    ray.Direction = rayData.Direction;

                    if (ray.Intersects(bounds).HasValue)
                    {
                        rayData.BlockList.Add(index);
                    }
                }
            }
        }

        private void SortLists()
        {
            watch.Restart();
            for (int i = 0; i < explosionRays.Length; i++)
            {
                for (int j = 0; j < explosionRays[i].Length; j++)
                {
                    ExplosionRay pair = explosionRays[i][j];
                    //pair.BlockList.Sort(i => entities[i].DistanceSquared);
                    pair.BlockList = pair.BlockList.OrderBy(p => entities[p].DistanceSquared).ToList();
                }
            }
            watch.Stop();
            MyLog.Default.Info($"Sort Hit Objects: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");

        }

        //private void DamageBlocks(float damage, MyStringHash ammoId, long shooter)
        //{
        //    watch.Restart();
        //    foreach (ExplosionRay pair in explosionRays)
        //    {
        //        float tempDmg = damage;
        //        for (int i = 0; i < pair.BlockList.Count && tempDmg > 0; i++)
        //        {
        //            int index = pair.BlockList[i];
        //            EntityDesc entity = entities[index];

        //            if (entity.Destroyed) continue;

        //            entity.AccumulatedDamage += tempDmg;
        //            tempDmg = 0;

        //            if (entity.AccumulatedDamage > entity.Object.Integrity)
        //            {
        //                tempDmg = entity.AccumulatedDamage - entity.Object.Integrity;
        //                entity.AccumulatedDamage = entity.Object.Integrity;
        //                entity.Destroyed = true;
        //            }

        //            entities[index] = entity;
        //        }
        //    }

        //    foreach (EntityDesc ent in entities)
        //    {
        //        if (ent.AccumulatedDamage > 0)
        //        {
        //            ent.Object.DoDamage(ent.AccumulatedDamage, id, true, null, attackerId);
        //        }
        //    }

        //    watch.Stop();
        //    MyLog.Default.Info($"Damage Time: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");
        //}
    }
}