using ProjectilesImproved.Bullets;
using ProtoBuf;
using VRage.Game.ModAPI;

namespace ProjectilesImproved.Effects
{
    [ProtoContract]
    public class ExplosiveRounds : EffectBase
    {
        [ProtoMember(2)]
        public float Radius { get; set; }

        [ProtoMember(3)]
        public bool AffectVoxels { get; set; }

        public override void Execute(IHitInfo hit, BulletBase bullet)
        {

        }
    }
}