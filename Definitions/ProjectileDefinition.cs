using ProjectilesImproved.Effects.Collision;
using ProjectilesImproved.Projectiles;
using ProtoBuf;
using System.Xml.Serialization;
using VRageMath;

namespace ProjectilesImproved.Definitions
{
    [ProtoContract]
    public class ProjectileDefinition
    {
        [ProtoMember]
        public bool UseDefaultsFromSBC;

        [ProtoMember]
        public string SubtypeId;

        [ProtoMember]
        public float DesiredSpeed;

        [XmlIgnore]
        public float SpeedVar;

        [ProtoMember]
        public float MaxTrajectory;

        [ProtoMember]
        public float BackkickForce;

        [XmlIgnore]
        public string Material;

        [ProtoMember]
        public float ProjectileHitImpulse;

        [ProtoMember]
        public float ProjectileTrailScale;

        [ProtoMember]
        public Vector3 ProjectileTrailColor;

        [ProtoMember]
        public float ProjectileTrailProbability;

        [XmlIgnore]
        public string ProjectileOnHitEffectName;

        [ProtoMember]
        public float ProjectileMassDamage;

        [ProtoMember]
        public float ProjectileHealthDamage;

        [ProtoMember]
        public float ProjectileHeadShotDamage;

        // Mod stuff

        [ProtoMember]
        public bool HasBulletDrop;

        private float bulletDropGravityScaler;
        [ProtoMember]
        public float BulletDropGravityScaler
        {
            get { return bulletDropGravityScaler; }
            set { bulletDropGravityScaler = (value >= 0) ? value : 0; }
        }

        [ProtoMember]
        public bool UseOverKillSpread;

        private float overKillSpreadScaler;
        [ProtoMember]
        public float OverKillSpreadScaler
        {
            get { return overKillSpreadScaler; }
            set { overKillSpreadScaler = (value >= 0) ? value : 0;  }
        }

        [ProtoMember]
        public bool IgnoreDamageReduction;

        [ProtoMember]
        public Penetration Penetration;

        [ProtoMember]
        public Ricochet Ricochet;

        [ProtoMember]
        public Explosive Explosive;


        public ProjectileDefinition Clone()
        {
            return new ProjectileDefinition()
            {
                UseDefaultsFromSBC = UseDefaultsFromSBC,
                SubtypeId = SubtypeId,
                DesiredSpeed = DesiredSpeed,
                SpeedVar = SpeedVar,
                MaxTrajectory = MaxTrajectory,
                BackkickForce = BackkickForce,
                Material = Material,
                ProjectileHitImpulse = ProjectileHitImpulse,
                ProjectileTrailScale = ProjectileTrailScale,
                ProjectileTrailColor = ProjectileTrailColor,
                ProjectileTrailProbability = ProjectileTrailProbability,
                ProjectileOnHitEffectName = ProjectileOnHitEffectName,
                ProjectileMassDamage = ProjectileMassDamage,
                ProjectileHealthDamage = ProjectileHealthDamage,
                ProjectileHeadShotDamage = ProjectileHeadShotDamage,


                HasBulletDrop = HasBulletDrop,
                BulletDropGravityScaler = BulletDropGravityScaler,
                UseOverKillSpread = UseOverKillSpread,
                OverKillSpreadScaler = OverKillSpreadScaler,
                IgnoreDamageReduction = IgnoreDamageReduction,
                Ricochet = (Ricochet == null) ? null : Ricochet.Clone(),
                Penetration = (Penetration == null) ? null : Penetration.Clone(),
                Explosive = (Explosive == null) ? null : Explosive.Clone()
            };
        }

        public Projectile CreateProjectile()
        {
            return new Projectile
            {
                UseDefaultsFromSBC = UseDefaultsFromSBC,
                SubtypeId = SubtypeId,
                DesiredSpeed = DesiredSpeed,
                SpeedVar = SpeedVar,
                MaxTrajectory = MaxTrajectory,
                BackkickForce = BackkickForce,
                Material = Material,
                ProjectileHitImpulse = ProjectileHitImpulse,
                ProjectileTrailScale = ProjectileTrailScale,
                ProjectileTrailColor = ProjectileTrailColor,
                ProjectileTrailProbability = ProjectileTrailProbability,
                ProjectileOnHitEffectName = ProjectileOnHitEffectName,
                ProjectileMassDamage = ProjectileMassDamage,
                ProjectileHealthDamage = ProjectileHealthDamage,
                ProjectileHeadShotDamage = ProjectileHeadShotDamage,


                HasBulletDrop = HasBulletDrop,
                BulletDropGravityScaler = BulletDropGravityScaler,
                UseOverKillSpread = UseOverKillSpread,
                OverKillSpreadScaler = OverKillSpreadScaler,
                IgnoreDamageReduction = IgnoreDamageReduction,
                Ricochet = (Ricochet == null) ? null : Ricochet.Clone(),
                Penetration = (Penetration == null) ? null : Penetration.Clone(),
                Explosive = (Explosive == null) ? null : Explosive.Clone()
            };
        }
    }
}
