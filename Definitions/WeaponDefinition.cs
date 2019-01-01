using ProjectilesImproved.Effects.Weapon;
using ProtoBuf;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.Utils;

namespace ProjectilesImproved.Definitions
{
    public enum WeaponType { Basic, Ramping }

    [ProtoContract]
    public class WeaponDefinition
    {
        [ProtoMember]
        public bool UseDefaultsFromSBC;

        [ProtoMember]
        public string SubtypeId;

        [ProtoMember]
        public float DeviateShotAngle;

        [ProtoMember]
        public int ReloadTime;

        [ProtoMember]
        public List<WeaponAmmoDefinition> AmmoDatas = new List<WeaponAmmoDefinition>();

        // mod effects
        [ProtoMember]
        public Ramping Ramping;

        // Unused stuff
        [XmlIgnore]
        public float ReleaseTimeAfterFire;

        [XmlIgnore]
        public MyStringHash PhysicalMaterial;

        [XmlIgnore]
        public float DamageMultiplier;

        [XmlIgnore]
        public int MuzzleFlashLifeSpan;

        [XmlIgnore]
        public bool UseDefaultMuzzleFlash;

        [XmlIgnore]
        public MySoundPair NoAmmoSound;

        [XmlIgnore]
        public MySoundPair ReloadSound;

        [XmlIgnore]
        public MySoundPair SecondarySound;

        public WeaponType Type()
        {
            if (Ramping != null)
            {
                return WeaponType.Ramping;
            }

            return WeaponType.Basic;
        }

        public WeaponDefinition Clone()
        {
            WeaponDefinition def = new WeaponDefinition();
            Clone(def);
            return def;
        }

        public void Clone(WeaponDefinition d)
        {
            d.UseDefaultsFromSBC = UseDefaultsFromSBC;
            d.SubtypeId = SubtypeId;
            d.DeviateShotAngle = DeviateShotAngle;
            d.ReloadTime = ReloadTime;
            d.AmmoDatas = new List<WeaponAmmoDefinition>
                    {
                        new WeaponAmmoDefinition(),
                        new WeaponAmmoDefinition(),
                        new WeaponAmmoDefinition(),
                        new WeaponAmmoDefinition(),
                        new WeaponAmmoDefinition()
                    };
            d.Ramping = (Ramping == null) ? null : Ramping.Clone();

            d.ReleaseTimeAfterFire = ReleaseTimeAfterFire;
            d.PhysicalMaterial = PhysicalMaterial;
            d.DamageMultiplier = DamageMultiplier;
            d.MuzzleFlashLifeSpan = MuzzleFlashLifeSpan;
            d.UseDefaultMuzzleFlash = UseDefaultMuzzleFlash;

            d.NoAmmoSound = NoAmmoSound;
            d.ReloadSound = ReloadSound;
            d.SecondarySound = SecondarySound;

            for (int i = 0; i < AmmoDatas.Count; i++)
            {
                d.AmmoDatas[i].RateOfFire = AmmoDatas[i].RateOfFire;
                d.AmmoDatas[i].ShotsInBurst = AmmoDatas[i].ShotsInBurst;
                d.AmmoDatas[i].ShootSound = AmmoDatas[i].ShootSound;
            }
        }

        public void Set(WeaponDefinition def)
        {
            UseDefaultsFromSBC = def.UseDefaultsFromSBC;
            SubtypeId = def.SubtypeId;
            DeviateShotAngle = def.DeviateShotAngle;
            ReloadTime = def.ReloadTime;
            AmmoDatas = new List<WeaponAmmoDefinition>
                    {
                        new WeaponAmmoDefinition(),
                        new WeaponAmmoDefinition(),
                        new WeaponAmmoDefinition(),
                        new WeaponAmmoDefinition(),
                        new WeaponAmmoDefinition()
                    };
            Ramping = (def.Ramping == null) ? null : def.Ramping.Clone();

            ReleaseTimeAfterFire = def.ReleaseTimeAfterFire;
            PhysicalMaterial = def.PhysicalMaterial;
            DamageMultiplier = def.DamageMultiplier;
            MuzzleFlashLifeSpan = def.MuzzleFlashLifeSpan;
            UseDefaultMuzzleFlash = def.UseDefaultMuzzleFlash;

            NoAmmoSound = def.NoAmmoSound;
            ReloadSound = def.ReloadSound;
            SecondarySound = def.SecondarySound;

            for (int i = 0; i < def.AmmoDatas.Count; i++)
            {
                AmmoDatas[i].RateOfFire = def.AmmoDatas[i].RateOfFire;
                AmmoDatas[i].ShotsInBurst = def.AmmoDatas[i].ShotsInBurst;
                AmmoDatas[i].ShootSound = def.AmmoDatas[i].ShootSound;
            }
        }
    }
}
