using ProtoBuf;
using Sandbox.Definitions;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Bullets
{
    [ProtoContract]
    public class BulletsBase
    {
        public static MyStringId BulletMaterial = MyStringId.GetOrCompute("ProjectileTrailLine");
        public static MyStringHash Bullet = MyStringHash.GetOrCompute("bullet");
        public const float Tick = 1f / 60f;

        [ProtoMember(1)]
        public long ShooterID;

        [ProtoMember(2)]
        public MyDefinitionId WeaponId;

        [ProtoMember(3)]
        public MyDefinitionId MagazineId;

        [ProtoMember(4)]
        public Vector3D Direction;

        public bool HasExpired;

        public IMySlimBlock Slim;

        public MyWeaponDefinition Weapon;

        public MyAmmoMagazineDefinition Magazine;

        public MyAmmoDefinition Ammo;

        public double Distance;

        public Vector3D Position;

        public Vector3D Velocity;

        public float LastPositionFraction = 0;

        public int LifeTimeTicks = 0;

        //[XmlIgnore]
        //public int TillNextRaycast = 0; // in game ticks

        public virtual void Update() { }
    }
}
