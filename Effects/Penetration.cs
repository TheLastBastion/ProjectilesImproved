using ProjectilesImproved.Bullets;
using ProtoBuf;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    [ProtoContract]
    public class Penetration : IEffect
    {
        [ProtoMember]
        public float VelocityDecreasePerHp { get; set; }

        public void Execute(IHitInfo hit, List<IHitInfo> hitlist, BulletBase bullet)
        {
            for (int i = 0; i < hitlist.Count; i++)
            {
                hit = hitlist[i];

                int framesToWait = (int)Math.Floor(hit.Fraction * (float)bullet.CollisionCheckFrames);
                if (framesToWait < 1)
                {

                    if (hit.HitEntity is IMyDestroyableObject)
                    {
                        IMyDestroyableObject obj = hit.HitEntity as IMyDestroyableObject;
                        (hit.HitEntity as IMyDestroyableObject).DoDamage(bullet.ProjectileHealthDamage, bullet.AmmoId.SubtypeId, true, default(MyHitInfo), bullet.BlockId);

                        hit.HitEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.PositionMatrix.Forward * bullet.ProjectileHitImpulse, hit.Position, null);

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

                                if (bullet.ProjectileMassDamage > block.Integrity)
                                {
                                    damage = block.Integrity;
                                    bullet.ProjectileMassDamage -= block.Integrity;
                                }
                                else
                                {
                                    damage = bullet.ProjectileMassDamage;
                                    bullet.ProjectileMassDamage = 0;
                                    bullet.HasExpired = true;
                                }

                                block.DoDamage(damage, bullet.AmmoId.SubtypeId, true, default(MyHitInfo), bullet.BlockId);

                                block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.PositionMatrix.Forward * bullet.ProjectileHitImpulse, hit.Position, null);

                                bullet.LastPositionFraction = hit.Fraction;
                            }
                        }
                    }
                }
            }
        }
    }
}
