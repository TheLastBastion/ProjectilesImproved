using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Bullets
{
    public class BulletDrop : BulletBase
    {
        public override void Update()
        {
            Draw();

            LifeTimeTicks++;
            Vector3D PositionBeforeUpdate = new Vector3D(Position);

            ExternalForceData forceData = WorldPlanets.GetExternalForces(Position);

            Velocity = Velocity + (forceData.Gravity * Settings.GravityMultiplyer);
            Direction = Vector3D.Normalize(Velocity);
            Position += VelocityPerTick;
            DistanceTraveled += VelocityPerTick.LengthSquared();

            //previous vector + gravity vector (in terms  of velocity) = next vector without drag

            if (DistanceTraveled * LifeTimeTicks > Ammo.MaxTrajectory * Ammo.MaxTrajectory)
            {
                HasExpired = true;
            }

            CollisionDetection();
        }
    }
}