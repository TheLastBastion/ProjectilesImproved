using ProjectilesImproved.Bullets;
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

    public class WeaponsBase : MyGameLogicComponent
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

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            block = Entity as IMyFunctionalBlock;
            cube = Entity as IMyCubeBlock;
            gun = Entity as IMyGunObject<MyGunBase>;
            IsFixedGun = Entity is IMySmallGatlingGun;

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
                        WeaponsBase weps = block.GameLogic as WeaponsBase;
                        MyAPIGateway.Utilities.ShowNotification($"shoot", 500);
                        weps.terminalShooting = !weps.terminalShooting;
                    };

                    a.Writer = WeaponsFiringWriter;
                }
                else if (a.Id == "ShootOnce")
                {
                    a.Action = (block) =>
                    {
                        WeaponsBase weapon = (block.GameLogic as WeaponsBase);
                        if (weapon.cooldownTime == 0 && weapon.timeTillNextShot >= 1)
                        {
                            FireWeapon();
                        }
                    };

                }
                if (a.Id == "Shoot_On")
                {
                    a.Action = (block) =>
                    {
                        (block.GameLogic as WeaponsBase).terminalShooting = true;
                    };

                    a.Writer = WeaponsFiringWriter;
                }
                else if (a.Id == "Shoot_Off")
                {
                    a.Action = (block) =>
                    {
                        MyAPIGateway.Utilities.ShowNotification($"Shoot off {block.GameLogic is WeaponsBase}", 500);
                        (block.GameLogic as WeaponsBase).terminalShooting = false;
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
                        (block.GameLogic as WeaponsBase).terminalShooting = value;
                    };

                    onoff.Getter = (block) =>
                    {
                        return (block.GameLogic as WeaponsBase).terminalShooting;
                    };
                }
            }
        }

        private void WeaponsFiringWriter(IMyTerminalBlock block, StringBuilder str)
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

        public override void UpdateAfterSimulation()
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

            FireWeapon();
        }

        private void FireWeapon()
        {
            if (timeTillNextShot >= 1)
            {
                MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();

                MatrixD positionMatrix = Matrix.CreateWorld(
                    muzzleMatrix.Translation + (block.CubeGrid.Physics.LinearVelocity * Tools.Tick),// + (block.CubeGrid.Physics.LinearAcceleration * Tools.Tick),
                    gun.GunBase.GetDeviatedVector(gun.GunBase.DeviateAngle, muzzleMatrix.Forward),
                    muzzleMatrix.Up);

                BulletDrop fireData = new BulletDrop()
                {
                    GridId = cube.CubeGrid.EntityId,
                    BlockId = Entity.EntityId,
                    WeaponId = weapon.Id,
                    MagazineId = gun.GunBase.CurrentAmmoMagazineId,
                    AmmoId = gun.GunBase.CurrentAmmoDefinition.Id,
                    InitialGridVelocity = block.CubeGrid.Physics.LinearVelocity,
                    Velocity = block.CubeGrid.Physics.LinearVelocity + (positionMatrix.Forward * gun.GunBase.CurrentAmmoDefinition.DesiredSpeed),
                    PositionMatrix = positionMatrix
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

                    positionMatrix.Translation += positionMatrix.Forward * (timeTillNextShot * 0.03); // using timeTillNextShot to offset bullets being fired in the same frame
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
