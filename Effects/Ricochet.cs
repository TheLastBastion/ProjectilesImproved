using ProjectilesImproved.Bullets;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    public class Ricochet : IEffect
    {
        public void Execute(IHitInfo hit, BulletBase bullet)
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

            float deflectionFactor = bullet.ProjectileMassDamage / (hitEntityHealth + bullet.ProjectileMassDamage) + 0.15f;
            float deflectionAngle0to90 = Vector3.Distance(-Vector3.Normalize(relativeV), hit.Normal) / 1.5f;

            if (deflectionAngle0to90 > deflectionFactor)
            {
                if (hit.HitEntity.Physics != null)
                {
                    float impulse = hitObjectVelocity.Length() * bullet.ProjectileHitImpulse * (1 - deflectionAngle0to90);
                    hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.Velocity * impulse * -hit.Normal, hit.Position, null);
                    bullet.ProjectileHitImpulse = bullet.ProjectileHitImpulse * (1 - deflectionAngle0to90);
                }

                if (obj != null)
                {
                    obj.DoDamage(bullet.ProjectileMassDamage * (1 - deflectionAngle0to90), bullet.AmmoId.SubtypeId, true);
                }

                bullet.Velocity = (deflectionAngle0to90 * Vector3.Reflect(relativeV, hit.Normal)) + hitObjectVelocity;
                bullet.ProjectileMassDamage = bullet.ProjectileMassDamage * deflectionAngle0to90;
                bullet.ResetCollisionCheck();

                bullet.PositionMatrix.Forward = Vector3D.Normalize(bullet.Velocity);
                bullet.PositionMatrix.Translation = hit.Position;


                bullet.Start = bullet.PositionMatrix.Translation + (bullet.PositionMatrix.Forward*0.5f); // ensure it does not hit itself
                bullet.End = bullet.PositionMatrix.Translation + bullet.VelocityPerTick;


                bullet.CollisionDetection();
                bullet.Draw();

            }
            else
            {
                if (obj != null)
                {
                    obj.DoDamage(bullet.ProjectileMassDamage, bullet.AmmoId.SubtypeId, true);
                }

                bullet.HasExpired = true;
            }

            MyLog.Default.Info($"Entity Health {hitEntityHealth}, Bullet Damage {bullet.ProjectileMassDamage}, Angle: {deflectionAngle0to90}, Degree: {deflectionAngle0to90*90}");
        }
    }
}
