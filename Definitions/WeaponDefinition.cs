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
        public float ReleaseTimeAfterFire;

        [ProtoMember]
        public int MuzzleFlashLifeSpan;

        [ProtoMember]
        public bool UseDefaultMuzzleFlash;

        [ProtoMember]
        public int ReloadTime;

        [ProtoMember]
        public float DamageMultiplier;

        [ProtoMember]
        public MyStringHash PhysicalMaterial;

        [ProtoMember]
        public int RateOfFire;

        [XmlIgnore]
        public MySoundPair ShootSound;

        [XmlIgnore]
        public MySoundPair NoAmmoSound;

        [XmlIgnore]
        public MySoundPair ReloadSound;

        [XmlIgnore]
        public MySoundPair SecondarySound;

        [ProtoMember]
        public int ShotsInBurst; 

        [ProtoMember]
        public Ramping Ramping;


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
