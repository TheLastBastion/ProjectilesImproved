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

        Paring[] parings;

        List<EntityDesc> entities = new List<EntityDesc>();

        MatrixD transformationMatrix;
        float radiusSquared;
        Vector3D epicenter;
        BoundingSphereD sphere;
        private Stopwatch watch = new Stopwatch();
        MyStringHash id;

        public void Execute(IHitInfo hit, BulletBase bullet)
        {
            bullet.HasExpired = true;
            id = bullet.AmmoId.SubtypeId;

            radiusSquared = Radius * Radius;
            epicenter = hit.Position - (bullet.PositionMatrix.Forward * Offset);
            transformationMatrix = new MatrixD(bullet.PositionMatrix);
            transformationMatrix.Translation = epicenter + (transformationMatrix.Forward * Radius);

            watch.Restart();
            parings = ExplosionShapeGenerator.GetParings(bullet.AmmoId.SubtypeId, transformationMatrix, epicenter);
            watch.Stop();
            MyLog.Default.Info($"Pull Rays: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");

            sphere = new BoundingSphereD(hit.Position, Radius);
            List<IMyEntity> effectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            List<IMySlimBlock> temp = new List<IMySlimBlock>(); // this is only needed to get around keens function


            foreach (IMyEntity ent in effectedEntities)
            {
                watch.Restart();
                if (ent is IMyCubeGrid)
                {
                    IMyCubeGrid grid = ent as IMyCubeGrid;

                    List<IMySlimBlock> slims = GetBlocks(grid);

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
                else if (ent is IMyDestroyableObject)
                {
                    BlockEater(ent as IMyDestroyableObject, ent.WorldAABB);
                }

                watch.Stop();
                MyLog.Default.Info($"Entity Ray Casting: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");
            }

            SortLists();
            DamageBlocks((bullet.ProjectileMassDamage / parings.Length), bullet.AmmoId.SubtypeId, bullet.BlockId);
        }

        private List<IMySlimBlock> GetBlocks(IMyCubeGrid grid)
        {
            Vector3I iEpicenter = grid.WorldToGridInteger(epicenter);
            int iRadius = (int)Math.Ceiling(Radius / grid.GridSize);

            Vector3I Min = new Vector3I(iEpicenter.X - iRadius, iEpicenter.Y - iRadius, iEpicenter.Z - iRadius);
            Vector3I Max = new Vector3I(iEpicenter.X + iRadius, iEpicenter.Y + iRadius, iEpicenter.Z + iRadius);

            List<IMySlimBlock> slims = new List<IMySlimBlock>(1+iRadius*iRadius*iRadius*2);

            Vector3I loc = Vector3I.Zero;
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

        private bool BlockEater(IMyDestroyableObject obj, BoundingBoxD bounds)
        {
            double distance = (bounds.Center - epicenter).LengthSquared();
            if (distance > radiusSquared)
            {
                return false;
            }

            entities.Add(new EntityDesc(obj, distance));
            int index = entities.Count - 1;

            foreach (Paring pair in parings)
            {
                if (bounds.Intersects(pair.Ray).HasValue)
                {
                    pair.BlockList.Add(index);
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
                pair.BlockList = pair.BlockList.OrderBy(p => entities[p].DistanceSquared).ToList();
            }
            watch.Stop();
            MyLog.Default.Info($"Sort Hit Objects: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");

        }

        private void DamageBlocks(float damage, MyStringHash ammoId, long shooter)
        {
            watch.Restart();

            EntityDesc entity;
            int index;
            foreach (Paring pair in parings)
            {
                float tempDmg = damage;
                for (int i = 0; i < pair.BlockList.Count && tempDmg > 0; i++)
                {
                    index = pair.BlockList[i];
                    entity = entities[index];

                    if (entity.Destroyed) continue;

                    entity.AccumulatedDamage += tempDmg;
                    tempDmg = 0;

                    MyLog.Default.Info($"Accumulated: {entity.AccumulatedDamage}, Health: {entity.Object.Integrity}, Destroyed {entity.Destroyed}");

                    if (entity.AccumulatedDamage > entity.Object.Integrity)
                    {
                        tempDmg = entity.AccumulatedDamage - entity.Object.Integrity;
                        entity.AccumulatedDamage = entity.Object.Integrity;
                        entity.Destroyed = true;
                    }

                    entities[index] = entity;
                }
            }

            foreach (EntityDesc ent in entities)
            {
                if (ent.AccumulatedDamage > 0)
                {
                    ent.Object.DoDamage(ent.AccumulatedDamage, id, true);
                }
            }

            watch.Stop();
            MyLog.Default.Info($"Damage Time: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");
        }
    }
}