using ProjectilesImproved.Definitions;
using ProjectilesImproved.Effects.Collision;
using ProjectilesImproved.Effects.Weapon;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using VRage.Game;
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

        public static void Init()
        {
            DefaultSettings = GetCurrentSettings();
            Load();
        }

        private static void MergeSBCInfo()
        {
            foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                MyLog.Default.Info($"Definition: {def.Id.SubtypeId.String} Type: {def.GetType()}");
                try
                {
                    if (def is MyAmmoMagazineDefinition)
                    {
                        MyAmmoDefinition ammo = MyDefinitionManager.Static.GetAmmoDefinition((def as MyAmmoMagazineDefinition).AmmoDefinitionId);
                        if (ammo.IsExplosive) continue;

                        MyProjectileAmmoDefinition p = ammo as MyProjectileAmmoDefinition;
                        MyLog.Default.Info($"Trajectory: {p.MaxTrajectory} Mass Damage: {p.ProjectileMassDamage}");

                        ProjectileDefinition SBCProjectile = new ProjectileDefinition()
                        {
                            UseFromSBC = true,
                            AmmoSubtypeId = p.Id.SubtypeId.String,
                            DesiredSpeed = p.DesiredSpeed,
                            SpeedVar = p.SpeedVar,
                            MaxTrajectory = p.MaxTrajectory,
                            BackkickForce = p.BackkickForce,
                            Material = p.ProjectileTrailMaterial,
                            ProjectileHitImpulse = p.ProjectileHitImpulse,
                            ProjectileTrailScale = p.ProjectileTrailScale,
                            ProjectileTrailColor = p.ProjectileTrailColor,
                            ProjectileTrailProbability = p.ProjectileTrailProbability,
                            ProjectileOnHitEffectName = p.ProjectileOnHitEffectName,
                            ProjectileMassDamage = p.ProjectileMassDamage,
                            ProjectileHealthDamage = p.ProjectileHealthDamage,
                            ProjectileHeadShotDamage = p.ProjectileHeadShotDamage,
                            HasBulletDrop = false,
                            BulletDropGravityScaler = 0.3f,
                            UseOverKillSpread = false,
                            OverKillSpreadScaler = 1,
                            IgnoreDamageReduction = false
                        };

                        if (ProjectileDefinitionLookup.ContainsKey(def.Id.SubtypeId.String))
                        {
                            ProjectileDefinition configProjectile = ProjectileDefinitionLookup[def.Id.SubtypeId.String];

                            if (configProjectile.UseFromSBC)
                            {
                                SBCProjectile.HasBulletDrop = configProjectile.HasBulletDrop;
                                SBCProjectile.BulletDropGravityScaler = configProjectile.BulletDropGravityScaler;
                                SBCProjectile.UseOverKillSpread = configProjectile.UseOverKillSpread;
                                SBCProjectile.OverKillSpreadScaler = configProjectile.OverKillSpreadScaler;
                                SBCProjectile.IgnoreDamageReduction = configProjectile.IgnoreDamageReduction;
                                SBCProjectile.Ricochet = (configProjectile.Ricochet == null) ? null : configProjectile.Ricochet.Clone();
                                SBCProjectile.Penetration = (configProjectile.Penetration == null) ? null : configProjectile.Penetration.Clone();
                                SBCProjectile.Explosive = (configProjectile.Explosive == null) ? null : configProjectile.Explosive.Clone();

                                MyLog.Default.Info($"Trajectory: {SBCProjectile.MaxTrajectory} Mass Damage: {SBCProjectile.ProjectileMassDamage}");
                                ProjectileDefinitionLookup[def.Id.SubtypeId.String] = SBCProjectile;
                                MyLog.Default.Info($"Trajectory: {ProjectileDefinitionLookup[def.Id.SubtypeId.String].MaxTrajectory} Mass Damage: {ProjectileDefinitionLookup[def.Id.SubtypeId.String].ProjectileMassDamage}");
                            }
                        }
                        else
                        {
                            ProjectileDefinitionLookup.Add(def.Id.SubtypeId.String, SBCProjectile);
                        }

                    }
                }
                catch (Exception e)
                {
                    MyLog.Default.Error($"Failed to load definition: {def.Id.ToString()}\n{e.ToString()}");
                }
            }
            Save();
        }

        public static ProjectileDefinition GetAmmoEffect(string id)
        {
            if (ProjectileDefinitionLookup.ContainsKey(id))
            {
                return ProjectileDefinitionLookup[id].Clone();
            }

            return new ProjectileDefinition();
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
        public List<WeaponEffect> WeaponDefinitions { get; set; } = new List<WeaponEffect>();

        [ProtoMember]
        public List<ProjectileDefinition> ProjectileDefinitions { get; set; } = new List<ProjectileDefinition>();

        private static Settings DefaultSettings = null;

        public static Dictionary<string, ProjectileDefinition> ProjectileDefinitionLookup { get; set; } = new Dictionary<string, ProjectileDefinition>
        {
            { "OKI230mmAmmoPars", new ProjectileDefinition()
                {
                    UseFromSBC = true,
                    AmmoSubtypeId = "OKI230mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = false,
                    IgnoreDamageReduction = true,
                    Penetration = new Penetration() { VelocityDecreasePerHp = 0 },
                }
            },
            {
                "OKI76mmAmmoPars", new ProjectileDefinition()
                {
                    UseFromSBC = true,
                    AmmoSubtypeId = "OKI76mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = false,
                    Explosive = new Explosive() { Offset = 1f, Angle = 20,  Radius = 7f, Resolution = 1.2f }
                }
            },
            { "OKI50mmAmmoPars", new ProjectileDefinition()
                {
                    UseFromSBC = true,
                    AmmoSubtypeId = "OKI50mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = true,
                    OverKillSpreadScaler = 1,
                }
            },
            { "OKI23mmAmmoPars", new ProjectileDefinition()
                {
                    UseFromSBC = true,
                    AmmoSubtypeId = "OKI23mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    Ricochet = new Ricochet { DeflectionAngle = 20, MaxDamageTransfer = 0.25f, MaxVelocityTransfer = 0.25f, RicochetChance = 0.5f },
                }
            },
            {
               "LargeCaliber", new ProjectileDefinition()
                {
                    UseFromSBC = true,
                    AmmoSubtypeId = "LargeCaliber",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = true,
                    OverKillSpreadScaler = 1,
                    Ricochet = new Ricochet { DeflectionAngle = 45, MaxDamageTransfer = 0.25f, MaxVelocityTransfer = 0.25f, RicochetChance = 0.35f },
                }
            },
            {
               "SmallCaliber", new ProjectileDefinition()
                {
                    UseFromSBC = true,
                    AmmoSubtypeId = "SmallCaliber",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 0.3f,
                    UseOverKillSpread = false,
                    Ricochet = new Ricochet { DeflectionAngle = 90, MaxDamageTransfer = 0.25f, MaxVelocityTransfer = 0.25f, RicochetChance = 0.25f },
                }
            }
        };

        public static Dictionary<string, WeaponEffect> WeaponEffectLookup { get; set; } = new Dictionary<string, WeaponEffect>
        {
            { "OKI23mmDG", new WeaponEffect()
                {
                    WeaponId = "OKI23mmDG",
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
                    MergeSBCInfo();
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
                s.WeaponDefinitions.Add(w);
            }

            foreach (ProjectileDefinition a in ProjectileDefinitionLookup.Values)
            {
                s.ProjectileDefinitions.Add(a);
            }
            return s;
        }

        public static void SetNewSettings(Settings s)
        {
            WeaponEffectLookup.Clear();
            foreach (WeaponEffect w in s.WeaponDefinitions)
            {
                if (WeaponEffectLookup.ContainsKey(w.WeaponId.ToString()))
                {
                    MyLog.Default.Warning($"[WeaponsOverhaul] Skipping '{w.WeaponId}'. Already in dictionary");
                    continue;
                }

                WeaponEffectLookup.Add(w.WeaponId.ToString(), w);
            }

            ProjectileDefinitionLookup.Clear();
            foreach (ProjectileDefinition a in s.ProjectileDefinitions)
            {
                if (ProjectileDefinitionLookup.ContainsKey(a.AmmoSubtypeId))
                {
                    MyLog.Default.Warning($"[WeaponsOverhaul] Skipping '{a.AmmoSubtypeId}'. Already in dictionary");
                    continue;
                }

                ProjectileDefinitionLookup.Add(a.AmmoSubtypeId, a);
            }

            MergeSBCInfo();
        }

    }
}
