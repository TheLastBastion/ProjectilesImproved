using ModNetworkAPI;
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

        public bool IsShooting => gun.IsShooting || TerminalShootOnce || TerminalShooting || (IsFixedGun && (Entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) == MyEntityUpdateEnum.EACH_FRAME);

        public double TimeTillNextShot = 1d;
        public int CurrentShotInBurst = 0;
        public float CooldownTime = 0;
        public int FirstTimeCooldown = 0;

        public bool IsFixedGun = false;
        public bool TerminalShooting = false;
        public bool TerminalShootOnce = false;

        public IMyFunctionalBlock Block;
        public IMyCubeBlock Cube;
        public IMyGunObject<MyGunBase> gun;

        private MyWeaponDefinition Weapon;
        private MyAmmoDefinition Ammo;
        public WeaponDefinition Definition;

        private MyEntity3DSoundEmitter soundEmitter;

        private int retry = 0;

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

        public static bool UpdateTerminalShooting(TerminalShoot t)
        {
            IMyTerminalBlock block = (IMyTerminalBlock)MyAPIGateway.Entities.GetEntityById(t.BlockId);

            if (block == null)
            {
                MyLog.Default.Warning("Failed to find block in entities");
                return false;
            }

            ProjectileWeapon weapon = (block.GameLogic as ProjectileWeapon);
            if (weapon == null)
            {
                MyLog.Default.Warning($"Failed set weapon to {t.State.ToString()}. Block was of type {block.GameLogic.GetType()} not ProjectileWeapon.");
                return false;
            }

            if (t.State == TerminalState.ShootOnce)
            {
                weapon.TerminalShootOnce = true;
            }
            else
            {
                weapon.TerminalShooting = t.State == TerminalState.Shoot_On;
            }

            return true;
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
                        ProjectileWeapon weapons = block.GameLogic as ProjectileWeapon;

                        if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                        {
                            weapons.TerminalShooting = !weapons.TerminalShooting;
                        }

                        NetworkAPI.Instance.SendCommand("shoot", data: MyAPIGateway.Utilities.SerializeToBinary(new TerminalShoot
                        {
                            BlockId = block.EntityId,
                            State = ((weapons.TerminalShooting) ? TerminalState.Shoot_Off : TerminalState.Shoot_On)
                        }));
                    };

                    a.Writer = WeaponsFiringWriter;
                }
                else if (a.Id == "ShootOnce")
                {
                    a.Action = (block) =>
                    {
                        ProjectileWeapon weapons = block.GameLogic as ProjectileWeapon;

                        if (weapons != null && !weapons.TerminalShootOnce)
                        {
                            if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                            {
                                weapons.TerminalShootOnce = true;
                            }

                            NetworkAPI.Instance.SendCommand("shoot", data: MyAPIGateway.Utilities.SerializeToBinary(new TerminalShoot
                            {
                                BlockId = block.EntityId,
                                State = TerminalState.ShootOnce
                            }));
                        }
                    };

                }
                if (a.Id == "Shoot_On")
                {
                    a.Action = (block) =>
                    {
                        ProjectileWeapon weapons = block.GameLogic as ProjectileWeapon;

                        if (weapons != null && !weapons.TerminalShooting)
                        {
                            if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                            {
                                weapons.TerminalShooting = true;
                            }

                            NetworkAPI.Instance.SendCommand("shoot", data: MyAPIGateway.Utilities.SerializeToBinary(new TerminalShoot
                            {
                                BlockId = block.EntityId,
                                State = TerminalState.Shoot_On
                            }));
                        }
                    };

                    a.Writer = WeaponsFiringWriter;
                }
                else if (a.Id == "Shoot_Off")
                {
                    a.Action = (block) =>
                    {
                        ProjectileWeapon weapons = block.GameLogic as ProjectileWeapon;

                        if (weapons != null && weapons.TerminalShooting)
                        {
                            if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                            {
                                weapons.TerminalShooting = false;
                            }

                            NetworkAPI.Instance.SendCommand("shoot", data: MyAPIGateway.Utilities.SerializeToBinary(new TerminalShoot
                            {
                                BlockId = block.EntityId,
                                State = TerminalState.Shoot_Off
                            }));
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
                        ProjectileWeapon weapons = block.GameLogic as ProjectileWeapon;

                        if (weapons != null && weapons.TerminalShooting != value)
                        {
                            if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                            {
                                weapons.TerminalShooting = value;
                            }

                            NetworkAPI.Instance.SendCommand("shoot", data: MyAPIGateway.Utilities.SerializeToBinary(new TerminalShoot
                            {
                                BlockId = block.EntityId,
                                State = (value) ? TerminalState.Shoot_On : TerminalState.Shoot_Off
                            }));
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
                            MyLog.Default.Warning($"Failed in the terminal Shoot_off! {block.GameLogic is ProjectileWeapon} {block.GameLogic.GetType()}");
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
            //if (!MyAPIGateway.Utilities.IsDedicated)
            //{
            //    MyAPIGateway.Utilities.ShowNotification($"{(gun.IsShooting ? "Turret Mouse Click" : "")} - {(TerminalShootOnce ? "Shoot Once" : "")} - {(TerminalShooting ? "Terminal Shooting" : "")} - {((IsFixedGun && (Entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) == MyEntityUpdateEnum.EACH_FRAME) ? "Fixed Gun Mouse Click" : "")}",1);
            //}

            // makes sure clients have gotten the update
            if (!Settings.Instance.HasBeenSetByServer)
            {
                if (retry == 0)
                {
                    NetworkAPI.Instance.SendCommand("update");
                    retry = 300;
                }

                retry--;
                return;
            }

            if (Ammo != gun.GunBase.CurrentAmmoDefinition)
            {
                Ammo = gun.GunBase.CurrentAmmoDefinition;
                GetMoreWeaponDef();
            }

            if (FirstTimeCooldown > 0)
            {
                FirstTimeCooldown--;
                TerminalShootOnce = false;
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

            TerminalShootOnce = false;
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
                    bullet.Position = positionMatrix.Translation;

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

                    if (TerminalShootOnce)
                    {
                        TerminalShootOnce = false;
                        return;
                    }
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
