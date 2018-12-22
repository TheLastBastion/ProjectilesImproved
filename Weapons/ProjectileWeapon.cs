using ProjectilesImproved.Definitions;
using ProjectilesImproved.Effects.Weapon;
using ProjectilesImproved.Projectiles;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
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
    public class LargeTurret : ProjectileWeapon
    {
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallGatlingGun), false)]
    public class SmallGatling : ProjectileWeapon
    {
    }

    public class ProjectileWeapon : MyGameLogicComponent
    {

        public static DefaultWeaponEffect DefaultEffect = new DefaultWeaponEffect();

        public bool ControlsUpdated = false; // TODO: change this to static?

        public bool IsShooting => gun.IsShooting || TerminalShooting || (IsFixedGun && (Entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) == MyEntityUpdateEnum.EACH_FRAME);

        public  double TimeTillNextShot = 1d;
        public int CurrentShotInBurst = 0;
        public float CooldownTime = 0;
        public int FirstTimeCooldown = 0;

        public bool IsFixedGun = false;
        public bool TerminalShooting = false;

        public IMyFunctionalBlock Block;
        public IMyCubeBlock Cube;
        public IMyGunObject<MyGunBase> gun;

        private MyWeaponDefinition Weapon;
        private MyAmmoDefinition Ammo;
        public WeaponDefinition Definition;

        private MyEntity3DSoundEmitter soundEmitter;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Block = Entity as IMyFunctionalBlock;
            Cube = Entity as IMyCubeBlock;
            gun = Entity as IMyGunObject<MyGunBase>;
            IsFixedGun = Entity is IMySmallGatlingGun;
            FirstTimeCooldown = 10;

            Ammo = gun.GunBase.CurrentAmmoDefinition;

            soundEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity, true, 1f);

            if (!Core.IsInitialized)
            {
                Core.OnLoadComplete += LoadComplete;
            }
            else
            {
                OverrideDefaultControls();
                GetWeaponDef();

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        private void LoadComplete()
        {
            Core.OnLoadComplete -= LoadComplete;
            OverrideDefaultControls();
            GetWeaponDef();

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        private void GetWeaponDef()
        {
            if (gun != null)
            {
                Weapon = MyDefinitionManager.Static.GetWeaponDefinition((Block.SlimBlock.BlockDefinition as MyWeaponBlockDefinition).WeaponDefinitionId);

                Definition = Settings.GetWeaponDefinition(Weapon.Id.SubtypeId.String);
                GetMoreWeaponDef();

                // Thanks for the help Digi
                for (int i = 0; i < Weapon.WeaponAmmoDatas.Length; i++)
                {
                    var ammoData = Weapon.WeaponAmmoDatas[i];

                    if (ammoData == null)
                        continue;

                    ammoData.ShootIntervalInMiliseconds = int.MaxValue;
                }
            }
        }

        private void GetMoreWeaponDef()
        {
            MyWeaponDefinition.MyWeaponAmmoData moreDetails = Weapon.WeaponAmmoDatas[GetAmmoLookup()];

            Definition.RateOfFire = moreDetails.RateOfFire;
            Definition.ShotsInBurst = moreDetails.ShotsInBurst;
            Definition.ShootSound = moreDetails.ShootSound;
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
                            ProjectileWeapon weps = block.GameLogic as ProjectileWeapon;
                            MyAPIGateway.Utilities.ShowNotification($"shoot action", 500);
                            weps.TerminalShooting = !weps.TerminalShooting;
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
                            ProjectileWeapon weapon = (block.GameLogic as ProjectileWeapon);
                            if (weapon.CooldownTime == 0 && weapon.TimeTillNextShot >= 1)
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
                            (block.GameLogic as ProjectileWeapon).TerminalShooting = true;
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
                            MyAPIGateway.Utilities.ShowNotification($"Shoot off {block.GameLogic is ProjectileWeapon}", 500);
                            (block.GameLogic as ProjectileWeapon).TerminalShooting = false;
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
                            (block.GameLogic as ProjectileWeapon).TerminalShooting = value;
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
                            return (block.GameLogic as ProjectileWeapon).TerminalShooting;
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
            if ((block.GameLogic as ProjectileWeapon).TerminalShooting)
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
            if (Ammo != gun.GunBase.CurrentAmmoDefinition)
            {
                Ammo = gun.GunBase.CurrentAmmoDefinition;
                GetMoreWeaponDef();
            }

            if (FirstTimeCooldown > 0)
            {
                FirstTimeCooldown--;
                return;
            }

            if (Definition.Ramping != null)
            {
                if (Definition.Ramping.Update(this))
                {
                    FireWeapon();
                }
            }
            else if (DefaultEffect.Update(this))
            {
                FireWeapon();
            }
            
        }

        private void FireWeapon()
        {
            if (TimeTillNextShot >= 1)
            {
                MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();
                Vector3D bonus = (Block.CubeGrid.Physics.LinearVelocity * Tools.Tick);

                bonus.Rotate(muzzleMatrix);
                muzzleMatrix.Translation += bonus;

                ProjectileDefinition bulletData = Settings.GetAmmoDefinition(gun.GunBase.CurrentAmmoDefinition.Id.SubtypeId.String);

                while (TimeTillNextShot >= 1)
                {
                    MatrixD positionMatrix = Matrix.CreateWorld(
                        muzzleMatrix.Translation,
                        gun.GunBase.GetDeviatedVector(Definition.DeviateShotAngle, muzzleMatrix.Forward),
                        muzzleMatrix.Up);

                    Projectile bullet = bulletData.CreateProjectile();
                    bullet.InitialGridVelocity = Block.CubeGrid.Physics.LinearVelocity;
                    bullet.Velocity = Block.CubeGrid.Physics.LinearVelocity + (positionMatrix.Forward * bulletData.DesiredSpeed);
                    bullet.PositionMatrix = positionMatrix;

                    Core.SpawnProjectile(bullet);
                    gun.GunBase.ConsumeAmmo();
                    TimeTillNextShot--;

                    soundEmitter.PlaySound(gun.GunBase.ShootSound, false, false, false, false, false, null);

                    CurrentShotInBurst++;
                    if (CurrentShotInBurst == Definition.ShotsInBurst)
                    {
                        TimeTillNextShot = 0;
                        CurrentShotInBurst = 0;
                        CooldownTime = Definition.ReloadTime;
                        break;
                    }

                    var forceVector = -positionMatrix.Forward * bulletData.BackkickForce;
                    Block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, forceVector, Block.WorldAABB.Center, null);
                }
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
