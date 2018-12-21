using ProjectilesImproved.Projectiles;

namespace ProjectilesImproved.Effects.Flight
{
    public interface IFlight
    {
        /// <summary>
        /// Applies travel effects
        /// </summary>
        /// <param name="bullet">The bullet under effect</param>
        void Update(Projectile bullet);
    }
}
