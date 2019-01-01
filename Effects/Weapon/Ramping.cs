using ProjectilesImproved.Weapons;
using ProtoBuf;

namespace ProjectilesImproved.Effects.Weapon
{
    [ProtoContract]
    public class Ramping
    {
        [ProtoMember]
        public float StartRPM
        {
            get { return startRPM; }
            set { startRPM = (value > 0) ? value : 1; }
        }

        [ProtoMember]
        public float MaxRPM
        {
            get { return maxRPM; }
            set { maxRPM = (value >= StartRPM) ? value : StartRPM; }
        }

        [ProtoMember]
        public float TimeToMax
        {
            get { return timeToMax; }
            set { timeToMax = (value <= 0) ? 1 : value; }
        }

        [ProtoMember]
        public float RampDownScaler
        {
            get { return rampDownScaler; }
            set { rampDownScaler = (value <= 0) ? 1 : value; }
        }

        private float rampDownScaler = 1;
        private float timeToMax = 0;
        private float maxRPM = 1;
        private float startRPM = 1;

        private float currentTime = 0; // in miliseconds

        public Ramping Clone()
        {
            return new Ramping
            {
                StartRPM = StartRPM,
                MaxRPM = MaxRPM,
                TimeToMax = TimeToMax,
                RampDownScaler = RampDownScaler
            };
        }
    }
}
