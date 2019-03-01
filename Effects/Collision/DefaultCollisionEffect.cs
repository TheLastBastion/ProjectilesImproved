using ProjectilesImproved.Definitions;
using ProjectilesImproved.Projectiles;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Effects.Collision
{
    [ProtoContract]
    public class DefaultCollisionEffect : ICollision
    {
        [ProtoMember]
        public string AmmoId { get; set; }

        [ProtoMember]
        public bool HasBulletDrop { get; set; }

        [ProtoMember]
        public float BulletDropGravityScaler { get; set; }

        [ProtoMember]
        public bool UseOverKillSpread { get; set; }

        [ProtoMember]
        public float OverKillSpreadScaler { get; set; }

        [ProtoMember]
        public bool IgnoreDamageReduction { get; set; }

        [ProtoMember]
        public Penetration Penetration { get; set; }

        [ProtoMember]
        public Ricochet Ricochet { get; set; }

        [ProtoMember]
        public Explosive Explosive { get; set; }


        public void Execute(IHitInfo hit, List<IHitInfo> hitlist, Projectile bullet)
        {
            if (Penetration != null)
            {
                Penetration.Execute(hit, hitlist, bullet);
            }
            else if (Ricochet != null)
            {
                Ricochet.Execute(hit, null, bullet);
            }
            else if (Explosive != null)
            {
                Explosive.Execute(hit, null, bullet);
            }
            else
            {
                if (!MyAPIGateway.Session.IsServer) return;

                if (hit.HitEntity is IMyDestroyableObject)
                {
                    IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;

                    lock (Core.DamageRequests)
                    {
                        Core.DamageRequests.Enqueue(new DamageDefinition
                        {
                            Victim = (hit.HitEntity as IMyDestroyableObject),
                            Damage = bullet.ProjectileHealthDamage,
                            DamageType = MyStringHash.GetOrCompute(bullet.SubtypeId),
                            Sync = true,
                            Hit = default(MyHitInfo),
                            AttackerId = bullet.ParentBlockId
                        });
                    }

                    //(hit.HitEntity as IMyDestroyableObject).DoDamage(bullet.ProjectileHealthDamage, MyStringHash.GetOrCompute(bullet.SubtypeId), true, default(MyHitInfo), bullet.ParentBlockId);

                    hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.Direction * bullet.ProjectileHitImpulse, hit.Position, null);

                    bullet.LastPositionFraction = hit.Fraction;
                }
                else if (hit.HitEntity is IMyCubeGrid)
                {
                    IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;

                    Vector3D direction = bullet.Direction;
                    Vector3I? hitPos = grid.RayCastBlocks(hit.Position, hit.Position + direction);
                    if (hitPos.HasValue)
                    {
                        IMySlimBlock block = grid.GetCubeBlock(hitPos.Value);
                        if (IgnoreDamageReduction)
                        {
                            float mult = Tools.GetScalerInverse(((MyCubeBlockDefinition)block.BlockDefinition).GeneralDamageMultiplier);

                            lock (Core.DamageRequests)
                            {
                                Core.DamageRequests.Enqueue(new DamageDefinition
                                {
                                    Victim = block,
                                    Damage = bullet.ProjectileMassDamage * mult,
                                    DamageType = MyStringHash.GetOrCompute(bullet.SubtypeId),
                                    Sync = true,
                                    Hit = default(MyHitInfo),
                                    AttackerId = bullet.ParentBlockId
                                });
                            }

                            //block.DoDamage(bullet.ProjectileMassDamage * mult, MyStringHash.GetOrCompute(bullet.SubtypeId), true, default(MyHitInfo), bullet.ParentBlockId);
                        }
                        else
                        {
                            lock (Core.DamageRequests)
                            {
                                Core.DamageRequests.Enqueue(new DamageDefinition
                                {
                                    Victim = block,
                                    Damage = bullet.ProjectileMassDamage,
                                    DamageType = MyStringHash.GetOrCompute(bullet.SubtypeId),
                                    Sync = true,
                                    Hit = default(MyHitInfo),
                                    AttackerId = bullet.ParentBlockId
                                });
                            }

                            //block.DoDamage(bullet.ProjectileMassDamage, MyStringHash.GetOrCompute(bullet.SubtypeId), true, default(MyHitInfo), bullet.ParentBlockId);
                        }

                        block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.Direction * bullet.ProjectileHitImpulse, hit.Position, null);

                        bullet.LastPositionFraction = hit.Fraction;
                    }
                }

                bullet.HasExpired = true;
            }
        }

        public DefaultCollisionEffect Clone()
        {
            return new DefaultCollisionEffect
            {
                AmmoId = AmmoId,
                HasBulletDrop = HasBulletDrop,
                BulletDropGravityScaler = BulletDropGravityScaler,
                IgnoreDamageReduction = IgnoreDamageReduction,
                UseOverKillSpread = UseOverKillSpread,
                OverKillSpreadScaler = OverKillSpreadScaler,

                Ricochet = (Ricochet == null) ? null : Ricochet.Clone(),
                Penetration = (Penetration == null) ? null : Penetration.Clone(),
                Explosive = (Explosive == null) ? null : Explosive.Clone()
            };
        }
    }
}
