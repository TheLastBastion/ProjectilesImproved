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
                    HasBulletDrop = true,
                    BulletDropMultiplyer = 0.3f,
                    Explosive = new Explosive() { Radius = 5, Resolution = 0.5f, Angle = 180, Offset = 0, AffectVoxels = true },
                }
            },
            { MyStringHash.GetOrCompute("OKI23mmAmmoPars"), new EffectBase()
                {
                    HasBulletDrop = true,
                    BulletDropMultiplyer = 2f,
                    Ricochet = new Ricochet { DeflectionAngle = 30, MaxDamageTransfer = 0f, MaxVelocityTransfer = 0f },
                }
            },
            { MyStringHash.GetOrCompute("OKI50mmAmmoPars"), new EffectBase()
                {
                }
            },
            {
                MyStringHash.GetOrCompute("LargeCaliber"), new EffectBase()
                {
                    Ricochet = new Ricochet { DeflectionAngle = 90, MaxDamageTransfer = 0f, MaxVelocityTransfer = 0f },
                } 
            }
        };

        //public const float GravityMultiplyer = 0.4f;


    }
}
