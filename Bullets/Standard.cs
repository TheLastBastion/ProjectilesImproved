using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Bullets
{
    public class Standard : BulletsBase
    {
        public override void Update()
        {
            LifeTimeTicks++;
            MyProjectileAmmoDefinition ammo = Ammo as MyProjectileAmmoDefinition;

            float scaleFactor = MyParticlesManager.Paused ? 1f : MyUtils.GetRandomFloat(1f, 2f);
            float lengthMultiplier = 40f * ammo.ProjectileTrailScale;
            float thickness = (MyParticlesManager.Paused ? 0.2f : MyUtils.GetRandomFloat(0.2f, 0.3f)) * (ammo.ProjectileTrailScale + 0.5f);
            thickness *= MathHelper.Lerp(0.2f, 0.8f, 1f);

            if (LastPositionFraction == 0)
            {
                MyTransparentGeometry.AddLineBillboard(
                    BulletMaterial,
                    new Vector4(ammo.ProjectileTrailColor * scaleFactor * 10f, 1f),
                    Position,
                    Direction,
                    lengthMultiplier,
                    thickness);
            }
            else
            {
                MyTransparentGeometry.AddLineBillboard(
                    BulletMaterial,
                    new Vector4(ammo.ProjectileTrailColor * scaleFactor * 10f, 1f),
                    Position + (Velocity * Tick) * LastPositionFraction,
                    Direction,
                    lengthMultiplier,
                    thickness);

                HasExpired = true;
            }

            Vector3D velocityPerTick = Velocity * Tick;
            Vector3D positionBeforeUpdate = new Vector3D(Position /*+ ((Distance > velocityPerTick.LengthSquared()) ? -velocityPerTick : Vector3D.Zero)*/);
            Position += velocityPerTick;
            Distance += velocityPerTick.LengthSquared();
            Vector3D toEnd = Position + velocityPerTick + (Direction * lengthMultiplier);

            //Vector4 color = new Vector4(ammo.ProjectileTrailColor * 10f, 1f);
            //MySimpleObjectDraw.DrawLine(positionBeforeUpdate, toEnd, BulletMaterial, ref color, 1.5f);

            //MyAPIGateway.Utilities.ShowNotification($"distance: {(Distance * LifeTimeTicks).ToString("n0")} Max: {(ammo.MaxTrajectory).ToString("n0")}", 1);

            if (Distance * LifeTimeTicks > ammo.MaxTrajectory * ammo.MaxTrajectory)
            {
                HasExpired = true;
            }

            List<IHitInfo> hitlist = new List<IHitInfo>();
            MyAPIGateway.Physics.CastRay(positionBeforeUpdate, toEnd, hitlist);
            foreach (IHitInfo hit in hitlist)
            {
                if (hit.HitEntity is IMyDestroyableObject)
                {
                    IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;
                    (hit.HitEntity as IMyDestroyableObject).DoDamage(ammo.ProjectileHealthDamage, Bullet, true, default(MyHitInfo), ShooterID);

                    hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, Direction * ammo.ProjectileHitImpulse, hit.Position, null);

                    LastPositionFraction = hit.Fraction;
                    break;
                }
                else if (hit.HitEntity is IMyCubeGrid)
                {
                    IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;
                    Vector3I? hitPos = grid.RayCastBlocks(positionBeforeUpdate, toEnd);
                    if (hitPos.HasValue)
                    {
                        IMySlimBlock block = grid.GetCubeBlock(hitPos.Value);
                        block.DoDamage(ammo.ProjectileMassDamage, Bullet, true, default(MyHitInfo), ShooterID);

                        block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, Direction * ammo.ProjectileHitImpulse, hit.Position, null);

                        LastPositionFraction = hit.Fraction;
                        break;
                    }
                }
            }
        }
    }
}
