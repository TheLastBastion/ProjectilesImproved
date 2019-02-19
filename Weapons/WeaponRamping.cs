using ProjectilesImproved.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectilesImproved.Weapons
{
    public class WeaponRamping : WeaponBasic, IWeapon
    {

        public float CurrentRampingTime = 0; // in miliseconds

        public override void Update()
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                MyAPIGateway.Utilities.ShowNotification($"{(IsShooting ? "Shooting" : "Idle")}, RoF: {AmmoDatas[0].RateOfFire}, Shots: {CurrentShotInBurst}/{AmmoDatas[0].ShotsInBurst}, {(CurrentReloadTime > 0 ? $"Cooldown {(ReloadTime - CurrentReloadTime).ToString("n0")}/{ReloadTime}, " : "")}release: {CurrentReleaseTime.ToString("n0")}/{ReleaseTimeAfterFire}, Time: {TimeTillNextShot.ToString("n2")}", 1);
            }

            WillFireThisFrame = true;

            if (FirstTimeCooldown > 0)
            {
                FirstTimeCooldown--;
                TerminalShootOnce = false;
                WillFireThisFrame = false;
                return;
            }

            // If cooldown is greater than 0 the gun is on cooldown and should not fire
            // reduce cooldown and dont fire projectiles
            if (CurrentReloadTime > 0)
            {
                CurrentReloadTime -= Tools.MillisecondPerFrame;
                WillFireThisFrame = false;
            }

            // if the block is not functional toggle shooting to off
            // this is not venilla and may get changed
            if (!Cube.IsFunctional)
            {
                TerminalShooting = false;
                WillFireThisFrame = false;
            }

            // if a user is manually shooting toggle terminal shoot off
            if (gun.IsShooting)
            {
                TerminalShooting = false;
            }

            if (!IsShooting ||
                Cube?.CubeGrid?.Physics == null ||
                !gun.GunBase.HasEnoughAmmunition() ||
                !WillFireThisFrame)
            {
                // this makes sure the gun will fire instantly when fire condisions are met
                if (TimeTillNextShot < 1)
                {
                    TimeTillNextShot += AmmoDatas[AmmoType].RateOfFire * Tools.FireRateMultiplayer;
                }

                if (TimeTillNextShot > 1)
                {
                    TimeTillNextShot = 1;
                }

                WillFireThisFrame = false;
            }

            if (WillFireThisFrame)
            {
                AmmoDatas[AmmoType].RateOfFire = CurrentRPM();
                TimeTillNextShot += AmmoDatas[AmmoType].RateOfFire * Tools.FireRateMultiplayer;

                CurrentRampingTime += Tools.MillisecondPerFrame;
                if (CurrentRampingTime > Ramping.TimeToMax)
                {
                    CurrentRampingTime = Ramping.TimeToMax;
                }
            }
            else if (CurrentRampingTime >= 0)
            {
                StopShootingSound();
                CurrentRampingTime -= (Tools.MillisecondPerFrame * Ramping.RampDownScaler);
                AmmoDatas[AmmoType].RateOfFire = CurrentRPM();
                if (CurrentRampingTime < 0)
                {
                    CurrentRampingTime = 0;
                }
            }

            IdleReload();
        }

        private int CurrentRPM()
        {
            int rof = (int)(Ramping.StartRPM + (Ramping.MaxRPM - Ramping.StartRPM) * ((CurrentRampingTime <= Ramping.TimeToMax) ? (CurrentRampingTime / Ramping.TimeToMax) : 1));
            return rof < 0 ? 0 : rof;
        }
    }
}
