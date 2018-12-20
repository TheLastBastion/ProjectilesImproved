using ProjectilesImproved.Weapons;
using ProtoBuf;
using Sandbox.ModAPI;

namespace ProjectilesImproved.Effects.Weapon
{
    [ProtoContract]
    public class Ramping : IWeapon
    {
        [ProtoMember]
        public float StartRPM;
        [ProtoMember]
        public float MaxRPM;

        [ProtoMember]
        public float TimeToMax; // in miliseconds

        private float currentTime = 0; // in miliseconds

        public bool Update(ProjectileWeapons weapon)
        {
            bool willShoot = true;

            // If cooldown is greater than 0 the gun is on cooldown and should not fire
            // reduce cooldown and dont fire projectiles
            if (weapon.CooldownTime > 0)
            {
                weapon.CooldownTime -= ProjectileWeapons.MillisecondPerFrame;
                willShoot = false;
            }

            // if the block is not functional toggle shooting to off
            // this is not venilla and may get changed
            if (!weapon.Cube.IsFunctional)
            {
                weapon.TerminalShooting = false;
                willShoot = false;
            }

            // if a user is manually shooting toggle terminal shoot off
            if (weapon.gun.IsShooting)
            {
                weapon.TerminalShooting = false;
            }

            if (!weapon.IsShooting ||
                weapon.Cube?.CubeGrid?.Physics == null ||
                !weapon.gun.GunBase.HasEnoughAmmunition() ||
                !willShoot)
            {
                // this makes sure the gun will fire instantly when fire condisions are met
                if (weapon.TimeTillNextShot < 1)
                {
                    weapon.TimeTillNextShot += weapon.RateOfFire * ProjectileWeapons.FireRateMultiplayer;
                }

                if (weapon.TimeTillNextShot > 1)
                {
                    weapon.TimeTillNextShot = 1;
                }

                willShoot = false;
            }

            if (willShoot)
            {
                weapon.RateOfFire = CurrentRPM();
                weapon.TimeTillNextShot += weapon.RateOfFire * ProjectileWeapons.FireRateMultiplayer;
                currentTime += ProjectileWeapons.MillisecondPerFrame;
                MyAPIGateway.Utilities.ShowNotification($"ROF: {weapon.RateOfFire} Time: {currentTime.ToString("n0")} MaxTime: {TimeToMax.ToString("n0")}", 1);
            }
            else
            {
                currentTime -= ProjectileWeapons.MillisecondPerFrame;
                if (currentTime < 0)
                {
                    currentTime = 0;
                }
            }

            return willShoot;
        }

        public int CurrentRPM()
        {
            return (int)(StartRPM + (MaxRPM - StartRPM) * ((currentTime <= TimeToMax) ? (currentTime / TimeToMax) : 1));
        }
    }
}
