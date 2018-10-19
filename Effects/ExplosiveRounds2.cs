using ProjectilesImproved.Bullets;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    public class ExplosiveRounds2 : EffectBase
    {
        //[ProtoMember(2)]
        //public Vector3D Epicenter { get; set; }

        [ProtoMember(3)]
        public float Radius { get; set; }

        [ProtoMember(4)]
        public bool AffectVoxels { get; set; }


        private MatrixD epicenter;
        private float RadiusSquared;
        Stopwatch watch = new Stopwatch();

        public override void Execute(IHitInfo hit, BulletBase bullet)
        {
            MyLog.Default.Info($"----- Start Explosion -----");
            epicenter = new MatrixD(bullet.PositionMatrix);
            epicenter.Translation = hit.Position;
            RadiusSquared = Radius * Radius;

            watch.Start();
            BoundingSphereD sphere = new BoundingSphereD(epicenter.Translation, Radius);
            List<IMyEntity> effectedEntities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            watch.Stop();
            MyLog.Default.Info($"Entities In Sphere: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");

            List<FormatedEntity> entities = new List<FormatedEntity>();

            watch.Restart();
            foreach (IMyEntity ent in effectedEntities)
            {
                if (ent is IMyCubeGrid)
                {
                    List<IMySlimBlock> blocks = (ent as IMyCubeGrid).GetBlocksInsideSphere(ref sphere);

                    foreach (IMySlimBlock block in blocks)
                    {
                        BoundingBoxD box;
                        block.GetWorldBoundingBox(out box);

                        Vector3D localized = ent.WorldAABB.Center - epicenter.Translation;

                        entities.Add(new FormatedEntity()
                        {
                            Entity = block as IMyDestroyableObject,
                            Box = box,
                            Ray = new RayD(epicenter.Translation, localized),
                            Shielding = 0,
                            PotentialDamage = (float)(bullet.ProjectileMassDamage * (1- localized.LengthSquared() / RadiusSquared))
                        });
                    }
                }
                else if (ent is IMyDestroyableObject)
                {
                    Vector3D localized = ent.WorldAABB.Center - epicenter.Translation;

                    entities.Add(new FormatedEntity()
                    {
                        Entity = ent as IMyDestroyableObject,
                        Box = ent.WorldAABB,
                        Ray = new RayD(epicenter.Translation, localized),
                        Shielding = 0,
                        PotentialDamage = (float)(bullet.ProjectileMassDamage * (1 - localized.LengthSquared() / RadiusSquared))
                    });
                }
            }
            watch.Stop();
            MyLog.Default.Info($"Prep all entites: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");

            watch.Restart();
            for (int i = 0; i < entities.Count; i++)
            {
                FormatedEntity entity = entities[i];
                for (int j = 0; j < entities.Count; j++)
                {
                    if (entity.Shielding > entity.PotentialDamage) break;
                    if (i == j) continue;
                    
                    FormatedEntity blocker = entities[j];

                    double? nullable = blocker.Box.Intersects(entity.Ray);
                    if (nullable.HasValue)
                    {
                        entity.Shielding += blocker.Entity.Integrity;
                    }
                }
            }
            watch.Stop();
            MyLog.Default.Info($"Apply Shielding: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");

            watch.Restart();
            foreach (FormatedEntity entity in entities)
            {
                if (entity.Shielding > entity.PotentialDamage) continue;
                entity.Entity.DoDamage(entity.PotentialDamage - entity.Shielding, bullet.AmmoId.SubtypeId, true);
            }
            watch.Stop();
            MyLog.Default.Info($"Do Damage: {((float)watch.ElapsedTicks / (float)Stopwatch.Frequency) * 1000d}ms");

            MyLog.Default.Info($"----- Stop Explosion -----");
            MyLog.Default.Flush();
        }

        public struct FormatedEntity
        {
            public RayD Ray;
            public BoundingBoxD Box;
            public IMyDestroyableObject Entity;
            public float Shielding;
            public float PotentialDamage;
        }
    }
}
