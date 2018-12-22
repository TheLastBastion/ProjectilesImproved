using ProjectilesImproved.Effects.Collision;
using ProjectilesImproved.Projectiles;
using ProtoBuf;
using VRage.Game;
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

        [ProtoMember]
        public float SpeedVar;

        [ProtoMember]
        public float MaxTrajectory;

        [ProtoMember]
        public float BackkickForce;

        [ProtoMember]
        public string Material;

        [ProtoMember]
        public float ProjectileHitImpulse;

        [ProtoMember]
        public float ProjectileTrailScale;

        [ProtoMember]
        public Vector3 ProjectileTrailColor;

        [ProtoMember]
        public float ProjectileTrailProbability;

        [ProtoMember]
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

        [ProtoMember]
        public float BulletDropGravityScaler;

        [ProtoMember]
        public bool UseOverKillSpread;

        [ProtoMember]
        public float OverKillSpreadScaler;

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
