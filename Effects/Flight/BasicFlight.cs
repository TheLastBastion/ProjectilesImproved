﻿using System.Collections.Generic;
using ProjectilesImproved.Effects;
using ProjectilesImproved.Projectiles;
using VRage.Game.ModAPI;

namespace ProjectilesImproved.Effects.Flight
{
    public class BasicFlight : IFlight
    {
        public void Update(Projectile bullet)
        {
            bullet.PositionMatrix.Translation += bullet.VelocityPerTick;
            bullet.DistanceTraveled += bullet.VelocityPerTick.LengthSquared();

            if (bullet.IsAtRange)
            {
                bullet.HasExpired = true;
            }
        }
    }
}