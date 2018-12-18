using ProjectilesImproved.Effects.Collision;
using ProjectilesImproved.Weapons;
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

        public static AmmoEffect GetAmmoEffect(string id)
        {
            if (AmmoEffectLookup.ContainsKey(id))
            {
                return AmmoEffectLookup[id].Clone();
            }

            return new AmmoEffect();
        }

        [ProtoMember]
        public List<WeaponEffects> WeaponEffects { get; set; } = new List<WeaponEffects>();

        [ProtoMember]
        public List<AmmoEffect> AmmoEffects { get; set; } = new List<AmmoEffect>();

        public static Dictionary<string, AmmoEffect> AmmoEffectLookup { get; set; } = new Dictionary<string, AmmoEffect>
        {
            { "MyObjectBuilder_AmmoDefinition/OKI230mmAmmoPars", new AmmoEffect()
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
                "MyObjectBuilder_AmmoDefinition/OKI76mmAmmoPars", new AmmoEffect()
                {
                    AmmoId = "MyObjectBuilder_AmmoDefinition/OKI76mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = false,
                    Explosive = new Explosive() { Offset = 1f, Angle = 20,  Radius = 7f, Resolution = 1.2f }
                }
            },
            { "MyObjectBuilder_AmmoDefinition/OKI50mmAmmoPars", new AmmoEffect()
                {
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = true,
                    OverKillSpreadScaler = 1,
                    AmmoId = "MyObjectBuilder_AmmoDefinition/OKI50mmAmmoPars",
                }
            },
            { "MyObjectBuilder_AmmoDefinition/OKI23mmAmmoPars", new AmmoEffect()
                {
                    AmmoId = "MyObjectBuilder_AmmoDefinition/OKI23mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    Ricochet = new Ricochet { DeflectionAngle = 20, MaxDamageTransfer = 0.25f, MaxVelocityTransfer = 0.25f, RicochetChance = 0.5f },
                }
            },
            {
               "MyObjectBuilder_AmmoDefinition/LargeCaliber", new AmmoEffect()
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
               "MyObjectBuilder_AmmoDefinition/SmallCaliber", new AmmoEffect()
                {
                    AmmoId = "MyObjectBuilder_AmmoDefinition/SmallCaliber",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = false,
                    Ricochet = new Ricochet { DeflectionAngle = 90, MaxDamageTransfer = 0.25f, MaxVelocityTransfer = 0.25f, RicochetChance = 0.25f },
                }
            }
        };

        private static Dictionary<string, WeaponEffects> WeaponEffectLookup { get; set; } = new Dictionary<string, WeaponEffects>
        {

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
            foreach (WeaponEffects w in WeaponEffectLookup.Values)
            {
                s.WeaponEffects.Add(w);
            }

            foreach (AmmoEffect a in AmmoEffectLookup.Values)
            {
                s.AmmoEffects.Add(a);
            }
            return s;
        }

        public static void SetNewSettings(Settings s)
        {
            WeaponEffectLookup.Clear();
            foreach (WeaponEffects w in s.WeaponEffects)
            {
                if (WeaponEffectLookup.ContainsKey(w.WeaponId.ToString()))
                {
                    MyLog.Default.Warning($"[WeaponsOverhaul] Skipping '{w.WeaponId}'. Already in dictionary");
                    continue;
                }

                WeaponEffectLookup.Add(w.WeaponId.ToString(), w);
            }

            AmmoEffectLookup.Clear();
            foreach (AmmoEffect a in s.AmmoEffects)
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
