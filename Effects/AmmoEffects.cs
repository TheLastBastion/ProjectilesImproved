using ProjectilesImproved.Bullets;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRageMath;

namespace ProjectilesImproved.Effects
{
    [ProtoContract]
    public class AmmoEffects : IEffect
    {
        [ProtoMember(1)]
        public string AmmoId { get; set; }

        [ProtoMember(2)]
        public bool HasBulletDrop { get; set; }

        [ProtoMember(3)]
        public float BulletDropMultiplyer { get; set; }

        [ProtoMember(4)]
        public Ricochet Ricochet { get; set; }

        [ProtoMember(5)]
        public Explosive Explosive { get; set; }


        public void Execute(IHitInfo hit, BulletBase bullet)
        {

            if (Ricochet != null)
            {
                Ricochet.Execute(hit, bullet);
            }
            else if (Explosive != null)
            {
                Explosive.Execute(hit, bullet);
            }
            else
            {
                if (!MyAPIGateway.Session.IsServer) return;

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
                    Vector3I? hitPos = grid.RayCastBlocks(hit.Position, hit.Position + (bullet.PositionMatrix.Forward * 0.5));
                    if (hitPos.HasValue)
                    {
                        IMySlimBlock block = grid.GetCubeBlock(hitPos.Value);
                        block.DoDamage(bullet.ProjectileMassDamage, bullet.AmmoId.SubtypeId, true, default(MyHitInfo), bullet.BlockId);

                        block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, bullet.PositionMatrix.Forward * bullet.ProjectileHitImpulse, hit.Position, null);

                        bullet.LastPositionFraction = hit.Fraction;
                    }
                }

                bullet.HasExpired = true;
            }
        }
    }
}
