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
        public bool IsAtRange => DistanceTraveled * LifeTimeTicks > Ammo.MaxTrajectory * Ammo.MaxTrajectory;
        public bool UseLongRaycast => Ammo.DesiredSpeed * Tick * CollisionCheckFrames > 50;

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

        public bool IsInitialized = false;

        public bool HasExpired = false;

        public IMySlimBlock Slim;

        public MyWeaponDefinition Weapon;

        public MyAmmoMagazineDefinition Magazine;

        public MyProjectileAmmoDefinition Ammo;

        public EffectBase OnHitEffects;

        public Vector3D Start;
        public Vector3D End;
        public float LengthMultiplyer => 40f * Ammo.ProjectileTrailScale;

        public float LastPositionFraction = 0;

        public int CollisionCheckFrames { get; private set; } = -1;
        private int CollisionCheckCounter = 0;

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

            if (Ammo == null)
            {
                MyLog.Default.Info($"Ammo ID: {AmmoId}");
                MyLog.Default.Flush();
                MyAmmoDefinition def = MyDefinitionManager.Static.GetAmmoDefinition(AmmoId);
                MyLog.Default.Info($"Ammo is null: {def == null}");
                MyLog.Default.Flush();
                Ammo = (MyProjectileAmmoDefinition)MyDefinitionManager.Static.GetAmmoDefinition(AmmoId);
                MyLog.Default.Info($"Ammo after convert is null: {Ammo == null}");
                MyLog.Default.Flush();
            }

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

            MyVisualScriptLogicProvider.AddGPS("", "", PositionMatrix.Translation, Color.Green);
        }

        /// <summary>
        /// Draws the projectile
        /// </summary>
        public virtual void Draw()
        {
            float scaleFactor = MyParticlesManager.Paused ? 1f : MyUtils.GetRandomFloat(1f, 2f);
            float thickness = (MyParticlesManager.Paused ? 0.2f : MyUtils.GetRandomFloat(0.2f, 0.3f)) * (Ammo.ProjectileTrailScale + 0.5f);
            thickness *= MathHelper.Lerp(0.2f, 0.8f, 1f);

            if (LastPositionFraction == 0)
            {
                MyTransparentGeometry.AddLineBillboard(
                    BulletMaterial,
                    new Vector4(Ammo.ProjectileTrailColor * scaleFactor * 10f, 1f),
                    PositionMatrix.Translation,
                    PositionMatrix.Forward,
                    LengthMultiplyer,
                    thickness);
            }
            else
            {
                MyTransparentGeometry.AddLineBillboard(
                    BulletMaterial,
                    new Vector4(Ammo.ProjectileTrailColor * scaleFactor * 10f, 1f),
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
            End = Start + (VelocityPerTick * CollisionCheckFrames);

            //MyVisualScriptLogicProvider.AddGPS("", "", Start + (VelocityPerTick * CollisionCheckFrames * 0.5f), Color.Pink);
            MyVisualScriptLogicProvider.AddGPS("", "", End, Color.Orange);
        }

        /// <summary>
        /// Checks for collisions
        /// </summary>
        public virtual void CollisionDetection()
        {
            List<IHitInfo> hitlist = new List<IHitInfo>();

            if (UseLongRaycast)
            {
                IHitInfo hit;
                MyAPIGateway.Physics.CastLongRay(Start, End, out hit, true);
                hitlist.Add(hit);
            }
            else
            {
                MyAPIGateway.Physics.CastRay(Start, End, hitlist);
            }

            if (hitlist.Count > 0)
            {
                MyLog.Default.Info($"hit: {hitlist.Count}");
                MyLog.Default.Flush();
                MyLog.Default.Info($"hit: {OnHitEffects == null}");
                MyLog.Default.Flush();
                MyVisualScriptLogicProvider.AddGPS("", "", hitlist[0].Position, Color.Red);
                OnHitEffects.Execute(hitlist[0], this);
                HasExpired = true;
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
            //if (this is BulletDrop)
            //{
            //    CollisionCheckFrames = 1;
            //}
            if (CollisionCheckFrames == -1)
            {
                if (MaxSpeedLimit == 0)
                {
                    CollisionCheckFrames = 1;
                }
                else
                {
                    CollisionCheckFrames = (int)Math.Floor((Ammo.DesiredSpeed / MaxSpeedLimit) * 0.5f);
                    MyLog.Default.Info($"CollisionCheckFrames: {CollisionCheckFrames}, Speed: {Ammo.SpeedVar}, DesiredSpeed: {Ammo.DesiredSpeed}, MaxSpeedLimit {MaxSpeedLimit}, Math: {(Ammo.DesiredSpeed / MaxSpeedLimit)}, With Reduction: {(Ammo.DesiredSpeed / MaxSpeedLimit) * 0.5f}");
                    MyLog.Default.Flush();

                    if (CollisionCheckFrames < 1)
                    {
                        CollisionCheckFrames = 1;
                    }
                }
            }

            return CollisionCheckFrames;
        }
    }
}
