using ProtoBuf;
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
        public int LifeTimeTicks = 0;

        public bool HasExpired;

        public IMySlimBlock Slim;

        public MyWeaponDefinition Weapon;

        public MyAmmoMagazineDefinition Magazine;

        public MyProjectileAmmoDefinition Ammo;

        public Vector3D PositionBeforeUpdate;

        public float LastPositionFraction = 0;


        private int CollisionCheckFrames = -1;
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
                    Position + (Velocity * Tick) * LastPositionFraction,
                    Direction,
                    lengthMultiplier,
                    thickness);

                HasExpired = true;
            }
        }

        public virtual void CollisionDetection()
        {
            if (!DoCollisionCheck()) return;

            float lengthMultiplier = 40f * Ammo.ProjectileTrailScale;
            Vector3D toEnd = Position + VelocityPerTick + (Direction * lengthMultiplier);


            List<IHitInfo> hitlist = new List<IHitInfo>();
            MyAPIGateway.Physics.CastRay(PositionBeforeUpdate, toEnd, hitlist);
            foreach (IHitInfo hit in hitlist)
            {
                if (hit.HitEntity is IMyDestroyableObject)
                {
                    IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;
                    (hit.HitEntity as IMyDestroyableObject).DoDamage(Ammo.ProjectileHealthDamage, Bullet, true, default(MyHitInfo), ShooterID);

                    hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, Direction * Ammo.ProjectileHitImpulse, hit.Position, null);

                    LastPositionFraction = hit.Fraction;
                    break;
                }
                else if (hit.HitEntity is IMyCubeGrid)
                {
                    IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
                    Vector3I? hitPos = grid.RayCastBlocks(PositionBeforeUpdate, toEnd);
                    if (hitPos.HasValue)
                    {
                        IMySlimBlock block = grid.GetCubeBlock(hitPos.Value);
                        block.DoDamage(Ammo.ProjectileMassDamage, Bullet, true, default(MyHitInfo), ShooterID);

                        block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, Direction * Ammo.ProjectileHitImpulse, hit.Position, null);

                        LastPositionFraction = hit.Fraction;
                        break;
                    }
                }
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
            if (CollisionCheckFrames == -1)
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
