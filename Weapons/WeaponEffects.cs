using ProtoBuf;
using VRage.Game;

namespace ProjectilesImproved.Weapons
{
    [ProtoContract]
    public class WeaponEffects
    {
        [ProtoMember(1)]
        public string WeaponId { get; set; }
    }
}
