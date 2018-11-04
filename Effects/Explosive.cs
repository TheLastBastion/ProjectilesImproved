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
            watch.Start("Explode");
            bullet.HasExpired = true;

            radiusSquared = Radius * Radius;
            epicenter = hit.Position - (bullet.PositionMatrix.Forward * Offset);
            transformationMatrix = new MatrixD(bullet.PositionMatrix);
            transformationMatrix.Translation = epicenter;

            watch.Start("Pull Rays");
            ExplosionRays = ExplosionShapeGenerator.GetExplosionRays(bullet.AmmoId.ToString(), transformationMatrix, epicenter, Radius, bullet.ProjectileMassDamage);
            watch.Stop("Pull Rays");

            watch.Start("Get World Entities");
            List<IMyEntity> effectedEntities;
            BoundingSphereD sphere = new BoundingSphereD(hit.Position, Radius);
            effectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            watch.Stop("Get World Entities");

            watch.Start("Ray Tracing");
            foreach (IMyEntity ent in effectedEntities)
            {
                if (ent is IMyCubeGrid)
                {
                    watch.Start("Get Blocks");
                    List<IMySlimBlock> slims = GetBlocks(ent as IMyCubeGrid);
                    watch.Stop("Get Blocks");

                    foreach (IMySlimBlock slim in slims)
                    {
                        if (slim != null)
                        {
                            watch.Start("Get Block Bounds");
                            BoundingBoxD bounds;
                            slim.GetWorldBoundingBox(out bounds);
                            watch.Stop("Get Block Bounds");

                            watch.Start("Block Eat");
                            BlockEater(slim, bounds);
                            watch.Stop("Block Eat");
                        }
                    }
                }
                else if (ent is IMyDestroyableObject && !ent.MarkedForClose)
                {
                    BlockEater(ent as IMyDestroyableObject, ent.WorldAABB);
                }
            }
            watch.Stop("Ray Tracing");

            watch.Start("Sort Hit Objects");
            orderedEntities = entities.OrderBy(e => e.DistanceSquared);
            watch.Stop("Sort Hit Objects");

            watch.Start("Damage Time");
            DamageBlocks(bullet.AmmoId.SubtypeId, bullet.BlockId);
            watch.Stop("Damage Time");
            watch.Stop("Explode");

            watch.Write("Explode");
            watch.Write("Pull Rays");
            watch.Write("Get World Entities");
            watch.Write("Ray Tracing");
            watch.Write("Get Blocks");
            watch.Write("Get Block Bounds");
            watch.Write("Block Eat");
            watch.Write("Sort Hit Objects");
            watch.Write("Damage Time");
            watch.ResetAll();
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
            if (Max.Y > grid.Max.Y) Max.Y = grid.Max.Y + 1;
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

            RayD ray = new RayD();
            for (int i = 0; i < 8; i++)
            {
                if (!octants[i]) continue;

                for (int j = 0; j < ExplosionRays[i].Length; j++)
                {
                    RayE data = ExplosionRays[i][j];
                    ray.Position = data.Position;
                    ray.Direction = data.Direction;

                    if (bounds.GaugeIntersects(ray))
                    {
                        entity.Rays.Add(ExplosionRays[i][j]);
                    }
                }
            }

            if (Settings.DebugMode_ShowBlockOctants)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (octants[i])
                    {
                        MyVisualScriptLogicProvider.AddGPS("", "", bounds.Center, Settings.DebugOctantColors[i]);
                    }
                }
            }

            if (entity.Rays.Count > 0)
            {
                entities.Add(entity);

                if (Settings.DebugMode_ShowBlockRayIntersects)
                {
                    float value = 255f * (entity.Rays.Count / 5f);
                    MyVisualScriptLogicProvider.AddGPS(entity.Rays.Count.ToString(), "", bounds.Center, Color.FromNonPremultiplied(new Vector4(value, 0, 0, 255)), 5);
                }

            }
        }

        private void DamageBlocks(MyStringHash ammoId, long shooter)
        {
            foreach (EntityDesc entity in orderedEntities)
            {
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

                //if (Settings.DebugMode)
                //{
                //    MyLog.Default.Info($"Accum: {entity.AccumulatedDamage}, ToDo: {damageToBeDone}, Rays: {entity.Rays.Count} Dist: {entity.DistanceSquared}");
                //    MyLog.Default.Flush();
                //}

                entity.Object.DoDamage(damageToBeDone, ammoId, true, null, shooter);
            }
        }
    }
}