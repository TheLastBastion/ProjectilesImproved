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
            Vector3D gravity = WorldPlanets.GetExternalForces(bullet.Position).Gravity * bullet.BulletDropGravityScaler;

            bullet.Velocity += gravity * Tools.Tick;
            bullet.Position += bullet.Velocity * Tools.Tick;

            bullet.Direction = Vector3D.Normalize(bullet.Velocity);
            bullet.DistanceTraveled += bullet.VelocityPerTick.LengthSquared();

            if (bullet.IsAtRange)
            {
                bullet.HasExpired = true;
            }
        }
    }
}