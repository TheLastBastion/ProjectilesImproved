using ProjectilesImproved.Bullets;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Weapons
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), false)]
    public class LargeTurret : WeaponsBase
    {
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallGatlingGun), false)]
    public class SmallGatling : WeaponsBase
    {
    }

    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncher), false)]
    //public class SmallMissileLauncher : WeaponsOverload
    //{
    //}

    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallMissileLauncherReload), false)]
    //public class SmallMissileLauncherReload : WeaponsOverload
    //{
    //}

    public class WeaponsBase : MyGameLogicComponent
    {
        public const float MillisecondPerFrame = 1000f / 60f;
        public const double FireRateMultiplayer = 1d / 60d / 60d;

        public bool ControlsUpdated = false;

        public IMyGunObject<MyGunBase> gun { get; private set; } = null;
        public bool IsShooting => gun.IsShooting || terminalShooting || (IsFixedGun && (Entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) == MyEntityUpdateEnum.EACH_FRAME);

        private MyEntity3DSoundEmitter soundEmitter;
        //private MyParticleEffect muzzleFlash;

        private MyWeaponDefinition weapon = null;
        private IMyFunctionalBlock block = null;
        private IMyCubeBlock cube = null;
        private double timeTillNextShot = 1d;
        private int currentShotInBurst = 0;
        private float cooldownTime = 0;

        private bool IsFixedGun = false;

        private bool terminalShooting = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            block = Entity as IMyFunctionalBlock;
            cube = Entity as IMyCubeBlock;
            gun = Entity as IMyGunObject<MyGunBase>;

            IsFixedGun = Entity is IMySmallGatlingGun || Entity is IMySmallMissileLauncher || Entity is IMySmallMissileLauncherReload;

            soundEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity, true, 1f);
            //MyParticlesManager.TryCreateParticleEffect("Muzzle_Flash_Large", gun.GunBase.GetMuzzleWorldMatrix(), out muzzleFlash);
            //muzzleFlash.Stop();

            if (!Core.IsInitialized)
            {
                Core.OnLoadComplete += init;
            }
            else
            {
                OverrideDefaultControls();
                getWeaponDef();
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        private void init()
        {
            Core.OnLoadComplete -= init;
            OverrideDefaultControls();
            getWeaponDef();
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        private void getWeaponDef()
        {
            if (gun != null)
            {
                weapon = MyDefinitionManager.Static.GetWeaponDefinition((block.SlimBlock.BlockDefinition as MyWeaponBlockDefinition).WeaponDefinitionId);

                // Thanks for the help Digi
                for (int i = 0; i < weapon.WeaponAmmoDatas.Length; i++)
                {
                    var ammoData = weapon.WeaponAmmoDatas[i];

                    if (ammoData == null)
                        continue;

                    ammoData.ShootIntervalInMiliseconds = int.MaxValue;
                }
            }
        }

        private void OverrideDefaultControls()
        {
            if (ControlsUpdated) return;
            ControlsUpdated = true;

            List<IMyTerminalAction> actions = new List<IMyTerminalAction>();

            if (Entity is IMyLargeTurretBase)
            {
                MyAPIGateway.TerminalControls.GetActions<IMyLargeTurretBase>(out actions);
            }
            else if (Entity is IMySmallGatlingGun)
            {
                MyAPIGateway.TerminalControls.GetActions<IMySmallGatlingGun>(out actions);
            }
            else if (Entity is IMySmallMissileLauncher)
            {
                MyAPIGateway.TerminalControls.GetActions<IMySmallMissileLauncher>(out actions);
            }
            else if (Entity is IMySmallMissileLauncherReload)
            {
                MyAPIGateway.TerminalControls.GetActions<IMySmallMissileLauncherReload>(out actions);
            }

            MyLog.Default.Info($"============ Terminal Actions ==============");

            foreach (IMyTerminalAction a in actions)
            {
                MyLog.Default.Info($"{a.Id}");

                if (a.Id == "Shoot")
                {
                    a.Action = (block) =>
                    {
                        WeaponsBase weps = block.GameLogic as WeaponsBase;
                        MyAPIGateway.Utilities.ShowNotification($"shoot", 1000);
                        weps.terminalShooting = !weps.terminalShooting;

                        //if (weps.NeedsUpdate == MyEntityUpdateEnum.EACH_FRAME)
                        //{
                        //    weps.terminalShooting = false;
                        //}
                        //else
                        //{
                        //    weps.terminalShooting = true;
                        //}
                    };

                    a.Writer = weaponsFiringWriter;
                }
                if (a.Id == "Shoot_On")
                {
                    a.Action = (block) =>
                    {
                        //MyAPIGateway.Utilities.ShowNotification($"Shoot on {block.GameLogic is WeaponsOverload}", 1000);

                        //WeaponsOverload weps = block.GameLogic as WeaponsOverload;

                        (block.GameLogic as WeaponsBase).terminalShooting = true;

                        //MyGunStatusEnum status;
                        //weps.gun.CanShoot(MyShootActionEnum.PrimaryAction, block.OwnerId, out status);
                        ////MyAPIGateway.Utilities.ShowNotification($"Shoot on {block.GameLogic is WeaponsOverload} {weps.gun.ShootDirectionUpdateTime} {weps.gun.GunBase.LastShootTime} {status.ToString()}", 1000);
                        //if (weps.gun.CanShoot(MyShootActionEnum.PrimaryAction, block.OwnerId, out status) && block.IsFunctional)
                        //{
                        //    weps.terminalShooting = true;
                        //}
                    };

                    a.Writer = weaponsFiringWriter;
                }
                else if (a.Id == "Shoot_Off")
                {
                    a.Action = (block) =>
                    {
                        MyAPIGateway.Utilities.ShowNotification($"Shoot off {block.GameLogic is WeaponsBase}", 1000);
                        (block.GameLogic as WeaponsBase).terminalShooting = false;
                    };

                    a.Writer = weaponsFiringWriter;
                }
            }

            List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
            if (Entity is IMyLargeTurretBase)
            {
                MyAPIGateway.TerminalControls.GetControls<IMyLargeTurretBase>(out controls);
            }
            else if (Entity is IMySmallGatlingGun)
            {
                MyAPIGateway.TerminalControls.GetControls<IMySmallGatlingGun>(out controls);
            }
            else if (Entity is IMySmallMissileLauncher)
            {
                MyAPIGateway.TerminalControls.GetControls<IMySmallMissileLauncher>(out controls);
            }
            else if (Entity is IMySmallMissileLauncherReload)
            {
                MyAPIGateway.TerminalControls.GetControls<IMySmallMissileLauncherReload>(out controls);
            }

            MyLog.Default.Info($"============ Terminal Controls ==============");

            foreach (IMyTerminalControl c in controls)
            {
                MyLog.Default.Info($"{c.Id}");

                if (c.Id == "Shoot")
                {
                    IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;

                    onoff.Setter = (block, value) =>
                    {
                        (block.GameLogic as WeaponsBase).terminalShooting = value;
                    };

                    onoff.Getter = (block) =>
                    {
                        return (block.GameLogic as WeaponsBase).terminalShooting;
                    };
                }
            }

            MyLog.Default.Flush();
        }

        private void weaponsFiringWriter(IMyTerminalBlock block, StringBuilder str)
        {
            if ((block.GameLogic as WeaponsBase).terminalShooting)
            {
                str.Append("On");
            }
            else
            {
                str.Append("Off");
            }
        }

        public override void UpdateBeforeSimulation()
        {
            MyAPIGateway.Utilities.ShowNotification($"{Entity.GetType().Name} {Entity.NeedsUpdate}", 1);
            if (timeTillNextShot < 1)
            {
                timeTillNextShot += weapon.WeaponAmmoDatas[GetAmmoLookup()].RateOfFire * FireRateMultiplayer;
            }

            if (cooldownTime > 0)
            {
                MyAPIGateway.Utilities.ShowNotification($"Reload: {cooldownTime.ToString("n0")}", 1);
                cooldownTime -= MillisecondPerFrame;
                return;
            }

            if (!IsShooting || !MyAPIGateway.Multiplayer.IsServer || cube?.CubeGrid?.Physics == null) return;

            if (gun.IsShooting) terminalShooting = false; // turns off auto shoot if user begins to fire

            if (!gun.GunBase.HasEnoughAmmunition())
            {
                return;
            }

            MyAPIGateway.Utilities.ShowNotification($"Mouse: {gun.IsShooting || (IsFixedGun && (Entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) == MyEntityUpdateEnum.EACH_FRAME)} Terminal: {terminalShooting} Next: {timeTillNextShot.ToString("n3")} BurstShot: {currentShotInBurst}/{gun.GunBase.ShotsInBurst} BackKick: {gun.GunBase.CurrentAmmoDefinition.BackkickForce}", 1); // {weapon.WeaponAmmoDatas[GetAmmoLookup()].RateOfFire}

            if (timeTillNextShot >= 1)
            {
                Vector3D direction = gun.GunBase.GetDeviatedVector(gun.GunBase.DeviateAngle, gun.GunBase.GetMuzzleWorldMatrix().Forward);

                Standard fireData = new Standard()
                {
                    ShooterID = Entity.EntityId,
                    WeaponId = weapon.Id,
                    Weapon = weapon,
                    MagazineId = gun.GunBase.CurrentAmmoMagazineId,
                    Magazine = gun.GunBase.CurrentAmmoMagazineDefinition,
                    Ammo = gun.GunBase.CurrentAmmoDefinition,
                    Direction = direction,//Vector3D.IsUnit(ref direction) ? direction : Vector3D.Normalize(direction),
                    Position = gun.GunBase.GetMuzzleWorldPosition(),
                    Velocity = block.CubeGrid.Physics.LinearVelocity + direction * gun.GunBase.CurrentAmmoDefinition.DesiredSpeed
                };

                while (timeTillNextShot >= 1)
                {
                    Core.SpawnProjectile(fireData);
                    gun.GunBase.ConsumeAmmo();
                    timeTillNextShot--;

                    soundEmitter.PlaySound(gun.GunBase.ShootSound, false, false, false, false, false, null);
                    //soundEmitter.PlaySound(gun.GunBase.SecondarySound, false, false, false, false, false, null);

                    currentShotInBurst++;
                    if (currentShotInBurst == gun.GunBase.ShotsInBurst)
                    {
                        timeTillNextShot = 0;
                        currentShotInBurst = 0;
                        cooldownTime = weapon.ReloadTime;
                        break;
                    }

                    fireData.Position += direction * (timeTillNextShot * 0.03); // using timeTillNextShot to offset bullets being fired in the same frame
                }

                var forceVector = -direction * gun.GunBase.CurrentAmmoDefinition.BackkickForce;
                block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, forceVector, block.GetPosition(), null);
            }
        }

        private int GetAmmoLookup()
        {
            if (gun.GunBase.CurrentAmmoDefinition != null)
            {
                return (int)gun.GunBase.CurrentAmmoDefinition.AmmoType;
            }

            return 0;
        }

        public override void Close()
        {
            if (soundEmitter != null)
            {
                soundEmitter.StopSound(true, true);
            }
        }
    }
}
