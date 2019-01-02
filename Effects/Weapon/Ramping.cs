using ProjectilesImproved.Weapons;
using ProtoBuf;

namespace ProjectilesImproved.Effects.Weapon
{
    [ProtoContract]
    public class Ramping : IWeapon
    {
        [ProtoMember]
        public float StartRPM
        {
            get { return startRPM; }
            set { startRPM = (value > 0) ? value : 1; }
        }

        [ProtoMember]
        public float MaxRPM
        {
            get { return maxRPM; }
            set { maxRPM = (value >= StartRPM) ? value : StartRPM; }
        }

        [ProtoMember]
        public float TimeToMax
        {
            get { return timeToMax; }
            set { timeToMax = (value <= 0) ? 1 : value; }
        }

        [ProtoMember]
        public float RampDownScaler
        {
            get { return rampDownScaler; }
            set { rampDownScaler = (value <= 0) ? 1 : value; }
        }

        private float rampDownScaler = 1;
        private float timeToMax = 0;
        private float maxRPM = 1;
        private float startRPM = 1;

        private float currentTime = 0; // in miliseconds

        public Ramping Clone()
        {
            return new Ramping
            {
                StartRPM = StartRPM,
                MaxRPM = MaxRPM,
                TimeToMax = TimeToMax,
                RampDownScaler = RampDownScaler
            };
        }

        public bool Update(ProjectileWeapon weapon)
        {
            bool willShoot = true;

            // If cooldown is greater than 0 the gun is on cooldown and should not fire
            // reduce cooldown and dont fire projectiles
            if (weapon.CurrentReloadTime > 0)
            {
                weapon.CurrentReloadTime -= Tools.MillisecondPerFrame;
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
                    weapon.TimeTillNextShot += weapon.Definition.AmmoDatas[weapon.AmmoType].RateOfFire * Tools.FireRateMultiplayer;
                }

                if (weapon.TimeTillNextShot > 1)
                {
                    weapon.TimeTillNextShot = 1;
                }

                willShoot = false;
            }

            if (willShoot)
            {
                weapon.Definition.AmmoDatas[weapon.AmmoType].RateOfFire = CurrentRPM();
                weapon.TimeTillNextShot += weapon.Definition.AmmoDatas[weapon.AmmoType].RateOfFire * Tools.FireRateMultiplayer;

                currentTime += Tools.MillisecondPerFrame;
                if (currentTime > TimeToMax)
                {
                    currentTime = TimeToMax;
                }
            }
            else if (currentTime != 0)
            {
                currentTime -= (Tools.MillisecondPerFrame * RampDownScaler);
                weapon.Definition.AmmoDatas[weapon.AmmoType].RateOfFire = CurrentRPM();
                if (currentTime < 0)
                {
                    currentTime = 0;
                }
            }

            return willShoot;
        }

        public int CurrentRPM()
        {
            int rof = (int)(StartRPM + (MaxRPM - StartRPM) * ((currentTime <= TimeToMax) ? (currentTime / TimeToMax) : 1));
            return rof < 0 ? 0 : rof;
        }
    }
}
