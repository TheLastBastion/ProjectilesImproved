using ProtoBuf;
using Sandbox.Game.Entities;
using System.Xml.Serialization;

namespace ProjectilesImproved.Definitions
{
    [ProtoContract]
    public class WeaponAmmoDefinition
    {
        [ProtoMember]
        public int RateOfFire;

        [ProtoMember]
        public int ShotsInBurst;

        [XmlIgnore]
        public MySoundPair ShootSound;
    }
}
