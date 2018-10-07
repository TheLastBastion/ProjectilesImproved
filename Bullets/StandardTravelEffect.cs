using VRageMath;

namespace ProjectilesImproved.Bullets
{
    public class StandardTravelEffect : BulletBase
    {

        public override void Update()
        {
            Draw();

            LifeTimeTicks++;
            Vector3D PositionBeforeUpdate = new Vector3D(Position);

            Position += VelocityPerTick;
            DistanceTraveled += VelocityPerTick.LengthSquared();

            if (DistanceTraveled * LifeTimeTicks > Ammo.MaxTrajectory * Ammo.MaxTrajectory)
            {
                HasExpired = true;
            }

            CollisionDetection();
        }
    }
}
