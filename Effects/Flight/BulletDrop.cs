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
            Vector3D gravity = WorldPlanets.GetExternalForces(bullet.PositionMatrix.Translation).Gravity * bullet.BulletDropGravityScaler;

            bullet.Velocity += gravity * Tools.Tick;
            bullet.PositionMatrix.Translation += bullet.Velocity * Tools.Tick;

            bullet.PositionMatrix.Forward = Vector3D.Normalize(bullet.Velocity - bullet.InitialGridVelocity);
            bullet.DistanceTraveled += bullet.VelocityPerTick.LengthSquared();

            if (bullet.IsAtRange)
            {
                bullet.HasExpired = true;
            }
        }
    }
}