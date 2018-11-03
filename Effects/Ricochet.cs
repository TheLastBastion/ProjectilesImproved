using ProjectilesImproved.Bullets;
using ProtoBuf;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    [ProtoContract]
    public class Ricochet : IEffect
    {

        [ProtoMember(1)]
        public float DeflectionAngle { get; set; }

        [ProtoMember(2)]
        public float MaxVelocityTransfer { get; set; }

        [ProtoMember(3)]
        public float MaxDamageTransfer { get; set; }

        public void Execute(IHitInfo hit, BulletBase bullet)
        {
            IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;
            if (hit.HitEntity is IMyCubeGrid)
            {
                IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
                Vector3I? hitPos = grid.RayCastBlocks(hit.Position, bullet.PositionMatrix.Forward*0.5);
                if (hitPos.HasValue)
                {
                    obj = grid.GetCubeBlock(hitPos.Value);
                }
            }

            Vector3 hitObjectVelocity = Vector3.Zero;
            if (hit.HitEntity.Physics != null)
            {
                hitObjectVelocity = hit.HitEntity.Physics.LinearVelocity;
            }
            Vector3 relativeV = bullet.Velocity - hitObjectVelocity;

            float HitAngle0to90 = Vector3.Distance(-Vector3.Normalize(relativeV), hit.Normal);

            if ((HitAngle0to90*90) < DeflectionAngle)
            {
                float NotHitAngle = (1 - HitAngle0to90);

                // Apply impulse
                float impulse = bullet.ProjectileHitImpulse * NotHitAngle * MaxVelocityTransfer;
                if (hit.HitEntity.Physics != null)
                {
                    hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.Velocity * impulse * -hit.Normal, hit.Position, null);
                }
                bullet.ProjectileHitImpulse -= impulse;

                // apply partial damage
                float damage = bullet.ProjectileMassDamage * NotHitAngle * MaxDamageTransfer;
                if (obj != null)
                {
                    obj.DoDamage(damage, bullet.AmmoId.SubtypeId, false);
                }
                bullet.ProjectileMassDamage -= damage;

                // reduce velocity
                bullet.Velocity -= bullet.Velocity * NotHitAngle * MaxVelocityTransfer;

                // calculate new direction
                bullet.ResetCollisionCheck();
                bullet.PositionMatrix.Forward = Vector3D.Normalize(bullet.Velocity);
                bullet.PositionMatrix.Translation = hit.Position;

                bullet.Start = bullet.PositionMatrix.Translation + (bullet.PositionMatrix.Forward * 0.5f); // ensure it does not hit itself
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
        }
    }
}
