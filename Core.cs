using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using ModNetworkAPI;
using ProjectilesImproved.Bullets;
using ProjectilesImproved.Effects;
using VRage.Game.ModAPI;
using System.Diagnostics;

namespace ProjectilesImproved
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation | MyUpdateOrder.BeforeSimulation)]
    public class Core : MySessionComponentBase
    {
        public static bool IsInitialized = false;
        public bool SentInitialRequest = false;
        private int waitInterval = 0;
        public static event Action OnLoadComplete;
        public static List<BulletBase> ActiveProjectiles = new List<BulletBase>();

        public const string ModName = "Weapons Overhaul";
        public const ushort ModID = 4112;

        private NetworkAPI Network => NetworkAPI.Instance;

        private Settings DefaultSettings = null;

        Stopwatch timer = new Stopwatch();

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (!NetworkAPI.IsInitialized)
            {
                NetworkAPI.Init(ModID, ModName, "/weapons");

                if (Network.NetworkType == NetworkTypes.Client)
                {
                    Network.RegisterNetworkCommand(null, ClientCallback_Update);
                    Network.RegisterChatCommand("update", (args) => { Network.SendCommand("update"); });
                    Network.RegisterChatCommand("load", (args) => { Network.SendCommand("load"); });
                    Network.RegisterChatCommand("save", (args) => { Network.SendCommand("save"); });
                    Network.RegisterChatCommand("reset_default",  (args) => { Network.SendCommand("reset_default"); });
                }
                else
                {
                    Network.RegisterNetworkCommand("update", ServerCallback_Update);
                    Network.RegisterNetworkCommand("load", ServerCallback_Load);
                    Network.RegisterNetworkCommand("save", ServerCallback_Save);
                    Network.RegisterNetworkCommand("reset_default", ServerCallback_Default);

                    if (Network.NetworkType != NetworkTypes.Dedicated)
                    {
                        Network.RegisterChatCommand("load", (args) => 
                        {
                            Settings.Load();
                            MyAPIGateway.Utilities.ShowMessage(ModName, "Loading from file");
                        });

                        Network.RegisterChatCommand("reset_default", (args) =>
                        {
                            Settings.SetNewSettings(DefaultSettings);
                            MyAPIGateway.Utilities.ShowMessage(ModName, "Reset default settings. This has not been saved yet.");
                        });

                        Network.RegisterChatCommand("save", (args) =>
                        {
                            Settings.Save();
                            MyAPIGateway.Utilities.ShowMessage(ModName, "Settings saved");
                        });
                    }
                }
            }

            DefaultSettings = Settings.GetCurrentSettings();
            Settings.Load();
            MyAPIGateway.Session.OnSessionReady += OnStartInit;
        }

        protected override void UnloadData()
        {
            Network.Close();
        }

        private void OnStartInit()
        {
            MyAPIGateway.Session.OnSessionReady -= OnStartInit;

            OnLoadComplete?.Invoke();
            ExplosionShapeGenerator.Initialize();
            IsInitialized = true;
        }

        public static void SpawnProjectile(BulletBase data)
        {
            ActiveProjectiles.Add(data);
        }

        public override void UpdateBeforeSimulation()
        {
            /*
            * this is a dumb hack to fix crashing when clients connect.
            * the session ready event sometimes does not have everything loaded when i trigger the send command
            */
            if (IsInitialized && !SentInitialRequest && Network.NetworkType != NetworkTypes.Client)
            {
                if (waitInterval == 600) // 5 second timer before sending update request
                {
                    Network.SendCommand("update");
                    SentInitialRequest = true;
                    
                }

                waitInterval++;
            }
        }

        public override void UpdateAfterSimulation()
        {
            //MyAPIGateway.Utilities.ShowNotification($"Total Projectiles: {ActiveProjectiles.Count}", 1);
            //long total = AmmoEffect.hits + AmmoEffect.misses;
            //MyAPIGateway.Utilities.ShowNotification($"Default Ammo Hit Success: {(((float)AmmoEffect.hits/(float)((total == 0) ? 1 : total))*100f).ToString("n0")}% Hit: {AmmoEffect.hits}, Missed: {AmmoEffect.misses}", 1);

            timer.Start("GameLoop");

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

            timer.Stop("GameLoop");
            MyAPIGateway.Utilities.ShowNotification($"Loop Time: {timer.Write("GameLoop")}", 1);
        }

        private void ClientCallback_Update(ulong steamId, string CommandString, byte[] data)
        {
            if (data != null)
            {
                Settings.SetNewSettings(MyAPIGateway.Utilities.SerializeFromBinary<Settings>(data));
            }
        }

        private void ServerCallback_Update(ulong steamId, string commandString, byte[] data)
        {
            Network.SendCommand(null, data: MyAPIGateway.Utilities.SerializeToBinary(Settings.GetCurrentSettings()), steamId: steamId);
        }

        private void ServerCallback_Load(ulong steamId, string commandString, byte[] data)
        {
            if (IsAllowedSpecialOperations(steamId))
            {
                Settings.Load();
                Network.SendCommand(null, "Settings loaded", MyAPIGateway.Utilities.SerializeToBinary(Settings.GetCurrentSettings()));
            }
            else
            {
                Network.SendCommand(null, "Load command requires Admin status.", steamId: steamId);
            }
        }

        private void ServerCallback_Save(ulong steamId, string commandString, byte[] data)
        {
            if (IsAllowedSpecialOperations(steamId))
            {
                Settings.Save();
                Network.SendCommand(null, "Settings loaded", MyAPIGateway.Utilities.SerializeToBinary(Settings.GetCurrentSettings()));
            }
            else
            {
                Network.SendCommand(null, "Load command requires Admin status.", steamId: steamId);
            }
        }

        private void ServerCallback_Default(ulong steamId, string commandString, byte[] data)
        {
            if (IsAllowedSpecialOperations(steamId))
            {
                Settings.SetNewSettings(DefaultSettings);
                Network.SendCommand(null, "Settings loaded", MyAPIGateway.Utilities.SerializeToBinary(Settings.GetCurrentSettings()));
            }
            else
            {
                Network.SendCommand(null, "Load command requires Admin status.", steamId: steamId);
            }
        }

        public static bool IsAllowedSpecialOperations(ulong steamId)
        {
            if (MyAPIGateway.Multiplayer.IsServer) return true;
            return IsAllowedSpecialOperations(MyAPIGateway.Session.GetUserPromoteLevel(steamId));
        }

        public static bool IsAllowedSpecialOperations(MyPromoteLevel level)
        {
            return level == MyPromoteLevel.SpaceMaster || level == MyPromoteLevel.Admin || level == MyPromoteLevel.Owner;
        }
    }
}
