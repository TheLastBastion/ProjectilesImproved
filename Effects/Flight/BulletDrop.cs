using ProjectilesImproved.Projectiles;
using ProtoBuf;
using VRageMath;

namespace ProjectilesImproved.Effects.Flight
{
    [ProtoContract]
    public class BulletDrop : IFlight
    {
        public void Update(Projectile bullet)
        {
            ExternalForceData forceData = WorldPlanets.GetExternalForces(bullet.PositionMatrix.Translation);

            bullet.Velocity += (forceData.Gravity * bullet.BulletDropGravityScaler) * Tools.Tick;
            bullet.PositionMatrix.Translation += bullet.VelocityPerTick;

            bullet.PositionMatrix.Forward = Vector3D.Normalize(bullet.Velocity - bullet.InitialGridVelocity);
            bullet.DistanceTraveled += bullet.VelocityPerTick.LengthSquared();

            if (bullet.IsAtRange)
            {
                bullet.HasExpired = true;
            }
        }
    }
}