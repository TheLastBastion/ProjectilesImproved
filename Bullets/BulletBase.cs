using ProjectilesImproved.Effects;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Bullets
{
    [ProtoContract]
    public class BulletBase
    {
        public static MyStringHash Bullet = MyStringHash.GetOrCompute("bullet");
        public static float MaxSpeedLimit => (MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed > MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed) ?
            MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed : MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;

        public const float Tick = 1f / 60f;
        public Vector3D VelocityPerTick => Velocity * Tick;
        public bool IsAtRange => DistanceTraveled * LifeTimeTicks > MaxTrajectory * MaxTrajectory;
        public bool UseLongRaycast => ProjectileSpeed * Tick * CollisionCheckFrames > 50;

        [ProtoMember(1)]
        public long GridId;

        [ProtoMember(2)]
        public long BlockId;

        [ProtoMember(3)]
        public MyDefinitionId WeaponId;

        [ProtoMember(4)]
        public MyDefinitionId MagazineId;

        [ProtoMember(5)]
        public MyDefinitionId AmmoId;

        [ProtoMember(6)]
        public MatrixD PositionMatrix;

        [ProtoMember(7)]
        public double DistanceTraveled;

        [ProtoMember(8)]
        public Vector3D Velocity;

        [ProtoMember(9)]
        public Vector3D InitialGridVelocity;

        [ProtoMember(10)]
        public int LifeTimeTicks;

        [ProtoMember(11)]
        public float ProjectileMassDamage = -1;

        [ProtoMember(12)]
        public float ProjectileHealthDamage = -1;

        [ProtoMember(13)]
        public float ProjectileSpeed = -1;

        [ProtoMember(14)]
        public float ProjectileHitImpulse = -1;

        [ProtoMember(15)]
        public float ProjectileTrailScale = -1;

        [ProtoMember(16)]
        public float MaxTrajectory = -1;

        [ProtoMember(17)]
        public Vector3 ProjectileTrailColor = Vector3.Zero;

        [ProtoMember(18)]
        public MyStringId BulletMaterial = MyStringId.GetOrCompute("ProjectileTrailLine");

        [ProtoMember(19)]
        public AmmoEffect Effects { get; set; }

        public bool IsInitialized = false;

        public bool HasExpired = false;

        public IMySlimBlock Slim;

        public MyWeaponDefinition Weapon;

        public MyAmmoMagazineDefinition Magazine;

        public Vector3D PreviousPosition;
        public Vector3D Start;
        public Vector3D End;

        public float LastPositionFraction = 0;

        public int CollisionCheckFrames { get; private set; } = -1;
        public int CollisionCheckCounter = 0;
        public bool DoShortRaycast = false;
        //private float VelocityPerTickLength = 0;

        /// <summary>
        /// Initializes all empty variables
        /// </summary>
        public void Init()
        {
            if (Weapon == null)
            {
                Weapon = MyDefinitionManager.Static.GetWeaponDefinition(WeaponId);
            }

            if (Magazine == null)
            {
                Magazine = MyDefinitionManager.Static.GetAmmoMagazineDefinition(MagazineId);
            }

            MyProjectileAmmoDefinition Ammo = (MyProjectileAmmoDefinition)MyDefinitionManager.Static.GetAmmoDefinition(AmmoId);

            if (ProjectileMassDamage == -1)
                ProjectileMassDamage = Ammo.ProjectileMassDamage;
            if (ProjectileHealthDamage == -1)
                ProjectileHealthDamage = Ammo.ProjectileHealthDamage;
            if (ProjectileSpeed == -1)
                ProjectileSpeed = Ammo.DesiredSpeed;
            if (ProjectileHitImpulse == -1)
                ProjectileHitImpulse = Ammo.ProjectileHitImpulse;
            if (MaxTrajectory == -1)
                MaxTrajectory = Ammo.MaxTrajectory;

            if (Ammo.ProjectileTrailMaterial != null)
            {
                BulletMaterial = MyStringId.GetOrCompute(Ammo.ProjectileTrailMaterial);
            }

            if (ProjectileTrailColor == Vector3.Zero)
                ProjectileTrailColor = Ammo.ProjectileTrailColor;

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
            PositionMatrix.Translation += VelocityPerTick;
            DistanceTraveled += VelocityPerTick.LengthSquared();

            if (IsAtRange)
            {
                HasExpired = true;
            }
        }

        /// <summary>
        /// Draws the projectile
        /// </summary>
        public virtual void Draw()
        {
            if (MyAPIGateway.Utilities.IsDedicated) return;

            float scaleFactor = ProjectileTrailScale * ProjectileTrailScale;
            float thickness = ProjectileTrailScale * 0.2f;
            float length = 10f * ProjectileTrailScale;

            MyTransparentGeometry.AddLineBillboard(
                    BulletMaterial,
                    new Vector4(ProjectileTrailColor * 10f, 1f),
                    PositionMatrix.Translation,
                    -PositionMatrix.Forward,
                    length,
                    thickness);
        }

        /// <summary>
        /// Define collision start and end points and other precalculation operations
        /// </summary>
        public virtual void PreCollitionDetection()
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
        public virtual void CollisionDetection()
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
                    Effects.Execute(hit, hitlist, this);
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
                    CollisionCheckFrames = 1 + (int)Math.Ceiling((ProjectileSpeed / MaxSpeedLimit) * 0.5f);
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
