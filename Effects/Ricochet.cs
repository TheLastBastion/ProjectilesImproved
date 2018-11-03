using ProjectilesImproved.Bullets;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
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
                Vector3I? hitPos = grid.RayCastBlocks(hit.Position, bullet.PositionMatrix.Forward * 0.5);
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
            Vector3D relativeV = bullet.Velocity - hitObjectVelocity;

            float HitAngle0to90 = (float)Tools.AngleBetween(-Vector3D.Normalize(relativeV), hit.Normal);
            float NotHitAngle = (1 - HitAngle0to90);

            if ((HitAngle0to90 * 90) < DeflectionAngle)
            {
                // Apply impulse
                float impulse = bullet.ProjectileHitImpulse * NotHitAngle * MaxVelocityTransfer;
                if (hit.HitEntity.Physics != null)
                {
                    hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.Velocity * impulse * -hit.Normal, hit.Position, null);
                }
                bullet.ProjectileHitImpulse -= impulse;

                // apply partial damage
                float damage = bullet.ProjectileMassDamage * NotHitAngle * MaxDamageTransfer;
                if (obj != null && MyAPIGateway.Session.IsServer)
                {
                    obj.DoDamage(damage, bullet.AmmoId.SubtypeId, true);
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
                if (obj != null && MyAPIGateway.Session.IsServer)
                {
                    obj.DoDamage(bullet.ProjectileMassDamage, bullet.AmmoId.SubtypeId, true);
                }

                bullet.HasExpired = true;
            }

            MyLog.Default.Info($"Damage {bullet.ProjectileMassDamage}, Velocity {bullet.Velocity.Length()}, Angle: {HitAngle0to90} : {HitAngle0to90*90}, NotAngle: {NotHitAngle} : {NotHitAngle * 90}");
        }
    }
}
