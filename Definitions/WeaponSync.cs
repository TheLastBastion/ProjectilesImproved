using ProtoBuf;
using static ProjectilesImproved.Weapons.WeaponBasic;

namespace ProjectilesImproved.Definitions
{
    [ProtoContract]
    public class WeaponSync
    {
        [ProtoMember]
        public long BlockId;

        [ProtoMember]
        public int DeviationIndex;

        [ProtoMember]
        public TerminalState State;

        [ProtoMember]
        public int CurrentShotInBurst;

        [ProtoMember]
        public float CurrentReloadTime;

        [ProtoMember]
        public float CurrentIdleReloadTime;

        [ProtoMember]
        public float CurrentReleaseTime;

        [ProtoMember]
        public double TimeTillNextShot;

        [ProtoMember]
        public float CurrentRampingTime;
    }
}
