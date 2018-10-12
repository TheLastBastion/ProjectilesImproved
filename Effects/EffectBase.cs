using ProjectilesImproved.Bullets;
using ProtoBuf;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    [ProtoContract]
    public class EffectBase
    {
        //[ProtoMember(1)]
        //public string Id;

        [ProtoMember(1)]
        public EffectBase NextEffect;

        public virtual void Execute(IHitInfo hit, BulletBase bullet)
        {
            if (hit.HitEntity is IMyDestroyableObject)
            {
                IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;
                (hit.HitEntity as IMyDestroyableObject).DoDamage(bullet.Ammo.ProjectileHealthDamage, bullet.Ammo.Id.SubtypeId, true, default(MyHitInfo), bullet.BlockId);

                hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.PositionMatrix.Forward * bullet.Ammo.ProjectileHitImpulse, hit.Position, null);

                bullet.LastPositionFraction = hit.Fraction;
            }
            else if (hit.HitEntity is IMyCubeGrid)
            {
                IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
                Vector3I? hitPos = grid.RayCastBlocks(bullet.PositionMatrix.Translation, bullet.End);
                if (hitPos.HasValue)
                {
                    IMySlimBlock block = grid.GetCubeBlock(hitPos.Value);
                    block.DoDamage(bullet.Ammo.ProjectileMassDamage, bullet.Ammo.Id.SubtypeId, true, default(MyHitInfo), bullet.BlockId);

                    block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.PositionMatrix.Forward * bullet.Ammo.ProjectileHitImpulse, hit.Position, null);

                    bullet.LastPositionFraction = hit.Fraction;
                }
            }

            NextEffect?.Execute(hit, bullet);
        }
    }
}
