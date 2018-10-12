using ProjectilesImproved.Effects;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Utils;

namespace ProjectilesImproved
{
    public class Settings
    {

        public static readonly Dictionary<MyStringHash, EffectBase> AmmoEffectLookup = new Dictionary<MyStringHash, EffectBase>()
        {
            { MyStringHash.GetOrCompute("OKI23mmAmmoPars"), new Ricochet() }
            //{ MyStringHash.GetOrCompute("OKI230mmAmmoPars"), new ExplosiveRounds() { AffectVoxels = true, Radius = 5, NextEffect = null }},
            //{ MyStringHash.GetOrCompute("OKI50mmAmmoPars"), new ExplosiveRounds() { AffectVoxels = true, Radius = 1, NextEffect = null }}
        };

        public const float GravityMultiplyer = 0.4f;


    }
}
