using ProjectilesImproved.Effects.Collision;
using ProjectilesImproved.Effects.Weapon;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved
{
    [ProtoContract]
    public class Settings
    {
        public const bool DebugMode = false;
        public const bool DebugMode_ShowBlockOctants = false;
        public const bool DebugMode_ShowSphereOctants = false;
        public const bool DebugMode_ShowBlockRayIntersects = false;

        public static readonly Color[] DebugOctantColors = new Color[] { Color.Green, Color.Blue, Color.Orange, Color.Black, Color.HotPink, Color.Red, Color.LightGreen, Color.White };

        public const string Filename = "WeaponsOverhaul.cfg";

        public static CollisionEffect GetAmmoEffect(string id)
        {
            if (AmmoEffectLookup.ContainsKey(id))
            {
                return AmmoEffectLookup[id].Clone();
            }

            return new CollisionEffect();
        }

        public static WeaponEffect GetWeaponEffect(string id)
        {
            if (WeaponEffectLookup.ContainsKey(id))
            {
                return WeaponEffectLookup[id].Clone();
            }

            return new WeaponEffect();

        }

        [ProtoMember]
        public List<WeaponEffect> WeaponEffects { get; set; } = new List<WeaponEffect>();

        [ProtoMember]
        public List<CollisionEffect> AmmoEffects { get; set; } = new List<CollisionEffect>();

        public static Dictionary<string, CollisionEffect> AmmoEffectLookup { get; set; } = new Dictionary<string, CollisionEffect>
        {
            { "MyObjectBuilder_AmmoDefinition/OKI230mmAmmoPars", new CollisionEffect()
                {
                    AmmoId = "MyObjectBuilder_AmmoDefinition/OKI230mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = false,
                    IgnoreDamageReduction = true,
                    Penetration = new Penetration() { VelocityDecreasePerHp = 0 },
                }
            },
            {
                "MyObjectBuilder_AmmoDefinition/OKI76mmAmmoPars", new CollisionEffect()
                {
                    AmmoId = "MyObjectBuilder_AmmoDefinition/OKI76mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = false,
                    Explosive = new Explosive() { Offset = 1f, Angle = 20,  Radius = 7f, Resolution = 1.2f }
                }
            },
            { "MyObjectBuilder_AmmoDefinition/OKI50mmAmmoPars", new CollisionEffect()
                {
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = true,
                    OverKillSpreadScaler = 1,
                    AmmoId = "MyObjectBuilder_AmmoDefinition/OKI50mmAmmoPars",
                }
            },
            { "MyObjectBuilder_AmmoDefinition/OKI23mmAmmoPars", new CollisionEffect()
                {
                    AmmoId = "MyObjectBuilder_AmmoDefinition/OKI23mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    Ricochet = new Ricochet { DeflectionAngle = 20, MaxDamageTransfer = 0.25f, MaxVelocityTransfer = 0.25f, RicochetChance = 0.5f },
                }
            },
            {
               "MyObjectBuilder_AmmoDefinition/LargeCaliber", new CollisionEffect()
                {
                    AmmoId = "MyObjectBuilder_AmmoDefinition/LargeCaliber",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = true,
                    OverKillSpreadScaler = 1,
                    Ricochet = new Ricochet { DeflectionAngle = 45, MaxDamageTransfer = 0.25f, MaxVelocityTransfer = 0.25f, RicochetChance = 0.35f },
                }
            },
            {
               "MyObjectBuilder_AmmoDefinition/SmallCaliber", new CollisionEffect()
                {
                    AmmoId = "MyObjectBuilder_AmmoDefinition/SmallCaliber",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = false,
                    Ricochet = new Ricochet { DeflectionAngle = 90, MaxDamageTransfer = 0.25f, MaxVelocityTransfer = 0.25f, RicochetChance = 0.25f },
                }
            }
        };

        public static Dictionary<string, WeaponEffect> WeaponEffectLookup { get; set; } = new Dictionary<string, WeaponEffect>
        {
            { "MyObjectBuilder_WeaponDefinition/OKI23mmDG", new WeaponEffect()
                {
                    WeaponId = "MyObjectBuilder_WeaponDefinition/OKI23mmDG",
                    Ramping = new Ramping() { StartRPM = 200, MaxRPM = 1000, TimeToMax = 8000 }
                }
            }
        };

        public static void Load()
        {
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInLocalStorage(Filename, typeof(Settings)))
                {
                    MyLog.Default.Info("[WeaponsOverhaul] Loading saved settings");
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Filename, typeof(Settings));
                    string text = reader.ReadToEnd();
                    reader.Close();

                    Settings s = MyAPIGateway.Utilities.SerializeFromXML<Settings>(text);
                    SetNewSettings(s);
                }
                else
                {
                    MyLog.Default.Warning("[WeaponsOverhaul] Config file not found. Loading default settings");
                    Save();

                }
            }
            catch (Exception e)
            {
                MyLog.Default.Warning($"[WeaponsOverhaul] Failed to load saved configuration. Loading defaults\n {e.ToString()}");
                Save();
            }
        }

        public static void Save()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                try
                {
                    MyLog.Default.Info("[WeaponsOverhaul] Saving Settings");
                    Settings s = GetCurrentSettings();

                    TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(Filename, typeof(Settings));
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(s));
                    writer.Close();
                }
                catch (Exception e)
                {
                    MyLog.Default.Error($"[WeaponsOverhaul] Failed to save settings\n{e.ToString()}");
                }
            }
        }

        public static Settings GetCurrentSettings()
        {
            Settings s = new Settings();
            foreach (WeaponEffect w in WeaponEffectLookup.Values)
            {
                s.WeaponEffects.Add(w);
            }

            foreach (CollisionEffect a in AmmoEffectLookup.Values)
            {
                s.AmmoEffects.Add(a);
            }
            return s;
        }

        public static void SetNewSettings(Settings s)
        {
            WeaponEffectLookup.Clear();
            foreach (WeaponEffect w in s.WeaponEffects)
            {
                if (WeaponEffectLookup.ContainsKey(w.WeaponId.ToString()))
                {
                    MyLog.Default.Warning($"[WeaponsOverhaul] Skipping '{w.WeaponId}'. Already in dictionary");
                    continue;
                }

                WeaponEffectLookup.Add(w.WeaponId.ToString(), w);
            }

            AmmoEffectLookup.Clear();
            foreach (CollisionEffect a in s.AmmoEffects)
            {
                if (AmmoEffectLookup.ContainsKey(a.AmmoId.ToString()))
                {
                    MyLog.Default.Warning($"[WeaponsOverhaul] Skipping '{a.AmmoId}'. Already in dictionary");
                    continue;
                }

                AmmoEffectLookup.Add(a.AmmoId.ToString(), a);
            }
        }

    }
}
