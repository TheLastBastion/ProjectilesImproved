using ProjectilesImproved.Effects.Weapon;
using ProtoBuf;
using Sandbox.Game.Entities;
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
        public int RateOfFire;

        [ProtoMember]
        public int ShotsInBurst;

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
        public MySoundPair ShootSound;

        [XmlIgnore]
        public MySoundPair NoAmmoSound;

        [XmlIgnore]
        public MySoundPair ReloadSound;

        [XmlIgnore]
        public MySoundPair SecondarySound;


        public WeaponDefinition Clone()
        {
            return new WeaponDefinition
            {
                SubtypeId = SubtypeId,
                NoAmmoSound = NoAmmoSound,
                ReloadSound = ReloadSound,
                SecondarySound = SecondarySound,
                DeviateShotAngle = DeviateShotAngle,
                ReleaseTimeAfterFire = ReleaseTimeAfterFire,
                MuzzleFlashLifeSpan = MuzzleFlashLifeSpan,
                UseDefaultMuzzleFlash = UseDefaultMuzzleFlash,
                ReloadTime = ReloadTime,
                DamageMultiplier = DamageMultiplier,

                Ramping = (Ramping == null) ? null : Ramping.Clone()
            };
        }
    }
}
