using ProjectilesImproved.Bullets;
using VRage.Game.ModAPI;

namespace ProjectilesImproved.Effects
{
    interface IEffect
    {
        /// <summary>
        /// Applies some effect when bullet hits
        /// </summary>
        /// <param name="hit">The raycast information</param>
        /// <param name="bullet">The bullet that did the hitting</param>
        void Execute(IHitInfo hit, BulletBase bullet);
    }
}
