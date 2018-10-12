using Sandbox.Game;
using VRageMath;

namespace ProjectilesImproved.Bullets
{
    public class BulletDrop : BulletBase
    {
        public override void Update()
        {
            ExternalForceData forceData = WorldPlanets.GetExternalForces(PositionMatrix.Translation);

            Velocity = Velocity + (forceData.Gravity * Settings.GravityMultiplyer);
            PositionMatrix.Forward = Vector3D.Normalize(Velocity);
            PositionMatrix.Translation += VelocityPerTick;
            DistanceTraveled += VelocityPerTick.LengthSquared();

            if (IsAtRange)
            {
                HasExpired = true;
            }

            MyVisualScriptLogicProvider.AddGPS("", "", PositionMatrix.Translation, Color.Green);
        }
    }
}