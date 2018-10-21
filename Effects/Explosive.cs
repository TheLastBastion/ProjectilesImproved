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

        ExplosionRay[][] explosionRays;

        private MatrixD transformationMatrix;
        private float radiusSquared;
        private Vector3D epicenter;

        private List<EntityDesc> entities = new List<EntityDesc>();
        private Stopwatch watch = new Stopwatch();

        public void Execute(IHitInfo hit, BulletBase bullet)
        {
            bullet.HasExpired = true;

            radiusSquared = Radius * Radius;
            epicenter = hit.Position - (bullet.PositionMatrix.Forward * Offset);
            transformationMatrix = new MatrixD(bullet.PositionMatrix);
            transformationMatrix.Translation = epicenter + (transformationMatrix.Forward * Radius); // cause the sphere generates funny

            watch.Restart();
            explosionRays = ExplosionShapeGenerator.GetExplosionRays(bullet.AmmoId.SubtypeId, transformationMatrix, epicenter);
            watch.Stop();
            MyLog.Default.Info($"Pull Rays: {(((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d).ToString("n4")}ms");

            watch.Restart();
            List<IMyEntity> effectedEntities;
            if (Radius > 15)
            {
                BoundingBoxD box = new BoundingBoxD(hit.Position - Radius, hit.Position + Radius);
                effectedEntities = MyAPIGateway.Entities.GetElementsInBox(ref box);
            }
            else
            {
                BoundingSphereD sphere = new BoundingSphereD(hit.Position, Radius);
                effectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            }
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
            SortLists();
            watch.Stop();
            MyLog.Default.Info($"Sort Hit Objects: {(((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d).ToString("n4")}ms");

            watch.Restart();
            DamageBlocks(bullet.ProjectileMassDamage / explosionRays.Length, bullet.AmmoId.SubtypeId, bullet.BlockId);
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
            EntityDesc ent = new EntityDesc(obj, distance);
            entities.Add(ent);
            int index = entities.Count - 1;

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
            for (int i = 0; i < explosionRays.Length; i++)
            {
                for (int j = 0; j < explosionRays[i].Length; j++)
                {
                    ExplosionRay pair = explosionRays[i][j];
                    pair.BlockList = pair.BlockList.OrderBy(p => entities[p].DistanceSquared).ToList();
                }
            }
        }

        private void DamageBlocks(float damage, MyStringHash ammoId, long shooter)
        {
            for (int i = 0; i < 8; i++)
            {
                foreach (ExplosionRay ray in explosionRays[i])
                {
                    float tempDmg = damage;
                    for (int j = 0; j < ray.BlockList.Count && tempDmg > 0; j++)
                    {

                        EntityDesc desc = entities[ray.BlockList[j]];

                        if (tempDmg < desc.Object.Integrity)
                        {
                            desc.AccumulatedDamage += tempDmg;
                        }
                        else
                        {
                            float todoDmg = tempDmg;
                            tempDmg = tempDmg - desc.Object.Integrity;
                            desc.AccumulatedDamage += todoDmg;
                        }

                        entities[ray.BlockList[j]] = desc;
                    }

                    foreach (EntityDesc desc in entities)
                    {
                        if (desc.AccumulatedDamage > 0)
                        {
                            desc.Object.DoDamage(desc.AccumulatedDamage, ammoId, true, null, shooter);
                        }
                    }
                }
            }
        }
    }
}