using ProjectilesImproved.Definitions;
using ProjectilesImproved.Effects.Flight;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Projectiles
{
    public class Projectile : ProjectileDefinition
    {
        public static float MaxSpeedLimit => (MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed > MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed) ?
            MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed : MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;

        public static MyStringId DefaultProjectileTrail = MyStringId.GetOrCompute("ProjectileTrailLine");

        public const float Tick = 1f / 60f;
        public Vector3D VelocityPerTick => Velocity * Tick;
        public bool IsAtRange => DistanceTraveled * LifeTimeTicks > MaxTrajectory * MaxTrajectory;
        public bool UseLongRaycast => DesiredSpeed * Tick * CollisionCheckFrames > 50;

        public long BlockId;

        public IMySlimBlock Slim;

        public MatrixD PositionMatrix;

        public double DistanceTraveled;

        public Vector3D InitialGridVelocity;

        public Vector3D Velocity;

        public IFlight FlightEffect { get; set; }

        public int LifeTimeTicks;

        public bool IsInitialized = false;

        public bool HasExpired = false;

        public Vector3D PreviousPosition;
        public Vector3D Start;
        public Vector3D End;

        public float LastPositionFraction = 0;

        public int CollisionCheckFrames { get; private set; } = -1;
        public int CollisionCheckCounter = 0;
        public bool DoShortRaycast = false;

        /// <summary>
        /// Initializes all empty variables
        /// </summary>
        public void Init()
        {
            if (HasBulletDrop)
            {
                FlightEffect = new BulletDrop();
            }
            else
            {
                FlightEffect = new BasicFlight();
            }

            IsInitialized = true;
        }

        /// <summary>
        /// Updates LifetimeTicks that keeps track of distance traveled
        /// </summary>
        public void PreUpdate()
        {
            PreviousPosition = PositionMatrix.Translation;
            LifeTimeTicks++;
        }

        public virtual void Update()
        {
            FlightEffect.Update(this);
        }

        /// <summary>
        /// Draws the projectile
        /// </summary>
        public void Draw()
        {
            if (MyAPIGateway.Utilities.IsDedicated) return;

            float thickness = ProjectileTrailScale * 0.2f;
            float length = 20f * ProjectileTrailScale;

            MyTransparentGeometry.AddLineBillboard(
                    string.IsNullOrWhiteSpace(Material) ? DefaultProjectileTrail : MyStringId.GetOrCompute(Material),
                    new Vector4(ProjectileTrailColor * 10f, 1f),
                    PositionMatrix.Translation + (PositionMatrix.Forward * length),
                    -PositionMatrix.Forward,
                    length,
                    thickness);
        }

        /// <summary>
        /// Define collision start and end points and other precalculation operations
        /// </summary>
        public void PreCollitionDetection()
        {
            Start = PositionMatrix.Translation;
            if (DoShortRaycast)
            {
                End = PositionMatrix.Translation + VelocityPerTick;
                DoShortRaycast = false;
            }
            else
            {
                End = PositionMatrix.Translation + (VelocityPerTick * CollisionCheckFrames);
            }
        }

        /// <summary>
        /// Checks for collisions
        /// </summary>
        public void CollisionDetection()
        {
            IHitInfo hit = null;
            List<IHitInfo> hitlist = new List<IHitInfo>();
            if (UseLongRaycast)
            {
                MyAPIGateway.Physics.CastLongRay(Start, End, out hit, true);
            }
            else
            {

                MyAPIGateway.Physics.CastRay(Start, End, hitlist);

                if (hitlist.Count > 0)
                {
                    hit = hitlist[0];
                }
            }

            if (hit != null && hit.Position != null)
            {
                int framesToWait = (int)Math.Floor(hit.Fraction * (float)CollisionCheckFrames);
                if (framesToWait < 1)
                {
                    if (Penetration != null)
                    {
                        Penetration.Execute(hit, hitlist, this);
                    }
                    else if (Ricochet != null)
                    {
                        Ricochet.Execute(hit, null, this);
                    }
                    else if (Explosive != null)
                    {
                        Explosive.Execute(hit, null, this);
                    }
                    else
                    {
                        if (!MyAPIGateway.Session.IsServer) return;

                        if (hit.HitEntity is IMyDestroyableObject)
                        {
                            IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;
                            (hit.HitEntity as IMyDestroyableObject).DoDamage(ProjectileHealthDamage, MyStringHash.GetOrCompute(AmmoSubtypeId), true, default(MyHitInfo), BlockId);

                            hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, PositionMatrix.Forward * ProjectileHitImpulse, hit.Position, null);

                            LastPositionFraction = hit.Fraction;
                        }
                        else if (hit.HitEntity is IMyCubeGrid)
                        {
                            IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;

                            Vector3D direction = PositionMatrix.Forward;
                            Vector3I? hitPos = grid.RayCastBlocks(hit.Position, hit.Position + direction);
                            if (hitPos.HasValue)
                            {
                                IMySlimBlock block = grid.GetCubeBlock(hitPos.Value);
                                if (IgnoreDamageReduction)
                                {
                                    float mult = Tools.GetScalerInverse(((MyCubeBlockDefinition)block.BlockDefinition).GeneralDamageMultiplier);

                                    block.DoDamage(ProjectileMassDamage * mult, MyStringHash.GetOrCompute(AmmoSubtypeId), true, default(MyHitInfo), BlockId);
                                }
                                else
                                {
                                    block.DoDamage(ProjectileMassDamage, MyStringHash.GetOrCompute(AmmoSubtypeId), true, default(MyHitInfo), BlockId);
                                }

                                block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, PositionMatrix.Forward * ProjectileHitImpulse, hit.Position, null);

                                LastPositionFraction = hit.Fraction;
                            }
                        }

                        HasExpired = true;
                    }
                }
                else
                {
                    CollisionCheckCounter = CollisionCheckWaitFrames() - framesToWait;
                    DoShortRaycast = true;
                }
            }
        }

        public bool DoCollisionCheck()
        {
            if (HasExpired)
            {
                return false;
            }

            CollisionCheckCounter++;
            if (CollisionCheckCounter != CollisionCheckWaitFrames())
            {
                return false;
            }
            else
            {
                CollisionCheckCounter = 0;
                return true;
            }
        }

        public int CollisionCheckWaitFrames()
        {
            if (CollisionCheckFrames == -1)
            {
                if (MaxSpeedLimit == 0)
                {
                    CollisionCheckFrames = 1;
                }
                else
                {
                    CollisionCheckFrames = 1 + (int)Math.Ceiling((DesiredSpeed / MaxSpeedLimit) * 0.5f);
                }
            }

            CollisionCheckFrames = 1;
            return CollisionCheckFrames;
        }

        public void ResetCollisionCheck()
        {
            CollisionCheckFrames = -1;
        }
    }
}
