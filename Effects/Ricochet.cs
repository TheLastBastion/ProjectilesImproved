using ProjectilesImproved.Bullets;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using VRage.Game;
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

        [ProtoMember(4)]
        public float RicochetChance { get; set; }

        public void Execute(IHitInfo hit, BulletBase bullet)
        {
            IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;
            if (hit.HitEntity is IMyCubeGrid)
            {
                IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
                Vector3I? hitPos = grid.RayCastBlocks(hit.Position, hit.Position+(bullet.PositionMatrix.Forward * 0.5));
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

            float NotHitAngle = (float)Tools.AngleBetween(-Vector3D.Normalize(relativeV), hit.Normal);
            float HitAngle = (90f - NotHitAngle);
            float NotHitFraction = NotHitAngle / 90f;

            float random = (float)Tools.Random.NextDouble();

            if (HitAngle < DeflectionAngle && RicochetChance > random)
            {
                // Apply impulse
                float impulse = bullet.ProjectileHitImpulse * NotHitFraction * MaxVelocityTransfer;
                if (hit.HitEntity.Physics != null)
                {
                    hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.Velocity * impulse * -hit.Normal, hit.Position, null);
                }
                bullet.ProjectileHitImpulse -= impulse;

                // apply partial damage
                float damage = bullet.ProjectileMassDamage * NotHitFraction * MaxDamageTransfer;
                if (obj != null && MyAPIGateway.Session.IsServer)
                {
                    obj.DoDamage(damage, bullet.AmmoId.SubtypeId, true);
                }
                bullet.ProjectileMassDamage -= damage;

                // reduce velocity
                bullet.Velocity -= bullet.Velocity * NotHitFraction * MaxVelocityTransfer;

                // reflect
                bullet.Velocity = Vector3.Reflect(bullet.Velocity, hit.Normal);

                // calculate new direction
                bullet.ResetCollisionCheck();
                bullet.PositionMatrix.Forward = Vector3D.Normalize(bullet.Velocity);
                bullet.PositionMatrix.Translation = hit.Position;

                bullet.Start = bullet.PositionMatrix.Translation + (bullet.PositionMatrix.Forward * 0.5f); // ensure it does not hit itself
                bullet.End = bullet.PositionMatrix.Translation + bullet.VelocityPerTick;

                bullet.CollisionDetection();
                bullet.Draw();

                if (!MyAPIGateway.Utilities.IsDedicated)
                {


                    MatrixD world = MatrixD.CreateFromDir(hit.Normal);
                    world.Translation = hit.Position;

                    MyParticleEffect effect;
                    MyParticlesManager.TryCreateParticleEffect("Collision_Sparks_Directional", world, out effect);

                    effect.Loop = false;
                    effect.UserScale = 0.5f;
                    effect.UserEmitterScale = 8f;
                    effect.UserRadiusMultiplier = 0.2f;
                    effect.UserBirthMultiplier = 20f;
                    effect.DurationMin = 0.01f;
                    effect.DurationMax = 0.02f;
                    effect.SetRandomDuration();
                    effect.DistanceMax = 500;

                }
            }
            else
            {
                if (obj != null && MyAPIGateway.Session.IsServer)
                {
                    obj.DoDamage(bullet.ProjectileMassDamage, bullet.AmmoId.SubtypeId, true);
                }

                bullet.HasExpired = true;
            }
        }
    }
}
