using ProjectilesImproved.Bullets;
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
            { MyStringHash.GetOrCompute("OKI23mmAmmoPars"), new Ricochet() },
            //{ MyStringHash.GetOrCompute("OKI230mmAmmoPars"), new Ricochet() },
            { MyStringHash.GetOrCompute("OKI50mmAmmoPars"), new Ricochet() },
            { MyStringHash.GetOrCompute("OKI230mmAmmoPars"), new ExplosiveRounds2() { AffectVoxels = true, Radius = 5, NextEffect = null }},
            //{ MyStringHash.GetOrCompute("OKI50mmAmmoPars"), new ExplosiveRounds() { AffectVoxels = true, Radius = 1, NextEffect = null }}
        };

        //public static readonly Dictionary<MyStringHash, BulletBase> TravelEffectLookup = new Dictionary<MyStringHash, BulletBase>()
        //{
        //    {  }
        //}

        public const float GravityMultiplyer = 0.4f;


    }
}
