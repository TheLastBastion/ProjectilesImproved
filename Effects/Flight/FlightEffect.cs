using System.Collections.Generic;
using ProjectilesImproved.Effects;
using ProjectilesImproved.Projectiles;
using VRage.Game.ModAPI;

namespace ProjectilesImproved.Effects.Flight
{
    public class FlightEffect : IFlight
    {
        public void Update(Bullet bullet)
        {
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