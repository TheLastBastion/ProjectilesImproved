using ProjectilesImproved.Bullets;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    [ProtoContract]
    public class AmmoEffect
    {
        public static long hits = 0;
        public static long misses = 0;

        [ProtoMember(1)]
        public string AmmoId { get; set; }

        [ProtoMember(2)]
        public bool HasBulletDrop { get; set; }

        [ProtoMember(3)]
        public float BulletDropGravityScaler { get; set; }

        [ProtoMember(4)]
        public bool UseOverKillSpread { get; set; }

        [ProtoMember(5)]
        public float OverKillSpreadScaler { get; set; }

        [ProtoMember(6)]
        public bool IgnoreDamageReduction { get; set; }

        [ProtoMember(7)]
        public Penetration Penetration { get; set; }

        [ProtoMember(8)]
        public Ricochet Ricochet { get; set; }

        [ProtoMember(9)]
        public Explosive Explosive { get; set; }


        public void Execute(IHitInfo hit, List<IHitInfo> hitlist, BulletBase bullet)
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
                    (hit.HitEntity as IMyDestroyableObject).DoDamage(bullet.ProjectileHealthDamage, bullet.AmmoId.SubtypeId, true, default(MyHitInfo), bullet.BlockId);

                    hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.PositionMatrix.Forward * bullet.ProjectileHitImpulse, hit.Position, null);

                    bullet.LastPositionFraction = hit.Fraction;
                }
                else if (hit.HitEntity is IMyCubeGrid)
                {
                    IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;

                    Vector3D direction = bullet.PositionMatrix.Forward;
                    Vector3I? hitPos = grid.RayCastBlocks(hit.Position, hit.Position + direction);
                    if (hitPos.HasValue)
                    {
                        IMySlimBlock block = grid.GetCubeBlock(hitPos.Value);
                        if (IgnoreDamageReduction)
                        {
                            float mult = Tools.GetScalerInverse(((MyCubeBlockDefinition)block.BlockDefinition).GeneralDamageMultiplier);

                            block.DoDamage(bullet.ProjectileMassDamage*mult, bullet.AmmoId.SubtypeId, true, default(MyHitInfo), bullet.BlockId);
                        }
                        else
                        {
                            block.DoDamage(bullet.ProjectileMassDamage, bullet.AmmoId.SubtypeId, true, default(MyHitInfo), bullet.BlockId);
                        }

                        block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.PositionMatrix.Forward * bullet.ProjectileHitImpulse, hit.Position, null);

                        bullet.LastPositionFraction = hit.Fraction;
                        hits++;
                    }
                    else
                    {
                        misses++;
                    }
                }

                bullet.HasExpired = true;
            }
        }
    }
}
