using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectilesImproved.Definitions
{
    public enum TerminalState { Shoot_On, Shoot_Off, ShootOnce }

    [ProtoContract]
    public class TerminalShoot
    {
        [ProtoMember]
        public long BlockId;

        [ProtoMember]
        public TerminalState State;
    }
}
