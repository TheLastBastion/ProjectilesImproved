using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;

namespace ProjectilesImproved.Definitions
{
    public class DamageDefinition
    {
        public IMyDestroyableObject Victim;

        public float Damage;

        public MyStringHash DamageType;

        public bool Sync;

        public MyHitInfo? Hit;

        public long AttackerId;
    }
}
