﻿using ProjectilesImproved.Effects;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Game;
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
        public static MyStringId BulletMaterial = MyStringId.GetOrCompute("ProjectileTrailLine");
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
        public int LifeTimeTicks;

        [ProtoMember(10)]
        public float ProjectileMassDamage = -1;

        [ProtoMember(13)]
        public float ProjectileHealthDamage = -1;

        [ProtoMember(11)]
        public float ProjectileSpeed = -1;

        [ProtoMember(12)]
        public float ProjectileHitImpulse = -1;

        [ProtoMember(13)]
        public float ProjectileTrailScale = -1;

        [ProtoMember(14)]
        public float MaxTrajectory = -1;

        [ProtoMember(15)]
        public Vector3 ProjectileTrailColor = Vector3.Zero;

        public bool IsInitialized = false;

        public bool HasExpired = false;

        public IMySlimBlock Slim;

        public MyWeaponDefinition Weapon;

        public MyAmmoMagazineDefinition Magazine;

        //public MyProjectileAmmoDefinition Ammo;

        public EffectBase OnHitEffects;

        public Vector3D Start;
        public Vector3D End;
        public float LengthMultiplyer => 40f * ProjectileTrailScale;

        public float LastPositionFraction = 0;

        public int CollisionCheckFrames { get; private set; } = -1;
        private int CollisionCheckCounter = 0;
        private bool DoShortRaycast = false;

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

            if (ProjectileTrailColor == Vector3.Zero)
                ProjectileTrailColor = Ammo.ProjectileTrailColor;

            if (OnHitEffects == null)
            {
                if (Settings.AmmoEffectLookup.ContainsKey(Ammo.Id.SubtypeId))
                {
                    OnHitEffects = Settings.AmmoEffectLookup[Ammo.Id.SubtypeId];
                }
                else
                {
                    OnHitEffects = new EffectBase();
                }
            }

            IsInitialized = true;
        }

        /// <summary>
        /// Updates LifetimeTicks that keeps track of distance traveled
        /// </summary>
        public void PreUpdate()
        {
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

            //MyAPIGateway.Utilities.ShowNotification($"CheckFrames: {CollisionCheckWaitFrames()}, Current: {CollisionCheckCounter}", 1);
            //MyVisualScriptLogicProvider.AddGPS("", "", PositionMatrix.Translation, Color.Green);
        }

        /// <summary>
        /// Draws the projectile
        /// </summary>
        public virtual void Draw()
        {
            float scaleFactor = MyParticlesManager.Paused ? 1f : MyUtils.GetRandomFloat(1f, 2f);
            float thickness = (MyParticlesManager.Paused ? 0.2f : MyUtils.GetRandomFloat(0.2f, 0.3f)) * (ProjectileTrailScale + 0.5f);
            thickness *= MathHelper.Lerp(0.2f, 0.8f, 1f);

            if (LastPositionFraction == 0)
            {
                MyTransparentGeometry.AddLineBillboard(
                    BulletMaterial,
                    new Vector4(ProjectileTrailColor * scaleFactor * 10f, 1f),
                    PositionMatrix.Translation,
                    PositionMatrix.Forward,
                    LengthMultiplyer,
                    thickness);
            }
            else
            {
                MyTransparentGeometry.AddLineBillboard(
                    BulletMaterial,
                    new Vector4(ProjectileTrailColor * scaleFactor * 10f, 1f),
                    PositionMatrix.Translation + VelocityPerTick * LastPositionFraction,
                    PositionMatrix.Forward,
                    LengthMultiplyer,
                    thickness);

                HasExpired = true;
            }
        }

        /// <summary>
        /// Define collision start and end points and other precalculation operations
        /// </summary>
        public virtual void PreCollitionDetection()
        {
            Start = PositionMatrix.Translation;
            if (DoShortRaycast)
            {
                End = Start + VelocityPerTick;
                DoShortRaycast = false;
            }
            else
            {
                End = Start + (VelocityPerTick * CollisionCheckFrames);
            }

            //MyVisualScriptLogicProvider.AddGPS("", "", End, Color.Orange);
        }

        /// <summary>
        /// Checks for collisions
        /// </summary>
        public virtual void CollisionDetection()
        {
            IHitInfo hit = null;

            if (UseLongRaycast)
            {
                MyAPIGateway.Physics.CastLongRay(Start, End, out hit, true);
            }
            else
            {
                List<IHitInfo> hitlist = new List<IHitInfo>();
                MyAPIGateway.Physics.CastRay(Start, End, hitlist);

                if (hitlist.Count > 0)
                {
                    hit = hitlist[0];
                }
            }

            if (hit != null && hit.Position != null)
            {
                int framesToWait = (int)Math.Floor(hit.Fraction * (float)CollisionCheckFrames);
                MyLog.Default.Info($"Fraction: {hit.Fraction}, Frames: {CollisionCheckFrames}, FramesToWait: {framesToWait}, Current Collision Counter: {CollisionCheckCounter}");
                if (framesToWait < 1)
                {
                    OnHitEffects.Execute(hit, this);
                }
                else
                {
                    CollisionCheckCounter = CollisionCheckWaitFrames() - framesToWait;
                    DoShortRaycast = true;
                }

                //MyVisualScriptLogicProvider.AddGPS("", "", hit.Position, Color.Red);
                //OnHitEffects.Execute(hit, this);
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
                    //MyLog.Default.Info($"CollisionCheckFrames: {CollisionCheckFrames}, Speed: {Ammo.SpeedVar}, DesiredSpeed: {Ammo.DesiredSpeed}, MaxSpeedLimit {MaxSpeedLimit}, Math: {(Ammo.DesiredSpeed / MaxSpeedLimit)}, With Reduction: {(Ammo.DesiredSpeed / MaxSpeedLimit) * 0.5f}");
                    //MyLog.Default.Flush();
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
