using ProjectilesImproved.Projectiles;
using ProtoBuf;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Effects.Collision
{
    [ProtoContract]
    public class Penetration : ICollision
    {
        private float velocityDecreasePerHp;
        [ProtoMember]
        public float VelocityDecreasePerHp
        {
            get { return velocityDecreasePerHp; }
            set { velocityDecreasePerHp = (value >= 0) ? value : 0;  }
        }

        public void Execute(IHitInfo hit, List<IHitInfo> hitlist, Projectile bullet)
        {
            for (int i = 0; i < hitlist.Count; i++)
            {
                hit = hitlist[i];

                int framesToWait = (int)Math.Floor(hit.Fraction * (float)bullet.CollisionCheckFrames);
                if (framesToWait > 1)                
                {
                    bullet.CollisionCheckCounter = bullet.CollisionCheckWaitFrames() - framesToWait;
                    bullet.DoShortRaycast = true;
                }
                else
                {
                    if (hit.HitEntity is IMyDestroyableObject)
                    {
                        IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;
                        (hit.HitEntity as IMyDestroyableObject).DoDamage(bullet.ProjectileHealthDamage, MyStringHash.GetOrCompute(bullet.SubtypeId), true, default(MyHitInfo), bullet.ParentBlockId);

                        hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.Direction * bullet.ProjectileHitImpulse, hit.Position, null);

                        bullet.LastPositionFraction = hit.Fraction;
                    }
                    else if (hit.HitEntity is IMyCubeGrid)
                    {
                        IMyCubeGrid grid = hit.HitEntity as IMyCubeGrid;

                        List<Vector3I> possibleBlocks = new List<Vector3I>();
                        grid.RayCastCells(hit.Position, hit.Position + bullet.VelocityPerTick, possibleBlocks);

                        foreach (Vector3I possible in possibleBlocks)
                        {
                            IMySlimBlock block = grid.GetCubeBlock(possible);
                            if (block != null)
                            {
                                float damage;

                                float mult = Tools.GetScalerInverse(((MyCubeBlockDefinition)block.BlockDefinition).GeneralDamageMultiplier);
                                float trueIntegrity = block.Integrity * mult;
                                if (bullet.IgnoreDamageReduction)
                                {
                                    if (bullet.ProjectileMassDamage > block.Integrity)
                                    {
                                        damage = trueIntegrity;
                                        bullet.ProjectileMassDamage -= block.Integrity;
                                    }
                                    else
                                    {
                                        damage = bullet.ProjectileMassDamage * mult;
                                        bullet.ProjectileMassDamage = 0;
                                        bullet.HasExpired = true;
                                    }
                                }
                                else
                                {
                                    if (bullet.ProjectileMassDamage > trueIntegrity)
                                    {
                                        damage = trueIntegrity;
                                        bullet.ProjectileMassDamage -= trueIntegrity;
                                    }
                                    else
                                    {
                                        damage = bullet.ProjectileMassDamage;
                                        bullet.ProjectileMassDamage = 0;
                                        bullet.HasExpired = true;
                                    }
                                }

                                block.DoDamage(damage, MyStringHash.GetOrCompute(bullet.SubtypeId), true, default(MyHitInfo), bullet.ParentBlockId);

                                block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.Direction * bullet.ProjectileHitImpulse, hit.Position, null);

                                bullet.LastPositionFraction = hit.Fraction;
                            }
                        }
                    }
                }
            }
        }

        public Penetration Clone()
        {
            return new Penetration
            {
                VelocityDecreasePerHp = VelocityDecreasePerHp
            };
        }

        public void Update(Projectile bullet)
        {
            return;
        }
    }
}
