using ProjectilesImproved.Bullets;
using ProtoBuf;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;

namespace ProjectilesImproved.Effects
{
    [ProtoContract]
    public class EffectBase : IEffect
    {
        [ProtoMember(1)]
        public Ricochet Ricochet { get; set; }

        [ProtoMember(2)]
        public Explosive Explosive { get; set; }

        public void Execute(IHitInfo hit, BulletBase bullet)
        {

            if (Ricochet != null)
            {
                Ricochet.Execute(hit, bullet);
            }
            else if (Explosive != null)
            {
                Explosive.Execute(hit, bullet);
            }
            else
            {
                if (hit.HitEntity is IMyDestroyableObject)
                {
                    IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;
                    (hit.HitEntity as IMyDestroyableObject).DoDamage(bullet.ProjectileHealthDamage, bullet.AmmoId.SubtypeId, true, default(MyHitInfo), bullet.BlockId);

                    hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.PositionMatrix.Forward * bullet.ProjectileHitImpulse, hit.Position, null);

                    bullet.LastPositionFraction = hit.Fraction;
                    bullet.HasExpired = true;
                }
                else if (hit.HitEntity is IMyCubeGrid)
                {
                    IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
                    IMySlimBlock block = grid.GetCubeBlock(grid.WorldToGridInteger(hit.Position));

                    if (block == null) // cover all the bases
                    {
                        block = grid.GetCubeBlock(grid.WorldToGridInteger(hit.Position + (bullet.PositionMatrix.Forward * 0.03f)));
                    }

                    if (block != null)
                    {
                        block.DoDamage(bullet.ProjectileMassDamage, bullet.AmmoId.SubtypeId, true, default(MyHitInfo), bullet.BlockId);

                        block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.PositionMatrix.Forward * bullet.ProjectileHitImpulse, hit.Position, null);

                        bullet.LastPositionFraction = hit.Fraction;
                        bullet.HasExpired = true;
                    }
                }
            }
        }
    }
}
