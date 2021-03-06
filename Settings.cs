﻿using ProjectilesImproved.Definitions;
using ProjectilesImproved.Effects.Collision;
using ProjectilesImproved.Effects.Weapon;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Utils;

namespace ProjectilesImproved
{
    [ProtoContract]
    public class Settings
    {
        public const string Filename = "WeaponsOverhaul.cfg";

        public static Settings Instance = new Settings();

        [XmlIgnore]
        public bool HasBeenSetByServer;

        [ProtoMember]
        public bool UseTurretLeadIndicators;

        [ProtoMember]
        public bool UseFixedGunLeadIndicators;

        [ProtoMember]
        public List<WeaponDefinition> WeaponDefinitions { get; set; } = new List<WeaponDefinition>();

        [ProtoMember]
        public List<ProjectileDefinition> ProjectileDefinitions { get; set; } = new List<ProjectileDefinition>();

        private static Settings DefaultSettings = null;

        public static Dictionary<string, ProjectileDefinition> ProjectileDefinitionLookup { get; set; } = new Dictionary<string, ProjectileDefinition>
        {
            { "OKI230mmAmmoPars", new ProjectileDefinition()
                {
                    UseDefaultsFromSBC = true,
                    SubtypeId = "OKI230mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 1f,
                    UseOverKillSpread = false,
                    IgnoreDamageReduction = true,
                    Penetration = new Penetration() { VelocityDecreasePerHp = 0 },
                }
            },
            {
                "OKI76mmAmmoPars", new ProjectileDefinition()
                {
                    UseDefaultsFromSBC = true,
                    SubtypeId = "OKI76mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 1f,
                    UseOverKillSpread = false,
                }
            },
            { "OKI50mmAmmoPars", new ProjectileDefinition()
                {
                    UseDefaultsFromSBC = true,
                    SubtypeId = "OKI50mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 1f,
                    UseOverKillSpread = false,
                }
            },
            { "OKI23mmAmmoPars", new ProjectileDefinition()
                {
                    UseDefaultsFromSBC = true,
                    SubtypeId = "OKI23mmAmmoPars",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 1f,
                    Ricochet = new Ricochet { DeflectionAngle = 45, MaxDamageTransfer = 0.5f, MaxVelocityTransfer = 0.25f, RicochetChance = 0.5f },
                }
            },
            {
               "LargeCaliber", new ProjectileDefinition()
                {
                    UseDefaultsFromSBC = true,
                    SubtypeId = "LargeCaliber",
                    HasBulletDrop = false,
                    BulletDropGravityScaler = 1f,
                    UseOverKillSpread = false,
                    OverKillSpreadScaler = 1,
                    Ricochet = new Ricochet { DeflectionAngle = 30, MaxDamageTransfer = 0.5f, MaxVelocityTransfer = 0.25f, RicochetChance = 0.5f },
                }
            },
            {
               "SmallCaliber", new ProjectileDefinition()
                {
                    UseDefaultsFromSBC = true,
                    SubtypeId = "SmallCaliber",
                    HasBulletDrop = true,
                    BulletDropGravityScaler = 1f,
                    UseOverKillSpread = false,
                    Ricochet = new Ricochet { DeflectionAngle = 90, MaxDamageTransfer = 0.25f, MaxVelocityTransfer = 0.25f, RicochetChance = 0.25f },
                }
            },
            {
                "DeU_25x184mm", new ProjectileDefinition()
                {
                    UseDefaultsFromSBC = true,
                    SubtypeId = "DeU_25x184mm",
                    HasBulletDrop = false,
                    UseOverKillSpread = false,
                    Ricochet = new Ricochet { DeflectionAngle = 45, MaxDamageTransfer = 0.25f, MaxVelocityTransfer = 0.50f, RicochetChance = 0.5f },
                }
            }
        };

