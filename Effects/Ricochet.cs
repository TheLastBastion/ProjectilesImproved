using ProjectilesImproved.Bullets;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    public class Ricochet : EffectBase
    {
        public override void Execute(IHitInfo hit, BulletBase bullet)
        {
            float hitEntityHealth = 5000; // default value if voxel or something

            IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;
            if (obj != null)
            {
                hitEntityHealth = obj.Integrity;
            }
            else if (hit.HitEntity is IMyCubeGrid)
            {
                IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
                Vector3I? hitPos = grid.RayCastBlocks(bullet.PositionMatrix.Translation, bullet.VelocityPerTick);
                if (hitPos.HasValue)
                {
                    obj = grid.GetCubeBlock(hitPos.Value);
                    hitEntityHealth = obj.Integrity;
                }
            }

            Vector3 hitObjectVelocity = Vector3.Zero;
            if (hit.HitEntity.Physics != null)
            {
                hitObjectVelocity = hit.HitEntity.Physics.LinearVelocity;
            }

            Vector3 relativeV = bullet.Velocity - hitObjectVelocity;

            float deflectionFactor = bullet.Ammo.ProjectileMassDamage / (hitEntityHealth + bullet.Ammo.ProjectileMassDamage);
            float deflectionAngle0to90 = Vector3.Distance(-Vector3.Normalize(relativeV), hit.Normal) / 1.5f;

            if (deflectionAngle0to90 > deflectionFactor)
            {
                if (hit.HitEntity.Physics != null)
                {
                    float impulse = hitObjectVelocity.Length() * bullet.Ammo.ProjectileHitImpulse * (1- deflectionAngle0to90);
                    hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.Velocity * impulse * -hit.Normal, hit.Position, null);
                }

                if (obj != null)
                {
                    obj.DoDamage(bullet.Ammo.ProjectileMassDamage * (1 - deflectionAngle0to90), bullet.Ammo.Id.SubtypeId, true);
                }

                bullet.Velocity = (deflectionAngle0to90 * Vector3.Reflect(relativeV, hit.Normal)) + hitObjectVelocity;
                bullet.Ammo.ProjectileMassDamage = bullet.Ammo.ProjectileMassDamage * deflectionAngle0to90;
            }
            else
            {
                if (obj != null)
                {
                    obj.DoDamage(bullet.Ammo.ProjectileMassDamage, bullet.Ammo.Id.SubtypeId, true);
                }
            }

            MyLog.Default.Info($"Entity Health {hitEntityHealth}, Bullet Damage {bullet.Ammo.ProjectileMassDamage}, Angle: {deflectionAngle0to90}, Degree: {deflectionAngle0to90*90}");
        }
    }
}
