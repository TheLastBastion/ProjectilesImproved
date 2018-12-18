﻿using ProjectilesImproved.Effects.Collision;
using ProjectilesImproved.Effects.Flight;
using ProjectilesImproved.Projectiles;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using System.Text;
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
    public class LargeTurret : Weapons
    {
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallGatlingGun), false)]
    public class SmallGatling : Weapons
    {
    }

    public class Weapons : MyGameLogicComponent
    {
        public const float MillisecondPerFrame = 1000f / 60f;
        public const double FireRateMultiplayer = 1d / 60d / 60d;

        public bool ControlsUpdated = false;

        public IMyGunObject<MyGunBase> gun { get; private set; } = null;
        public bool IsShooting => gun.IsShooting || terminalShooting || (IsFixedGun && (Entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) == MyEntityUpdateEnum.EACH_FRAME);

        private MyWeaponDefinition weapon = null;
        private IMyFunctionalBlock block = null;
        private IMyCubeBlock cube = null;
        private MyEntity3DSoundEmitter soundEmitter;

        private bool IsFixedGun = false;
        private bool terminalShooting = false;

        private double timeTillNextShot = 1d;
        private int currentShotInBurst = 0;
        private float cooldownTime = 0;
        private int FirstTimeCooldown = 0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            block = Entity as IMyFunctionalBlock;
            cube = Entity as IMyCubeBlock;
            gun = Entity as IMyGunObject<MyGunBase>;
            IsFixedGun = Entity is IMySmallGatlingGun;
            FirstTimeCooldown = 10;

            soundEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity, true, 1f);

            if (!Core.IsInitialized)
            {
                Core.OnLoadComplete += Init;
            }
            else
            {
                OverrideDefaultControls();
                getWeaponDef();
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        private void Init()
        {
            Core.OnLoadComplete -= Init;
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

            foreach (IMyTerminalAction a in actions)
            {
                if (a.Id == "Shoot")
                {
                    a.Action = (block) =>
                    {
                        try
                        {
                            Weapons weps = block.GameLogic as Weapons;
                            MyAPIGateway.Utilities.ShowNotification($"shoot action", 500);
                            weps.terminalShooting = !weps.terminalShooting;
                        }
                        catch
                        {
                            MyLog.Default.Warning("Failed in the Shoot!");
                        }
                    };

                    a.Writer = WeaponsFiringWriter;
                }
                else if (a.Id == "ShootOnce")
                {
                    a.Action = (block) =>
                    {
                        try
                        {
                            Weapons weapon = (block.GameLogic as Weapons);
                            if (weapon.cooldownTime == 0 && weapon.timeTillNextShot >= 1)
                            {
                                weapon.FireWeapon();
                            }
                        }
                        catch
                        {
                            MyLog.Default.Warning("Failed in the Shoot Once!");
                        }
                    };

                }
                if (a.Id == "Shoot_On")
                {
                    a.Action = (block) =>
                    {
                        try
                        {
                            (block.GameLogic as Weapons).terminalShooting = true;
                        }
                        catch
                        {
                            MyLog.Default.Warning("Failed in the Shoot_On!");
                        }
                    };

                    a.Writer = WeaponsFiringWriter;
                }
                else if (a.Id == "Shoot_Off")
                {
                    a.Action = (block) =>
                    {
                        try
                        {
                            MyAPIGateway.Utilities.ShowNotification($"Shoot off {block.GameLogic is Weapons}", 500);
                            (block.GameLogic as Weapons).terminalShooting = false;
                        }
                        catch
                        {
                            MyLog.Default.Warning("Failed in the Shoot_Off!");
                        }
                    };

                    a.Writer = WeaponsFiringWriter;
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

            foreach (IMyTerminalControl c in controls)
            {
                if (c.Id == "Shoot")
                {
                    IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;

                    onoff.Setter = (block, value) =>
                    {
                        try
                        {
                            (block.GameLogic as Weapons).terminalShooting = value;
                        }
                        catch
                        {
                            MyLog.Default.Warning("Failed in the terminal Shoot_On!");
                        }
                    };

                    onoff.Getter = (block) =>
                    {
                        try
                        {
                            return (block.GameLogic as Weapons).terminalShooting;
                        }
                        catch
                        {
                            MyLog.Default.Warning("Failed in the terminal Shoot_off!");
                            return false;
                        }
                    };
                }
            }
        }

        private void WeaponsFiringWriter(IMyTerminalBlock block, StringBuilder str)
        {
            if ((block.GameLogic as Weapons).terminalShooting)
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
            bool shouldReturn = false;

            if (timeTillNextShot < 1)
            {
                timeTillNextShot += weapon.WeaponAmmoDatas[GetAmmoLookup()].RateOfFire * FireRateMultiplayer;
            }

            if (FirstTimeCooldown > 0)
            {
                FirstTimeCooldown--;
                shouldReturn = true;
            }

            if (cooldownTime > 0)
            {
                cooldownTime -= MillisecondPerFrame;
                shouldReturn = true;
            }

            if (!cube.IsFunctional)
            {
                terminalShooting = false;
                shouldReturn = true;
            }

            if (gun.IsShooting)
            {
                terminalShooting = false;
            }

            if (!IsShooting ||
                cube?.CubeGrid?.Physics == null ||
                !gun.GunBase.HasEnoughAmmunition() ||
                shouldReturn)
            {
                return;
            }

            FireWeapon();
        }

        private void FireWeapon()
        {
            if (timeTillNextShot >= 1)
            {
                MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();
                Vector3D bonus = (block.CubeGrid.Physics.LinearVelocity * Tools.Tick);

                bonus.Rotate(muzzleMatrix);
                muzzleMatrix.Translation += bonus;

                MatrixD positionMatrix = Matrix.CreateWorld(
                    muzzleMatrix.Translation,
                    gun.GunBase.GetDeviatedVector(gun.GunBase.DeviateAngle, muzzleMatrix.Forward),
                    muzzleMatrix.Up);

                CollisionEffect collisionEffect = Settings.GetAmmoEffect(gun.GunBase.CurrentAmmoDefinition.Id.ToString());

                IFlight flightEffect = new BulletDrop();

                Bullet fireData = new Bullet
                {
                    GridId = cube.CubeGrid.EntityId,
                    BlockId = Entity.EntityId,
                    WeaponId = weapon.Id,
                    MagazineId = gun.GunBase.CurrentAmmoMagazineId,
                    AmmoId = gun.GunBase.CurrentAmmoDefinition.Id,
                    InitialGridVelocity = block.CubeGrid.Physics.LinearVelocity,
                    Velocity = block.CubeGrid.Physics.LinearVelocity + (positionMatrix.Forward * gun.GunBase.CurrentAmmoDefinition.DesiredSpeed),
                    PositionMatrix = positionMatrix,
                    CollisionEffect = collisionEffect,
                    FlightEffect = flightEffect
                };

                while (timeTillNextShot >= 1)
                {
                    Core.SpawnProjectile(fireData);
                    gun.GunBase.ConsumeAmmo();
                    timeTillNextShot--;

                    soundEmitter.PlaySound(gun.GunBase.ShootSound, false, false, false, false, false, null);

                    currentShotInBurst++;
                    if (currentShotInBurst == gun.GunBase.ShotsInBurst)
                    {
                        timeTillNextShot = 0;
                        currentShotInBurst = 0;
                        cooldownTime = weapon.ReloadTime;
                        break;
                    }

                    //positionMatrix.Translation += positionMatrix.Forward * (timeTillNextShot * 0.03);
                }

                var forceVector = -positionMatrix.Forward * gun.GunBase.CurrentAmmoDefinition.BackkickForce;
                block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, forceVector, block.WorldAABB.Center, null);
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
