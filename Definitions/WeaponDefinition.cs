﻿using ProjectilesImproved.Effects.Weapon;
using ProtoBuf;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.Utils;

namespace ProjectilesImproved.Definitions
{
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


        public WeaponDefinition Clone()
        {
            WeaponDefinition def = new WeaponDefinition
            {
                UseDefaultsFromSBC = UseDefaultsFromSBC,
                SubtypeId = SubtypeId,
                DeviateShotAngle = DeviateShotAngle,
                ReloadTime = ReloadTime,
                AmmoDatas = new List<WeaponAmmoDefinition>
                    {
                        new WeaponAmmoDefinition(),
                        new WeaponAmmoDefinition(),
                        new WeaponAmmoDefinition(),
                        new WeaponAmmoDefinition(),
                        new WeaponAmmoDefinition()
                    },

                Ramping = (Ramping == null) ? null : Ramping.Clone(),

                ReleaseTimeAfterFire = ReleaseTimeAfterFire,
                PhysicalMaterial = PhysicalMaterial,
                DamageMultiplier = DamageMultiplier,
                MuzzleFlashLifeSpan = MuzzleFlashLifeSpan,
                UseDefaultMuzzleFlash = UseDefaultMuzzleFlash,

                NoAmmoSound = NoAmmoSound,
                ReloadSound = ReloadSound,
                SecondarySound = SecondarySound,
            };

            for (int i = 0; i < AmmoDatas.Count; i++)
            {
                def.AmmoDatas[i].RateOfFire = AmmoDatas[i].RateOfFire;
                def.AmmoDatas[i].ShotsInBurst = AmmoDatas[i].ShotsInBurst;
                def.AmmoDatas[i].ShootSound = AmmoDatas[i].ShootSound;
            }

            return def;
        }
    }
}
