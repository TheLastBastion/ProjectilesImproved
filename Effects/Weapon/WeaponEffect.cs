using ProjectilesImproved.Weapons;
using ProtoBuf;

namespace ProjectilesImproved.Effects.Weapon
{
    [ProtoContract]
    public class WeaponEffect : IWeapon
    {
        [ProtoMember]
        public Ramping Ramping;

        public bool Update(ProjectileWeapons weapon)
        {
            if (Ramping != null)
            {
                return Ramping.Update(weapon);
            }


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
                weapon.TimeTillNextShot += weapon.RateOfFire * ProjectileWeapons.FireRateMultiplayer;
            }

            return willShoot;
        }
    }
}
