using ProjectilesImproved.Effects;
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
        public const float Tick = 1f / 60f;

        public static float MaxSpeedLimit => (MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed > MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed) ? 
            MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed : MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;

        public Vector3D VelocityPerTick => Velocity * Tick;

        [ProtoMember(1)]
        public long ShooterID;

        [ProtoMember(2)]
        public MyDefinitionId WeaponId;

        [ProtoMember(3)]
        public MyDefinitionId MagazineId;

        [ProtoMember(4)]
        public Vector3D Direction;

        [ProtoMember(5)]
        public double DistanceTraveled;

        [ProtoMember(6)]
        public Vector3D Position;

        [ProtoMember(7)]
        public Vector3D Velocity;

        [ProtoMember(8)]
        public Vector3 Up;

        [ProtoMember(9)]
        public int LifeTimeTicks = 0;

        [ProtoMember(10)]
        public EffectBase OnHitEffects;

        public bool HasExpired;

        public IMySlimBlock Slim;

        public MyWeaponDefinition Weapon;

        public MyAmmoMagazineDefinition Magazine;

        public MyProjectileAmmoDefinition Ammo;

        public Vector3D ToEnd;
        public float LengthMultiplyer => 40f * Ammo.ProjectileTrailScale;

        public float LastPositionFraction = 0;


        public int CollisionCheckFrames { get; private set; } = -1;
        private int CollisionCheckCounter = 0;

        public virtual void Update() { }

        public virtual void Draw()
        {
            float lengthMultiplier = 40f * Ammo.ProjectileTrailScale;
            float scaleFactor = MyParticlesManager.Paused ? 1f : MyUtils.GetRandomFloat(1f, 2f);
            float thickness = (MyParticlesManager.Paused ? 0.2f : MyUtils.GetRandomFloat(0.2f, 0.3f)) * (Ammo.ProjectileTrailScale + 0.5f);
            thickness *= MathHelper.Lerp(0.2f, 0.8f, 1f);

            if (LastPositionFraction == 0)
            {
                MyTransparentGeometry.AddLineBillboard(
                    BulletMaterial,
                    new Vector4(Ammo.ProjectileTrailColor * scaleFactor * 10f, 1f),
                    Position,
                    Direction,
                    lengthMultiplier,
                    thickness);
            }
            else
            {
                MyTransparentGeometry.AddLineBillboard(
                    BulletMaterial,
                    new Vector4(Ammo.ProjectileTrailColor * scaleFactor * 10f, 1f),
                    Position + VelocityPerTick * LastPositionFraction,
                    Direction,
                    lengthMultiplier,
                    thickness);

                HasExpired = true;
            }
        }

        public virtual void CollisionDetection()
        {
            if (!DoCollisionCheck() || HasExpired) return;

            ToEnd = Position + (VelocityPerTick * CollisionCheckFrames);// + (Direction * LengthMultiplyer);

            List<IHitInfo> hitlist = new List<IHitInfo>();


            if (Ammo.SpeedVar > 600)
            {
                IHitInfo hit;
                MyAPIGateway.Physics.CastLongRay(Position, ToEnd, out hit, true);
                hitlist.Add(hit);
            }
            else
            {
                MyAPIGateway.Physics.CastRay(Position, ToEnd, hitlist);
            }

            MyVisualScriptLogicProvider.AddGPS("", "", Position, Color.Green);

            if (hitlist.Count > 0)
            {
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

                OnHitEffects.Execute(hitlist[0], this);
                HasExpired = true;
            }
        }

        private bool DoCollisionCheck()
        {
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
            if (this is BulletDrop)
            {
                CollisionCheckFrames = 1;
            }
            else if (CollisionCheckFrames == -1)
            {
                if (MaxSpeedLimit == 0)
                {
                    CollisionCheckFrames = 1;
                }
                else
                {
                    CollisionCheckFrames = (int)Math.Floor((Ammo.SpeedVar / MaxSpeedLimit) * 0.5f);

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
