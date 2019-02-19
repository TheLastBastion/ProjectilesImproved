using ProjectilesImproved.Definitions;
using ProjectilesImproved.Effects.Flight;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Projectiles
{
    [ProtoContract]
    public class Projectile : ProjectileDefinition
    {
        [XmlIgnore]
        public static MyStringId DefaultProjectileTrail = MyStringId.GetOrCompute("ProjectileTrailLine");
        [XmlIgnore]
        public Vector3D VelocityPerTick => Velocity * Tools.Tick;
        [XmlIgnore]
        public bool IsAtRange => DistanceTraveled * LifeTimeTicks > MaxTrajectory * MaxTrajectory;
        [XmlIgnore]
        public bool UseLongRaycast => DesiredSpeed * Tools.Tick * CollisionCheckFrames > 50;
        [XmlIgnore]
        public IMySlimBlock PartentSlim;

        [ProtoMember]
        public long ParentBlockId;
        [ProtoMember]
        public Vector3D Position;
        [ProtoMember]
        public Vector3D Velocity;
        [XmlIgnore]
        public Vector3D Direction;
        [ProtoMember]
        public Vector3D InitialGridVelocity;
        [XmlIgnore]
        public double DistanceTraveled;
        [XmlIgnore]
        public IFlight FlightEffect;
        [XmlIgnore]
        public int LifeTimeTicks;
        [XmlIgnore]
        public bool IsInitialized = false;

        [XmlIgnore]
        public bool HasExpired;
        [XmlIgnore]
        public float LastPositionFraction = 0;

        private Vector3D PreviousPosition;
        private Vector3D Start;
        private Vector3D End;

        [XmlIgnore]
        public int CollisionCheckFrames = -1;
        [XmlIgnore]
        public int CollisionCheckCounter = 0;
        [XmlIgnore]
        public bool DoShortRaycast = false;

        /// <summary>
        /// Initializes all empty variables
        /// </summary>
        public void Init()
        {
            Direction = Vector3D.Normalize(Velocity);

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
            PreviousPosition = Position;
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
                    Position + (Direction * length),
                    -Direction,
                    length,
                    thickness);
        }

        /// <summary>
        /// Define collision start and end points and other precalculation operations
        /// </summary>
        public void PreCollitionDetection()
        {
            Start = Position;
            if (DoShortRaycast)
            {
                End = Position + VelocityPerTick;
                DoShortRaycast = false;
            }
            else
            {
                End = Position + (VelocityPerTick * CollisionCheckFrames);
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
                MyAPIGateway.Physics.CastLongRay(Start, End, out hit, false);
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
                        HasExpired = true;
                        if (!MyAPIGateway.Session.IsServer) return;

                        if (hit.HitEntity is IMyDestroyableObject)
                        {
                            IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;
                            (hit.HitEntity as IMyDestroyableObject).DoDamage(ProjectileHealthDamage, MyStringHash.GetOrCompute(SubtypeId), true, default(MyHitInfo), ParentBlockId);

                            hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, Direction * ProjectileHitImpulse, hit.Position, null);

                            LastPositionFraction = hit.Fraction;
                        }
                        else if (hit.HitEntity is IMyCubeGrid)
                        {
                            IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;

                            Vector3D direction = Direction;
                            Vector3I? hitPos = grid.RayCastBlocks(hit.Position, hit.Position + direction);
                            if (hitPos.HasValue)
                            {
                                IMySlimBlock block = grid.GetCubeBlock(hitPos.Value);
                                if (IgnoreDamageReduction)
                                {
                                    float mult = Tools.GetScalerInverse(((MyCubeBlockDefinition)block.BlockDefinition).GeneralDamageMultiplier);

                                    block.DoDamage(ProjectileMassDamage * mult, MyStringHash.GetOrCompute(SubtypeId), true, default(MyHitInfo), ParentBlockId);
                                }
                                else
                                {
                                    block.DoDamage(ProjectileMassDamage, MyStringHash.GetOrCompute(SubtypeId), true, default(MyHitInfo), ParentBlockId);
                                }

                                block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, Direction * ProjectileHitImpulse, hit.Position, null);

                                LastPositionFraction = hit.Fraction;
                            }
                        }
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
                if (Tools.MaxSpeedLimit == 0)
                {
                    CollisionCheckFrames = 1;
                }
                else
                {
                    CollisionCheckFrames = 1 + (int)Math.Ceiling((DesiredSpeed / Tools.MaxSpeedLimit) * 0.5f);
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
