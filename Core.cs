using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using ModNetworkAPI;
using ProjectilesImproved.Projectiles;
using VRage.Game.ModAPI;
using ProjectilesImproved.Definitions;
using VRage.Utils;
using ProjectilesImproved.Weapons;
using System.Collections.Concurrent;
using VRage.ModAPI;

namespace ProjectilesImproved
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation | MyUpdateOrder.BeforeSimulation)]
    public class Core : MySessionComponentBase
    {
        public static ConcurrentQueue<DamageDefinition> DamageRequests = new ConcurrentQueue<DamageDefinition>();

        private static HashSet<Projectile> PendingProjectiles = new HashSet<Projectile>();
        private static HashSet<Projectile> ActiveProjectiles = new HashSet<Projectile>();
        private static HashSet<Projectile> ExpiredProjectiles = new HashSet<Projectile>();

        public const string ModName = "Weapons Overhaul";
        public const ushort ModID = 4112;

        public static event Action OnSettingsUpdate;

        private static bool ServerInitalized = false;
        private static bool ClientInitialized = false;

        public static bool IsInitialized()
        {
            if (MyAPIGateway.Session == null)
            {
                return false;
            }

            if (MyAPIGateway.Session.IsServer)
            {
                return ServerInitalized;
            }

            return ClientInitialized;
        }

        private NetworkAPI Network => NetworkAPI.Instance;
        private int waitInterval = 0;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (NetworkAPI.IsInitialized) return;
            NetworkAPI.Init(ModID, ModName, "/weapons");

            if (Network.NetworkType == NetworkTypes.Client)
            {
                Network.RegisterNetworkCommand(null, ClientCallback_Update);
                Network.RegisterNetworkCommand("sync", ClientCallback_WeaponSync);
                Network.RegisterChatCommand("update", (args) => { Network.SendCommand("update"); });
                Network.RegisterChatCommand("load", (args) => { Network.SendCommand("load"); });
                Network.RegisterChatCommand("save", (args) => { Network.SendCommand("save"); });
            }
            else
            {
                Network.RegisterNetworkCommand("sync", ServerCallback_WeaponSync);
                Network.RegisterNetworkCommand("update", ServerCallback_Update);
                Network.RegisterNetworkCommand("load", ServerCallback_Load);
                Network.RegisterNetworkCommand("save", ServerCallback_Save);

                if (Network.NetworkType != NetworkTypes.Dedicated)
                {
                    Network.RegisterChatCommand("load", (args) =>
                    {
                        Settings.Load();
                        OnSettingsUpdate?.Invoke();
                        Network.SendCommand(null, "New weapon settings loaded", MyAPIGateway.Utilities.SerializeToBinary(Settings.GetCurrentSettings()));
                        MyAPIGateway.Utilities.ShowMessage(ModName, "Loading from file");
                    });

                    Network.RegisterChatCommand("save", (args) =>
                    {
                        Settings.Save();
                        MyAPIGateway.Utilities.ShowMessage(ModName, "Settings saved");
                    });
                }
            }
        }

        public override void BeforeStart()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Settings.Init();
                OnSettingsUpdate?.Invoke();
                ServerInitalized = true;
            }
        }

        protected override void UnloadData()
        {
            Network.Close();
        }

        public static void SpawnProjectile(Projectile data)
        {
            lock (PendingProjectiles)
            {
                PendingProjectiles.Add(data);
            }
        }

        public override void UpdateBeforeSimulation()
        {
            /*
            * this is a dumb hack to fix crashing when clients connect.
            * the session ready event sometimes does not have everything loaded when i trigger the send command
            */
            if (!MyAPIGateway.Session.IsServer && !IsInitialized())
            {
                if (waitInterval == 60)
                {
                    //MyAPIGateway.Utilities.ShowNotification("Sending update Request", 1000);
                    Network.SendCommand("update");
                }

                waitInterval++;
            }


            //MyAPIGateway.Utilities.ShowNotification($"Total Projectiles: {ActiveProjectiles.Count}, Pending: {PendingProjectiles.Count}, Expired: {ExpiredProjectiles.Count}", 1);


            ActiveProjectiles.ExceptWith(ExpiredProjectiles);
            ExpiredProjectiles.Clear();

            ActiveProjectiles.UnionWith(PendingProjectiles);
            PendingProjectiles.Clear();

            MyAPIGateway.Parallel.ForEach(ActiveProjectiles, (Projectile p) =>
            {
                if (!p.IsInitialized)
                {
                    p.Init();
                }

                p.PreUpdate();

                if (p.DoCollisionCheck())
                {
                    p.PreCollitionDetection();
                    p.CollisionDetection();
                }

                p.Update();

                if (p.HasExpired)
                {
                    lock (ExpiredProjectiles)
                    {
                        ExpiredProjectiles.Add(p);
                    }
                }
            });

            // VVV for testing performance VVV

            //foreach (Projectile p in ActiveProjectiles)
            //{

            //    if (!p.IsInitialized)
            //    {
            //        p.Init();
            //    }

            //    p.PreUpdate();

            //    if (p.DoCollisionCheck())
            //    {
            //        p.PreCollitionDetection();
            //        p.CollisionDetection();
            //    }

            //    p.Draw();
            //    p.Update();

            //    if (p.HasExpired)
            //    {
            //        ExpiredProjectiles.Add(p);
            //    }
            //}

            //MyAPIGateway.Utilities.ShowNotification($"Damage Requests: {DamageRequests.Count}", 1);

            DamageDefinition def;
            while (DamageRequests.Count > 0)
            {
                DamageRequests.TryDequeue(out def);

                if (def.Victim != null)
                {
                    def.Victim.DoDamage(def.Damage, def.DamageType, def.Sync, def.Hit, def.AttackerId);

                    if (def.ImpulseEntity?.Physics != null && def.ImpulseForce != null && def.ImpulsePosition != null)
                    {
                        def.ImpulseEntity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, def.ImpulseForce, def.ImpulsePosition, null);
                    }

                }
            }
        }

        public override void Draw()
        {
            foreach (Projectile p in ActiveProjectiles)
            {
                if (!p.HasExpired)
                {
                    p.Draw();
                }
            }
        }

        private void ClientCallback_WeaponSync(ulong steamId, string CommandString, byte[] data)
        {
            WeaponSync sync = MyAPIGateway.Utilities.SerializeFromBinary<WeaponSync>(data);

            if (sync != null)
            {
                WeaponBasic.SyncWeapon(sync);
            }
            else
            {
                MyLog.Default.Warning("data did not unpack!");
            }
        }

        private void ServerCallback_WeaponSync(ulong steamId, string CommandString, byte[] data)
        {
            WeaponSync sync = MyAPIGateway.Utilities.SerializeFromBinary<WeaponSync>(data);

            if (sync != null)
            {
                WeaponBasic.SyncWeapon(sync);
            }
            else
            {
                MyLog.Default.Warning("data did not unpack!");
            }
        }

        private void ClientCallback_Update(ulong steamId, string CommandString, byte[] data)
        {
            if (data != null)
            {
                Settings s = MyAPIGateway.Utilities.SerializeFromBinary<Settings>(data);
                Settings.SetNewSettings(s);
                OnSettingsUpdate?.Invoke();
                ClientInitialized = true;
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
                OnSettingsUpdate?.Invoke();
                Network.SendCommand(null, "New weapon settings loaded", MyAPIGateway.Utilities.SerializeToBinary(Settings.GetCurrentSettings()));
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
                Network.SendCommand(null, "Settings Saved", MyAPIGateway.Utilities.SerializeToBinary(Settings.GetCurrentSettings()));
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
