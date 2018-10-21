using ProjectilesImproved.Bullets;
using ProtoBuf;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game;
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

        /// <summary>
        /// This is how far back the explosion should start from the hit position
        /// </summary>
        [ProtoMember(4)]
        public float Offset { get; set; }

        /// <summary>
        /// deform voxels?
        /// </summary>
        [ProtoMember(5)]
        public bool AffectVoxels { get; set; }

        private RayE[][] ExplosionRays;
        private List<EntityDesc> entities = new List<EntityDesc>();
        private IOrderedEnumerable<EntityDesc> orderedEntities;

        private Vector3D epicenter;
        private MatrixD transformationMatrix;

        private float radiusSquared;

        private Stopwatch watch = new Stopwatch();

        public void Execute(IHitInfo hit, BulletBase bullet)
        {
            bullet.HasExpired = true;

            radiusSquared = Radius * Radius;
            epicenter = hit.Position - (bullet.PositionMatrix.Forward * Offset);
            transformationMatrix = new MatrixD(bullet.PositionMatrix);
            transformationMatrix.Translation = epicenter + (transformationMatrix.Forward * Radius); // cause the sphere generates funny

            watch.Restart();
            ExplosionRays = ExplosionShapeGenerator.GetExplosionRays(bullet.AmmoId.SubtypeId, transformationMatrix, epicenter, bullet.ProjectileMassDamage);
            watch.Stop();
            MyLog.Default.Info($"Pull Rays: {(((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d).ToString("n4")}ms");

            watch.Restart();
            List<IMyEntity> effectedEntities;
            BoundingSphereD sphere = new BoundingSphereD(hit.Position, Radius);
            effectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            watch.Stop();
            MyLog.Default.Info($"Get Entity Objects: {(((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d).ToString("n4")}ms");

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
                            MyVisualScriptLogicProvider.AddGPS("", "", bounds.Center, Color.Red, 5);
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
            MyLog.Default.Info($"Entity Ray Casting: {(((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d).ToString("n4")}ms");

            watch.Restart();
            orderedEntities = entities.OrderBy(e => e.DistanceSquared);
            watch.Stop();
            MyLog.Default.Info($"Sort Hit Objects: {(((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d).ToString("n4")}ms");

            watch.Restart();
            DamageBlocks(bullet.AmmoId.SubtypeId, bullet.BlockId);
            watch.Stop();
            MyLog.Default.Info($"Damage Time: {(((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d).ToString("n4")}ms");
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

            List<IMySlimBlock> slims = new List<IMySlimBlock>();

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

            bool[] octants = bounds.GetOctants(epicenter);
            EntityDesc entity = new EntityDesc(obj, distance);
            entities.Add(entity);

            RayD ray = new RayD();
            for (int i = 0; i < 8; i++)
            {
                if (!octants[i]) continue;

                for (int j = 0; j < ExplosionRays[i].Length; j++)
                {
                    RayE data = ExplosionRays[i][j];
                    ray.Position = data.Position;
                    ray.Direction = data.Direction;

                    if (bounds.Intersects(ray).HasValue)
                    {
                        entity.Rays.Add(ExplosionRays[i][j]);
                    }
                }
            }
        }

        private void DamageBlocks(MyStringHash ammoId, long shooter)
        {
            foreach (EntityDesc entity in orderedEntities)
            {
                if (entity.Rays.Count == 0) continue;

                foreach (RayE ray in entity.Rays)
                {
                    entity.AccumulatedDamage += ray.Damage;
                }

                float damageToBeDone;
                if (entity.AccumulatedDamage < entity.Object.Integrity)
                {
                    damageToBeDone = entity.AccumulatedDamage;

                    for (int i = 0; i < entity.Rays.Count; i++)
                    {
                        RayE ray = entity.Rays[i];
                        ray.Damage = 0;
                    }
                }
                else
                {
                    damageToBeDone = entity.Object.Integrity;

                    float todoDamage = entity.AccumulatedDamage / entity.Rays.Count;
                    for (int i = 0; i < entity.Rays.Count; i++)
                    {
                        RayE ray = entity.Rays[i];

                        if (ray.Damage > todoDamage)
                        {
                            ray.Damage -= todoDamage;
                        }
                        else
                        {
                            todoDamage += (todoDamage - ray.Damage) / (entity.Rays.Count - (i + 1));
                        }
                    }
                }

                MyLog.Default.Info($"Accum: {entity.AccumulatedDamage}, ToBe: {damageToBeDone}, RayCount: {entity.Rays.Count} distance: {entity.DistanceSquared}");
                MyLog.Default.Flush();
                entity.Object.DoDamage(damageToBeDone, ammoId, true, null, shooter);
            }
        }
    }
}