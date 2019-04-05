using VRage.Game.Entity;

namespace ProjectilesImproved.Weapons
{
    public interface IWeapon
    {
        bool IsInitialized { get; }

        void Init(MyEntity entity);

        void OnAddedToContainer();

        void OnAddedToScene();

        void OnRemovedFromScene();

        void OnRemovedFromContainer();

        /// <summary>
        /// Updates game logic every frame: RoF, Cooldown...
        /// </summary>
        void Update();

        /// <summary>
        /// Spawns projectiles if available
        /// </summary>
        void Spawn();

        /// <summary>
        /// Updates animations every frame
        /// </summary>
        void Draw();

        void Close();
    }
}
