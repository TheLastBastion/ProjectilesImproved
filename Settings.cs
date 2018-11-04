﻿using ProjectilesImproved.Effects;
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
        public const bool DebugMode = true;
        public const bool DebugMode_ShowBlockOctants = false;
        public const bool DebugMode_ShowSphereOctants = false;
        public const bool DebugMode_ShowBlockRayIntersects = false;

        public static readonly Color[] DebugOctantColors = new Color[] { Color.Green, Color.Blue, Color.Orange, Color.Black, Color.HotPink, Color.Red, Color.LightGreen, Color.White };

        public const string Filename = "WeaponsOverhaul.cfg";

        [ProtoMember]
        public List<WeaponEffects> WeaponEffects { get; set; } = new List<WeaponEffects>();

        [ProtoMember]
        public List<AmmoEffects> AmmoEffects { get; set; } = new List<AmmoEffects>();


        public static Settings Defaults { get; } = GetCurrentSettings();

        public static Dictionary<string, AmmoEffects> AmmoEffectLookup { get; private set; } = new Dictionary<string, AmmoEffects>
        {
            { "MyObjectBuilder_AmmoDefinition/OKI230mmAmmoPars", new AmmoEffects()
                {
                    AmmoId = "MyObjectBuilder_AmmoDefinition/OKI230mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropMultiplyer = 0.3f,
                    Explosive = new Explosive() { Radius = 5, Resolution = 0.5f, Angle = 180, Offset = 0, AffectVoxels = true },
                }
            },
            { "MyObjectBuilder_AmmoDefinition/OKI23mmAmmoPars", new AmmoEffects()
                {
                    AmmoId = "MyObjectBuilder_AmmoDefinition/OKI23mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropMultiplyer = 2f,
                    Ricochet = new Ricochet { DeflectionAngle = 30, MaxDamageTransfer = 0.25f, MaxVelocityTransfer = 0.25f, RicochetChance = 1f },
                }
            },
            { "MyObjectBuilder_AmmoDefinition/OKI50mmAmmoPars", new AmmoEffects()
                {
                    AmmoId = "MyObjectBuilder_AmmoDefinition/OKI50mmAmmoPars",
                }
            },
            {
               "MyObjectBuilder_AmmoDefinition/LargeCaliber", new AmmoEffects()
                {
                    AmmoId = "MyObjectBuilder_AmmoDefinition/LargeCaliber",
                    Ricochet = new Ricochet { DeflectionAngle = 90, MaxDamageTransfer = 0.25f, MaxVelocityTransfer = 0.25f, RicochetChance = 0.5f },
                }
            }
        };

        public static Dictionary<string, WeaponEffects> WeaponEffectLookup { get; private set; } = new Dictionary<string, WeaponEffects>
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

            foreach (AmmoEffects a in AmmoEffectLookup.Values)
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
            foreach (AmmoEffects a in s.AmmoEffects)
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
