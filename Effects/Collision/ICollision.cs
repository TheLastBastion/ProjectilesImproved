using ProjectilesImproved.Projectiles;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace ProjectilesImproved.Effects.Collision
{
    public interface ICollision
    {
        /// <summary>
        /// Applies some effect when bullet hits
        /// </summary>
        /// <param name="hit">The raycast information</param>
        /// <param name="bullet">The bullet that did the hitting</param>
        void Execute(IHitInfo hit, List<IHitInfo> hitlist, Bullet bullet);
    }
}
