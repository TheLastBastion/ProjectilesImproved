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

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_InteriorTurret), false)]
    public class InteriorTurret : ProjectileWeapon
    {
    }

    public class ProjectileWeapon : MyGameLogicComponent
    {

        public static DefaultWeaponEffect DefaultEffect = new DefaultWeaponEffect();

        public bool ControlsUpdated = false; // TODO: change this to static?
        private bool initialize = false;

        public bool IsShooting => gun.IsShooting || TerminalShootOnce || TerminalShooting || (IsFixedGun && (Entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) == MyEntityUpdateEnum.EACH_FRAME);
        public int AmmoType => (Ammo != null && Ammo.AmmoType != MyAmmoType.Unknown) ? (int)Ammo.AmmoType : 0;

        public double TimeTillNextShot = 1d;
        public int CurrentShotInBurst = 0;
        public float CooldownTime = 0;
        public int FirstTimeCooldown = 0;
        public int LastNoAmmoSound = 0;
        private float currentReleaseTime = 0;
        private int retry = 0;

        public bool IsFixedGun = false;
        public bool TerminalShooting = false;
        public bool TerminalShootOnce = false;

        public IMyFunctionalBlock Block = null;
        public IMyCubeBlock Cube = null;
        public IMyGunObject<MyGunBase> gun = null;

        private MyWeaponDefinition Weapon = null;
        private MyAmmoDefinition Ammo = null;
        public WeaponDefinition Definition = null;

        private MyEntity3DSoundEmitter soundEmitter = null;
        private MyEntity3DSoundEmitter secondarySoundEmitter = null;

        private Vector3 originalBarrelPostion = Vector3.Zero;
        private MyEntitySubpart barrelSubpart = null;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Block = Entity as IMyFunctionalBlock;
            Cube = Entity as IMyCubeBlock;
            gun = Entity as IMyGunObject<MyGunBase>;
            IsFixedGun = Entity is IMySmallGatlingGun;
            FirstTimeCooldown = 10;

            Ammo = gun.GunBase.CurrentAmmoDefinition;

            soundEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity, true, 1f);
            secondarySoundEmitter = new MyEntity3DSoundEmitter((MyEntity)Entity, true, 1f);

            Core.OnLoadComplete += InitAfterLoad;
            if (Core.IsInitialized)
            {
                InitAfterLoad();
            }
        }

        private void InitAfterLoad()
        {
            Core.OnLoadComplete -= InitAfterLoad;
            OverrideDefaultControls();
            GetWeaponDef();
            currentReleaseTime = Definition.ReleaseTimeAfterFire;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        private void GetWeaponDef()
        {
            if (gun != null)
            {
                Weapon = MyDefinitionManager.Static.GetWeaponDefinition((Block.SlimBlock.BlockDefinition as MyWeaponBlockDefinition).WeaponDefinitionId);
                Definition = Settings.GetWeaponDefinition(Weapon.Id.SubtypeId.String);

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
            //    MyAPIGateway.Utilities.ShowNotification($"Settings Loaded: {Settings.Instance.HasBeenSetByServer}, IsShooting: {IsShooting}, RoF: {Definition.AmmoDatas[0].RateOfFire}, Shots: {Definition.AmmoDatas[0].ShotsInBurst}, release: {currentReleaseTime}, barrel: {barrelSubpart != null} x: {originalBarrelPostion.X} y: {originalBarrelPostion.Y}, z: {originalBarrelPostion.Z}",1);
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

            if (!initialize && Settings.Instance.HasBeenSetByServer)
            {
                GetWeaponDef();
                currentReleaseTime = Definition.ReleaseTimeAfterFire;
                initialize = true;
                return;
            }


            if (Ammo != gun.GunBase.CurrentAmmoDefinition)
            {
                Ammo = gun.GunBase.CurrentAmmoDefinition;
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

            if (IsShooting)
            {
                if (!gun.GunBase.HasEnoughAmmunition() && LastNoAmmoSound == 0)
                {
                    MakeNoAmmoSound();
                    LastNoAmmoSound = 60;
                }
                LastNoAmmoSound--;
            }
            else
            {
                StopShootingSound();
            }

            RotateBarrel();

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
                    MakeShootSound();
                    MakeSecondaryShotSound();


                    CurrentShotInBurst++;
                    if (CurrentShotInBurst == Definition.AmmoDatas[AmmoType].ShotsInBurst)
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

                    if (gun.GunBase.HasEnoughAmmunition())
                    {
                        LastNoAmmoSound = 0;
                        break;
                    }
                }
            }
        }

        private void InitializeBarrel()
        {
            MyEntity ent = (MyEntity)Entity;

            if (ent.Subparts.ContainsKey("GatlingTurretBase1"))
            {
                if (ent.Subparts["GatlingTurretBase1"].Subparts.ContainsKey("GatlingTurretBase2"))
                {
                    if (ent.Subparts["GatlingTurretBase1"].Subparts["GatlingTurretBase2"].Subparts.ContainsKey("GatlingBarrel"))
                    {
                        barrelSubpart = ent.Subparts["GatlingTurretBase1"].Subparts["GatlingTurretBase2"].Subparts["GatlingBarrel"];
                    }
                }
            }
            else if (ent.Subparts.ContainsKey("InteriorTurretBase1"))
            {
                if (ent.Subparts["InteriorTurretBase1"].Subparts.ContainsKey("InteriorTurretBase2"))
                {
                    if (ent.Subparts["InteriorTurretBase1"].Subparts["InteriorTurretBase2"].Subparts.ContainsKey("Barrel"))
                    {
                        barrelSubpart = ent.Subparts["InteriorTurretBase1"].Subparts["InteriorTurretBase2"].Subparts["Barrel"];
                    }
                }
            }
            else if (ent.Subparts.ContainsKey("Barrel"))
            {
                barrelSubpart = ent.Subparts["Barrel"];
            }

            if (barrelSubpart != null)
            {
                originalBarrelPostion = barrelSubpart.PositionComp.LocalMatrix.Translation;
            }
        }

        private void RotateBarrel()
        {
            if (barrelSubpart == null && Settings.Instance.HasBeenSetByServer)
            {
                InitializeBarrel();
            }

            if (barrelSubpart != null)
            {
                double rotationAmount = 0.0002f * Definition.AmmoDatas[0].RateOfFire;
                if (IsShooting)
                {
                    currentReleaseTime = 0;
                }
                else if (currentReleaseTime <= Definition.ReleaseTimeAfterFire)
                {
                    rotationAmount *= (1 - currentReleaseTime / Definition.ReleaseTimeAfterFire);

                    currentReleaseTime += Tools.MillisecondPerFrame;

                    if (currentReleaseTime >= Definition.ReleaseTimeAfterFire)
                    {
                        currentReleaseTime = Definition.ReleaseTimeAfterFire;
                    }
                }

                if (rotationAmount == 0) return;

                MatrixD rotation = MatrixD.CreateRotationZ(rotationAmount);

                Matrix matrix = barrelSubpart.PositionComp.LocalMatrix;

                matrix.Translation = new Vector3(originalBarrelPostion.X, originalBarrelPostion.Y, matrix.Translation.Z);

                barrelSubpart.PositionComp.LocalMatrix = matrix * rotation;
            }
        }

        private void MakeShootSound()
        {
            if (gun.GunBase.ShootSound != null)
            {
                if (soundEmitter.IsPlaying)
                {
                    if (!soundEmitter.Loop)
                    {
                        soundEmitter.PlaySound(gun.GunBase.ShootSound, false, false, false, false, false, null);
                    }
                }
                else
                {
                    soundEmitter.PlaySound(gun.GunBase.ShootSound, true, false, false, false, false, null);
                }
            }
        }

        private void MakeSecondaryShotSound()
        {
            if (gun.GunBase.SecondarySound != null)
            {
                if (soundEmitter.IsPlaying)
                {
                    if (!soundEmitter.Loop)
                    {
                        secondarySoundEmitter.PlaySound(gun.GunBase.SecondarySound, false, false, false, false, false, null);
                    }
                }
                else
                {
                    secondarySoundEmitter.PlaySound(gun.GunBase.SecondarySound, true, false, false, false, false, null);
                }
            }
        }

        public void StopShootingSound()
        {
            if (soundEmitter.Loop)
            {
                soundEmitter.StopSound(false, true);
            }

            if (secondarySoundEmitter.Loop)
            {
                secondarySoundEmitter.StopSound(false, true);
            }
        }

        private void MakeNoAmmoSound()
        {
            if (gun.GunBase.NoAmmoSound != null)
            {
                soundEmitter.StopSound(true, true);
                soundEmitter.PlaySingleSound(gun.GunBase.NoAmmoSound, true, false, false, null);
            }
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
