using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using ModNetworkAPI;
using ProjectilesImproved.Bullets;

namespace ProjectilesImproved
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation | MyUpdateOrder.BeforeSimulation)]
    public class Core : MySessionComponentBase
    {
        public static bool IsInitialized = false;
        public static event Action OnLoadComplete;
        public static List<BulletBase> ActiveProjectiles = new List<BulletBase>();

        public const string ModName = "Projectiles Improved";
        public const ushort ModID = 4112;

        private NetworkAPI Network => NetworkAPI.Instance;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (!NetworkAPI.IsInitialized)
            {
                NetworkAPI.Init(ModID, ModName);
            }

            MyAPIGateway.Session.OnSessionReady += OnStartInit;
        }

        private void OnStartInit()
        {
            MyAPIGateway.Session.OnSessionReady -= OnStartInit;

            IsInitialized = true;
            OnLoadComplete?.Invoke();
        }

        public static void SpawnProjectile(BulletBase data)
        {
            ActiveProjectiles.Add(data);
        }

        public override void UpdateBeforeSimulation()
        {
            MyAPIGateway.Utilities.ShowNotification($"Total Projectiles: {ActiveProjectiles.Count}", 1);

            for (int i = 0; i < ActiveProjectiles.Count; i++)
            {
                BulletBase bullet = ActiveProjectiles[i];

                if (bullet.HasExpired)
                {
                    ActiveProjectiles.RemoveAt(i);
                    i--;
                    continue;
                }

                if (!bullet.IsInitialized)
                {
                    bullet.Init();
                }

                bullet.PreUpdate();

                if (bullet.DoCollisionCheck())
                {
                    bullet.PreCollitionDetection();
                    bullet.CollisionDetection();
                }

                bullet.Draw();
                bullet.Update();
            }
        }
    }
}
