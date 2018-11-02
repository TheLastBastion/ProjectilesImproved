﻿using ProtoBuf;
using Sandbox.Game;
using VRageMath;

namespace ProjectilesImproved.Bullets
{
    [ProtoContract]
    public class BulletDrop : BulletBase
    {
        public override void Update()
        {
            ExternalForceData forceData = WorldPlanets.GetExternalForces(PositionMatrix.Translation);

            Velocity = Velocity + (forceData.Gravity * Settings.GravityMultiplyer);
            PositionMatrix.Forward = Vector3D.Normalize(Velocity - InitialGridVelocity);

            PositionMatrix.Translation += VelocityPerTick;
            DistanceTraveled += VelocityPerTick.LengthSquared();

            if (IsAtRange)
            {
                HasExpired = true;
            }
        }
    }
}