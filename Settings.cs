using ProjectilesImproved.Effects;
using System.Collections.Generic;
using VRage.Utils;

namespace ProjectilesImproved
{
    public class Settings
    {
        public static readonly Dictionary<MyStringHash, EffectBase> AmmoEffectLookup = new Dictionary<MyStringHash, EffectBase>()
        {
            { MyStringHash.GetOrCompute("OKI230mmAmmoPars"), new EffectBase()
                {
                    Explosive = new Explosive() { Radius = 5, Resolution = 0.5f, Angle = 100, Offset = 0, AffectVoxels = true },
                }
            },
        };

        public const float GravityMultiplyer = 0.4f;


    }
}
