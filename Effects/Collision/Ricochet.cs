using ProjectilesImproved.Definitions;
using ProjectilesImproved.Projectiles;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Effects.Collision
{
    [ProtoContract]
    public class Ricochet : ICollision
    {
        private float deflectionAngle;
        [ProtoMember]
        public float DeflectionAngle
        {
            get { return deflectionAngle; }
            set
            {
                if (value < 0)
                {
                    deflectionAngle = 0;
                }
                else if (value > 90)
                {
                    deflectionAngle = 90;
                }
                else
                {
                    deflectionAngle = value;
                }
            }
        }

        private float maxVelocityTransfer;
        [ProtoMember]
        public float MaxVelocityTransfer
        {
            get { return maxVelocityTransfer; }
            set
            {
                if (value < 0)
                {
                    maxVelocityTransfer = 0;
                }
                else if (value > 1)
                {
                    maxVelocityTransfer = 1;
                }
                else
                {
                    maxVelocityTransfer = value;
                }
            }
        }

        private float maxDamageTransfer;
        [ProtoMember]
        public float MaxDamageTransfer
        {
            get { return maxDamageTransfer; }
            set
            {
                if (value < 0)
                {
                    maxDamageTransfer = 0;
                }
                else if (value > 1)
                {
                    maxDamageTransfer = 1;
                }
                else
                {
                    maxDamageTransfer = value;
                }
            }
        }

        private float ricochetChance;
        [ProtoMember]
        public float RicochetChance
        {
            get { return ricochetChance; }
            set
            {
                if (value < 0)
                {
                    ricochetChance = 0;
                }
                else if (value > 1)
                {
                    ricochetChance = 1;
                }
                else
                {
                    ricochetChance = value;
                }
            }
        }


        public void Execute(IHitInfo hit, List<IHitInfo> hitlist, Projectile bullet)
        {
            try
            {
                IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;
                if (hit.HitEntity is IMyCubeGrid)
                {
                    IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
                    Vector3D direction = bullet.Direction;
                    Vector3I? hitPos = grid.RayCastBlocks(hit.Position, hit.Position + direction);
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
                        hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, -(impulse * bullet.Direction), hit.Position, null);
                    }
                    bullet.ProjectileHitImpulse -= impulse;

                    // apply partial damage
                    float damage = bullet.ProjectileMassDamage * NotHitFraction * MaxDamageTransfer;
                    if (obj != null && MyAPIGateway.Session.IsServer)
                    {
                        Core.DamageRequests.Enqueue(new DamageDefinition
                        {
                            Victim = obj,
                            Damage = damage,
                            DamageType = MyStringHash.GetOrCompute(bullet.SubtypeId),
                            Sync = true,
                            Hit = default(MyHitInfo),
                            AttackerId = bullet.ParentBlockId
                        });
                    }
                    bullet.ProjectileMassDamage -= damage;

                    // reduce velocity
                    bullet.Velocity -= bullet.Velocity * NotHitFraction * MaxVelocityTransfer;

                    // reflect
                    bullet.Velocity = Vector3.Reflect(bullet.Velocity, hit.Normal);

                    // calculate new direction
                    bullet.ResetCollisionCheck();
                    bullet.Direction = Vector3D.Normalize(bullet.Velocity);
                    bullet.Position = hit.Position + (bullet.Direction * 0.5f);

                    bullet.PreCollitionDetection();
                    bullet.CollisionDetection();
                    //bullet.Draw();

                    if (!MyAPIGateway.Utilities.IsDedicated)
                    {
                        MatrixD world = MatrixD.CreateFromDir(hit.Normal);
                        world.Translation = hit.Position;

                        MyParticleEffect effect;
                        MyParticlesManager.TryCreateParticleEffect("Collision_Sparks_Directional", world, out effect);

                        effect.Loop = false;
                        effect.UserScale = 0.5f;
                        effect.UserEmitterScale = 16f;
                        effect.UserRadiusMultiplier = 0.1f;
                        effect.UserBirthMultiplier = 20f;
                        effect.DurationMin = 0.015f;
                        effect.DurationMax = 0.025f;
                        effect.SetRandomDuration();
                    }
                }
                else
                {
                    if (obj != null && MyAPIGateway.Session.IsServer)
                    {
                        Core.DamageRequests.Enqueue(new DamageDefinition
                        {
                            Victim = obj,
                            Damage = bullet.ProjectileMassDamage,
                            DamageType = MyStringHash.GetOrCompute(bullet.SubtypeId),
                            Sync = true,
                            Hit = default(MyHitInfo),
                            AttackerId = bullet.ParentBlockId
                        });
                    }

                    bullet.HasExpired = true;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.Info(e.ToString());
            }
        }

        public Ricochet Clone()
        {
            return new Ricochet
            {
                DeflectionAngle = DeflectionAngle,
                MaxDamageTransfer = MaxDamageTransfer,
                MaxVelocityTransfer = MaxVelocityTransfer,
                RicochetChance = RicochetChance
            };
        }
    }
}