        public static Dictionary<string, WeaponDefinition> WeaponDefinitionLookup { get; set; } = new Dictionary<string, WeaponDefinition>
        {
            { "PDCTurret", new WeaponDefinition()
                {
                    UseDefaultsFromSBC = true,
                    SubtypeId = "PDCTurret",
                    Ramping = new Ramping() { StartRPM = 1, MaxRPM = 2000, TimeToMax = 25000, RampDownScaler = 5f }
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
                    Save();
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

            s.UseTurretLeadIndicators = Instance.UseTurretLeadIndicators;
            s.UseFixedGunLeadIndicators = Instance.UseFixedGunLeadIndicators;

            foreach (WeaponDefinition w in WeaponDefinitionLookup.Values)
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
            WeaponDefinitionLookup.Clear();
            foreach (WeaponDefinition w in s.WeaponDefinitions)
            {
                if (WeaponDefinitionLookup.ContainsKey(w.SubtypeId))
                {
                    MyLog.Default.Warning($"[WeaponsOverhaul] Skipping '{w.SubtypeId}'. Already in dictionary");
                    continue;
                }

                WeaponDefinitionLookup.Add(w.SubtypeId.ToString(), w);
            }

            ProjectileDefinitionLookup.Clear();
            foreach (ProjectileDefinition a in s.ProjectileDefinitions)
            {
                if (ProjectileDefinitionLookup.ContainsKey(a.SubtypeId))
                {
                    MyLog.Default.Warning($"[WeaponsOverhaul] Skipping '{a.SubtypeId}'. Already in dictionary");
                    continue;
                }

                ProjectileDefinitionLookup.Add(a.SubtypeId, a);
            }

            MergeSBCInfo();

            Settings current = GetCurrentSettings();
            current.HasBeenSetByServer = true;
            current.UseTurretLeadIndicators = s.UseTurretLeadIndicators;
            current.UseFixedGunLeadIndicators = s.UseFixedGunLeadIndicators;

            Instance = current;
        }

        public static void Init()
        {
            DefaultSettings = GetCurrentSettings();
            Load();
        }

        public static ProjectileDefinition GetAmmoDefinition(string id)
        {
            if (ProjectileDefinitionLookup.ContainsKey(id))
            {
                return ProjectileDefinitionLookup[id].Clone();
            }

            return new ProjectileDefinition().Clone();
        }

        public static WeaponDefinition GetWeaponDefinition(string id)
        {
            if (WeaponDefinitionLookup.ContainsKey(id))
            {
                return WeaponDefinitionLookup[id].Clone();
            }

            return new WeaponDefinition().Clone();

        }

        private static void MergeSBCInfo()
        {
            foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                try
                {
                    if (def is MyWeaponBlockDefinition)
                    {
                        MyWeaponBlockDefinition block = def as MyWeaponBlockDefinition;
                        MyWeaponDefinition w = MyDefinitionManager.Static.GetWeaponDefinition(block.WeaponDefinitionId);

                        List<WeaponAmmoDefinition> WeaponAmmosDefs = new List<WeaponAmmoDefinition>();
                        // there are five types in MyAmmoType
                        WeaponAmmosDefs.Add(new WeaponAmmoDefinition());
                        WeaponAmmosDefs.Add(new WeaponAmmoDefinition());
                        WeaponAmmosDefs.Add(new WeaponAmmoDefinition());
                        WeaponAmmosDefs.Add(new WeaponAmmoDefinition());
                        WeaponAmmosDefs.Add(new WeaponAmmoDefinition());

                        foreach (MyDefinitionId id in w.AmmoMagazinesId)
                        {
                            MyAmmoMagazineDefinition mag = MyDefinitionManager.Static.GetAmmoMagazineDefinition(id);
                            MyAmmoDefinition ammo = MyDefinitionManager.Static.GetAmmoDefinition(mag.AmmoDefinitionId);
                            int index = (int)ammo.AmmoType;

                            WeaponAmmosDefs[index].RateOfFire = w.WeaponAmmoDatas[index].RateOfFire;
                            WeaponAmmosDefs[index].ShotsInBurst = w.WeaponAmmoDatas[index].ShotsInBurst;
                            WeaponAmmosDefs[index].ShootSound = w.WeaponAmmoDatas[index].ShootSound;
                        }

                        WeaponDefinition SBCWeapon = new WeaponDefinition()
                        {
                            UseDefaultsFromSBC = true,
                            SubtypeId = w.Id.SubtypeId.String,
                            DeviateShotAngle = w.DeviateShotAngle,
                            ReloadTime = w.ReloadTime,
                            AmmoDatas = WeaponAmmosDefs,

                            ReleaseTimeAfterFire = w.ReleaseTimeAfterFire,
                            PhysicalMaterial = w.PhysicalMaterial,
                            DamageMultiplier = w.DamageMultiplier,
                            MuzzleFlashLifeSpan = w.MuzzleFlashLifeSpan,
                            UseDefaultMuzzleFlash = w.UseDefaultMuzzleFlash,

                            NoAmmoSound = w.NoAmmoSound,
                            ReloadSound = w.ReloadSound,
                            SecondarySound = w.SecondarySound,
                        };

                        if (WeaponDefinitionLookup.ContainsKey(w.Id.SubtypeId.String))
                        {
                            WeaponDefinition configWeapon = WeaponDefinitionLookup[w.Id.SubtypeId.String];

                            if (configWeapon.UseDefaultsFromSBC)
                            {
                                SBCWeapon.Ramping = (configWeapon.Ramping == null) ? null : configWeapon.Ramping.Clone();

                                WeaponDefinitionLookup[w.Id.SubtypeId.String] = SBCWeapon;
                            }
                        }
                        else
                        {
                            WeaponDefinitionLookup.Add(w.Id.SubtypeId.String, SBCWeapon);
                        }

                    }
                    else if (def is MyAmmoMagazineDefinition)
                    {
                        MyAmmoDefinition ammo = MyDefinitionManager.Static.GetAmmoDefinition((def as MyAmmoMagazineDefinition).AmmoDefinitionId);
                        if (ammo.IsExplosive) continue;

                        MyProjectileAmmoDefinition p = ammo as MyProjectileAmmoDefinition;
                        ProjectileDefinition SBCProjectile = new ProjectileDefinition()
                        {
                            UseDefaultsFromSBC = true,
                            SubtypeId = p.Id.SubtypeId.String,
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
                            BulletDropGravityScaler = 1f,
                            UseOverKillSpread = false,
                            OverKillSpreadScaler = 1f,
                            IgnoreDamageReduction = false
                        };

                        if (ProjectileDefinitionLookup.ContainsKey(p.Id.SubtypeId.String))
                        {
                            ProjectileDefinition configProjectile = ProjectileDefinitionLookup[p.Id.SubtypeId.String];

                            if (configProjectile.UseDefaultsFromSBC)
                            {
                                SBCProjectile.HasBulletDrop = configProjectile.HasBulletDrop;
                                SBCProjectile.BulletDropGravityScaler = configProjectile.BulletDropGravityScaler;
                                SBCProjectile.UseOverKillSpread = configProjectile.UseOverKillSpread;
                                SBCProjectile.OverKillSpreadScaler = configProjectile.OverKillSpreadScaler;
                                SBCProjectile.IgnoreDamageReduction = configProjectile.IgnoreDamageReduction;
                                SBCProjectile.Ricochet = (configProjectile.Ricochet == null) ? null : configProjectile.Ricochet.Clone();
                                SBCProjectile.Penetration = (configProjectile.Penetration == null) ? null : configProjectile.Penetration.Clone();
                                SBCProjectile.Explosive = (configProjectile.Explosive == null) ? null : configProjectile.Explosive.Clone();

                                ProjectileDefinitionLookup[p.Id.SubtypeId.String] = SBCProjectile;
                            }
                        }
                        else
                        {
                            ProjectileDefinitionLookup.Add(p.Id.SubtypeId.String, SBCProjectile);
                        }

                    }
                }
                catch (Exception e)
                {
                    MyLog.Default.Error($"Failed to load definition: {def.Id.ToString()}\n{e.ToString()}");
                }
            }
        }
    }
}
