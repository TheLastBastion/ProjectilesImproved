using System.Collections.Generic;
using ProjectilesImproved.Effects;
using ProjectilesImproved.Projectiles;
using ProtoBuf;
using VRage.Game.ModAPI;
using VRageMath;

namespace ProjectilesImproved.Effects.Flight
{
    [ProtoContract]
    public class BulletDrop : IFlight
    {
        public void Update(Bullet bullet)
        {
            ExternalForceData forceData = WorldPlanets.GetExternalForces(bullet.PositionMatrix.Translation);

            bullet.Velocity = bullet.Velocity + (forceData.Gravity * bullet.CollisionEffect.BulletDropGravityScaler);
            bullet.PositionMatrix.Forward = Vector3D.Normalize(bullet.Velocity - bullet.InitialGridVelocity);

            bullet.PositionMatrix.Translation += bullet.VelocityPerTick;
            bullet.DistanceTraveled += bullet.VelocityPerTick.LengthSquared();

            if (bullet.IsAtRange)
            {
                bullet.HasExpired = true;
            }
        }

        public void Execute(IHitInfo hit, List<IHitInfo> hitlist, Bullet bullet)
        {
            return;
        }
    }
}