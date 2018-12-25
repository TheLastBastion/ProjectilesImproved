using ProjectilesImproved.Projectiles;

namespace ProjectilesImproved.Effects.Flight
{
    public class BasicFlight : IFlight
    {
        public void Update(Projectile bullet)
        {
            bullet.Position += bullet.VelocityPerTick;
            bullet.DistanceTraveled += bullet.VelocityPerTick.LengthSquared();

            if (bullet.IsAtRange)
            {
                bullet.HasExpired = true;
            }
        }
    }
}