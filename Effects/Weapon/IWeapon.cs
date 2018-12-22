using System;
using System.Collections.Generic;
using System.Text;
using ProjectilesImproved.Weapons;

namespace ProjectilesImproved.Effects.Weapon
{
    public interface IWeapon
    {
        bool Update(ProjectileWeapon weapon);
    }
}
