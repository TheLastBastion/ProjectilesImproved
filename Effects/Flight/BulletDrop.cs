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
        public void Update(Projectile bullet)
        {
            ExternalForceData forceData = WorldPlanets.GetExternalForces(bullet.PositionMatrix.Translation);

            bullet.Velocity = bullet.Velocity + (forceData.Gravity * bullet.BulletDropGravityScaler);
            bullet.PositionMatrix.Forward = Vector3D.Normalize(bullet.Velocity - bullet.InitialGridVelocity);

            bullet.PositionMatrix.Translation += bullet.VelocityPerTick;
            bullet.DistanceTraveled += bullet.VelocityPerTick.LengthSquared();

            if (bullet.IsAtRange)
            {
                bullet.HasExpired = true;
            }
        }
    }
}