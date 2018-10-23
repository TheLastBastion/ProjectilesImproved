using ProjectilesImproved.Effects;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved
{
    public class Settings
    {
        public const bool DebugMode = true;
        public const bool DebugMode_ShowBlockOctants = false;
        public const bool DebugMode_ShowSphereOctants = false;
        public const bool DebugMode_ShowBlockRayIntersects = false;


        public static readonly Color[] DebugOctantColors = new Color[] { Color.Green, Color.Blue, Color.Orange, Color.Black, Color.HotPink, Color.Red, Color.LightGreen, Color.White };

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
