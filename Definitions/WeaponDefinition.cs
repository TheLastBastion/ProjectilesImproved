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
        public MySoundPair NoAmmoSound;

        [ProtoMember]
        public MySoundPair ReloadSound;

        [ProtoMember]
        public MySoundPair SecondarySound;

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

        //public List<MyDefinitionId> AmmoMagazinesId = new List<MyDefinitionId>();

        //public List<MyWeaponAmmoData> WeaponAmmoDatas = new List<MyWeaponAmmoData>();

        //public List<MyWeaponEffect> WeaponEffects = new List<MyWeaponEffect>();

        [XmlIgnore]
        public int RateOfFire;

        [XmlIgnore]
        public MySoundPair ShootSound;
        
        [XmlIgnore]
        public int ShotsInBurst; 


        //public MyDefinitionId[] AmmoMagazinesId;

        //public MyWeaponAmmoData[] WeaponAmmoDatas;

        //public MyWeaponEffect[] WeaponEffects;

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
