using ModNetworkAPI;
using ProjectilesImproved.Definitions;
using ProjectilesImproved.Projectiles;
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
using VRage.Utils;
using VRageMath;

namespace ProjectilesImproved.Weapons
{

    public class WeaponBasic : WeaponDefinition, IWeapon
    {
        public enum TerminalState { None, Shoot_On, Shoot_Off, ShootOnce }

        public bool IsFixedGun { get; private set; }
        public bool IsShooting => gun.IsShooting || TerminalShootOnce || TerminalShooting || (IsFixedGun && (Entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) == MyEntityUpdateEnum.EACH_FRAME);
        public bool IsInitialized { get; private set; }

        protected int AmmoType => (gun.GunBase.CurrentAmmoDefinition != null && gun.GunBase.CurrentAmmoDefinition.AmmoType != MyAmmoType.Unknown) ? (int)gun.GunBase.CurrentAmmoDefinition.AmmoType : 0;
        protected MyAmmoDefinition Ammo => gun.GunBase.CurrentAmmoDefinition;

        public bool TerminalShooting;
        public bool TerminalShootOnce;
        protected bool ControlsUpdated;
        protected bool WillFireThisFrame;

        protected int CurrentShotInBurst = 0;
        protected int FirstTimeCooldown = 0;
        protected int LastNoAmmoSound = 0;
        protected float CurrentReloadTime = 0;
        protected float CurrentReleaseTime = 0;
        protected double TimeTillNextShot = 1d;
        protected float CurrentIdleReloadTime = 0;

        protected MyEntity Entity;
        protected IMyFunctionalBlock Block;
        protected IMyCubeBlock Cube;
        protected IMyGunObject<MyGunBase> gun;

        protected MyEntity3DSoundEmitter soundEmitter;
        protected MyEntity3DSoundEmitter secondarySoundEmitter;

        private Vector3 originalBarrelPostion = Vector3.Zero;
        private MyEntitySubpart barrelSubpart = null;

        public static bool SyncWeapon(WeaponSync data)
        {
            IMyTerminalBlock block = (IMyTerminalBlock)MyAPIGateway.Entities.GetEntityById(data.BlockId);

            if (block == null)
            {
                //MyLog.Default.Warning("Failed to find block in entities");
                return false;
            }

            WeaponControlLayer controlLayer = block.GameLogic.GetAs<WeaponControlLayer>(); // (block.GameLogic as WeaponControlLayer);
            if (controlLayer == null)
            {
                MyLog.Default.Warning($"Failed set weapon to {data.State.ToString()}. Block was of type {block.GameLogic.GetType()} not ProjectileWeapon.");
                return false;
            }

            WeaponBasic weapon = controlLayer.Weapon as WeaponBasic;

            if (data.State == TerminalState.None)
            {
                // do nothing
            }
            else if (data.State == TerminalState.ShootOnce)
            {
                weapon.TerminalShootOnce = true;
            }
            else
            {
                weapon.TerminalShooting = data.State == TerminalState.Shoot_On;
            }

            if (MyAPIGateway.Session.IsServer)
            {
                data.DeviationIndex = weapon.Randomizer.Index;
                data.CurrentReloadTime = weapon.CurrentReloadTime;
                data.CurrentIdleReloadTime = weapon.CurrentIdleReloadTime;
                data.CurrentReleaseTime = weapon.CurrentReleaseTime;
                data.CurrentShotInBurst = weapon.CurrentShotInBurst;
                data.TimeTillNextShot = weapon.TimeTillNextShot;

                if (weapon is WeaponRamping)
                {
                    data.CurrentRampingTime = (weapon as WeaponRamping).CurrentRampingTime;
                }

                NetworkAPI.Instance.SendCommand("sync", data: MyAPIGateway.Utilities.SerializeToBinary(data));
            }
            else
            {
                weapon.Randomizer.Index = data.DeviationIndex;
                weapon.CurrentReloadTime = data.CurrentReloadTime;
                weapon.CurrentIdleReloadTime = data.CurrentIdleReloadTime;
                weapon.CurrentReleaseTime = data.CurrentReleaseTime;
                weapon.CurrentShotInBurst = data.CurrentShotInBurst;
                weapon.TimeTillNextShot = data.TimeTillNextShot;

                if (weapon is WeaponRamping)
                {
                    (weapon as WeaponRamping).CurrentRampingTime = data.CurrentRampingTime;
                }
            }

            //MyLog.Default.Info(MyAPIGateway.Utilities.SerializeToXML(data));

            return true;
        }

        public virtual void Init(MyEntity entity)
        {
            IsInitialized = true;
            Entity = entity;
            Block = Entity as IMyFunctionalBlock;
            Cube = Entity as IMyCubeBlock;
            gun = Entity as IMyGunObject<MyGunBase>;
            IsFixedGun = Entity is IMySmallGatlingGun;

            soundEmitter = new MyEntity3DSoundEmitter(Entity, true, 1f);
            secondarySoundEmitter = new MyEntity3DSoundEmitter(Entity, true, 1f);

            FirstTimeCooldown = 10;
        }

        public virtual void OnAddedToContainer()
        {

        }

        public virtual void OnAddedToScene()
        {
            OverrideDefaultControls();
            WeaponSync sync = new WeaponSync() { BlockId = Entity.EntityId };
            NetworkAPI.Instance.SendCommand("sync", data: MyAPIGateway.Utilities.SerializeToBinary(sync));

            InitializeBarrel();
        }

        public virtual void Close()
        {
            soundEmitter?.StopSound(true, true);
            secondarySoundEmitter?.StopSound(true, true);
        }

        public virtual void Update()
        {
            //if (!MyAPIGateway.Utilities.IsDedicated)
            //{
            //    MyAPIGateway.Utilities.ShowNotification($"{(IsShooting ? "Shooting" : "Idle")}, RoF: {AmmoDatas[0].RateOfFire}, Shots: {CurrentShotInBurst}/{AmmoDatas[0].ShotsInBurst}, {(CurrentReloadTime > 0 ? $"Cooldown {(ReloadTime - CurrentReloadTime).ToString("n0")}/{ReloadTime}, " : "")}release: {CurrentReleaseTime.ToString("n0")}/{ReleaseTimeAfterFire}, Time: {TimeTillNextShot.ToString("n2")}", 1);
            //}

            WillFireThisFrame = true;

            if (FirstTimeCooldown > 0)
            {
                FirstTimeCooldown--;
                WillFireThisFrame = false;
                TerminalShootOnce = false;
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

                //MyAPIGateway.Utilities.ShowNotification($"{!IsShooting}, {Cube?.CubeGrid?.Physics == null}, {gun.GunBase.HasEnoughAmmunition()} willFire: {WillFireThisFrame}, hasEnough:  Physical: ", 1);

                WillFireThisFrame = false;
            }

            if (WillFireThisFrame)
            {
                TimeTillNextShot += AmmoDatas[AmmoType].RateOfFire * Tools.FireRateMultiplayer;
            }
            else
            {
                StopShootingSound();
            }

            // trigger a no ammo sound every 60 seconds if out of ammo
            if (IsShooting)
            {
                if (!gun.GunBase.HasEnoughAmmunition() && LastNoAmmoSound == 0)
                {
                    MakeNoAmmoSound();
                    LastNoAmmoSound = 60;
                }

                if (LastNoAmmoSound > 0)
                {
                    LastNoAmmoSound--;
                }
            }

            IdleReload();
            TerminalShootOnce = false;
        }

        public virtual void Spawn()
        {
            if (TimeTillNextShot >= 1 && WillFireThisFrame)
            {
                MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();

                ProjectileDefinition bulletData = Settings.GetAmmoDefinition(gun.GunBase.CurrentAmmoDefinition.Id.SubtypeId.String);

                while (TimeTillNextShot >= 1)
                {
                    MatrixD positionMatrix = Matrix.CreateWorld(
                        muzzleMatrix.Translation,
                        Randomizer.ApplyDeviation(Entity, muzzleMatrix.Forward, DeviateShotAngle),
                        muzzleMatrix.Up);

                    Projectile bullet = bulletData.CreateProjectile();
                    bullet.ParentBlockId = Entity.EntityId;
                    bullet.PartentSlim = Cube.SlimBlock;
                    bullet.InitialGridVelocity = Block.CubeGrid.Physics.LinearVelocity;
                    bullet.Direction = positionMatrix.Forward;
                    bullet.Velocity = Block.CubeGrid.Physics.LinearVelocity + (positionMatrix.Forward * bulletData.DesiredSpeed);
                    bullet.Position = positionMatrix.Translation;

                    Core.SpawnProjectile(bullet);
                    gun.GunBase.ConsumeAmmo();
                    TimeTillNextShot--;
                    MakeShootSound();
                    MakeSecondaryShotSound();


                    CurrentShotInBurst++;
                    if (CurrentShotInBurst == AmmoDatas[AmmoType].ShotsInBurst)
                    {
                        TimeTillNextShot = 0;
                        CurrentShotInBurst = 0;
                        CurrentReloadTime = ReloadTime;
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

        public virtual void Draw()
        {
            //if (barrelSubpart == null) return;

            //double rotationAmount = 0.0002f * AmmoDatas[0].RateOfFire;
            //if (IsShooting)
            //{
            //    CurrentReleaseTime = 0;
            //}
            //else if (CurrentReleaseTime <= ReleaseTimeAfterFire)
            //{
            //    rotationAmount *= (1 - CurrentReleaseTime / ReleaseTimeAfterFire);

            //    CurrentReleaseTime += Tools.MillisecondPerFrame;

            //    if (CurrentReleaseTime >= ReleaseTimeAfterFire)
            //    {
            //        CurrentReleaseTime = ReleaseTimeAfterFire;
            //    }
            //}

            //if (rotationAmount == 0) return;

            //MatrixD rotation = MatrixD.CreateRotationZ(rotationAmount);

            //Matrix matrix = barrelSubpart.PositionComp.LocalMatrix;

            //matrix.Translation = new Vector3(originalBarrelPostion.X, originalBarrelPostion.Y, matrix.Translation.Z);

            //barrelSubpart.PositionComp.LocalMatrix = matrix * rotation;
        }

        public virtual void IdleReload()
        {
            if (!IsShooting && CurrentShotInBurst > 0)
            {
                if (CurrentIdleReloadTime >= ReloadTime)
                {
                    CurrentShotInBurst = 0;
                    CurrentIdleReloadTime = 0;
                }

                CurrentIdleReloadTime += Tools.MillisecondPerFrame;
            }
            else
            {
                CurrentIdleReloadTime = 0;
            }
        }

        protected void OverrideDefaultControls()
        {
            if (!WeaponControlLayer.DefaultTerminalControlsInitialized)
            {
                WeaponControlLayer.TerminalIntitalize();
            }

            if (WeaponControlLayer.IsThisBlockBlacklisted(Entity))
            {
                Entity.GameLogic.GetAs<WeaponControlLayer>().MarkForClose();
                ControlsUpdated = true;
                return;
            }

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
                            var logic = block.GameLogic.GetAs<WeaponControlLayer>();
                            if (logic != null)
                            {
                                WeaponBasic basic = logic.Weapon as WeaponBasic;
                                if (basic == null) return;

                                if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                                {
                                    basic.TerminalShooting = !basic.TerminalShooting;
                                }

                                NetworkAPI.Instance.SendCommand("sync", data: MyAPIGateway.Utilities.SerializeToBinary(new WeaponSync
                                {
                                    BlockId = block.EntityId,
                                    State = ((basic.TerminalShooting) ? TerminalState.Shoot_On : TerminalState.Shoot_Off)
                                }));
                            }
                            else
                            {
                                if (Entity is IMyLargeTurretBase)
                                {
                                    WeaponControlLayer.TerminalShootActionTurretBase.Invoke(block);
                                }
                                else
                                {
                                    WeaponControlLayer.TerminalShootActionGatlingGun.Invoke(block);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MyLog.Default.Warning($"Failed the shoot on/off action\n {e.ToString()}");
                        }
                    };

                    a.Writer = (block, text) =>
                        {
                            if (block.GameLogic.GetAs<WeaponControlLayer>() != null)
                            {
                                WeaponsFiringWriter(block, text);
                            }
                            else
                            {
                                if (Entity is IMyLargeTurretBase)
                                {
                                    WeaponControlLayer.TerminalShootWriterTurretBase(block, text);
                                }
                                else
                                {
                                    WeaponControlLayer.TerminalShootWriterGatlingGun(block, text);
                                }
                            }
                        };
                }
                else if (a.Id == "ShootOnce")
                {
                    a.Action = (block) =>
                    {
                        try
                        {
                            var logic = block.GameLogic.GetAs<WeaponControlLayer>();
                            if (logic != null)
                            {
                                WeaponBasic basic = logic.Weapon as WeaponBasic;

                                if (basic != null && !basic.TerminalShootOnce)
                                {
                                    if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                                    {
                                        basic.TerminalShootOnce = true;
                                    }

                                    NetworkAPI.Instance.SendCommand("sync", data: MyAPIGateway.Utilities.SerializeToBinary(new WeaponSync
                                    {
                                        BlockId = block.EntityId,
                                        State = TerminalState.ShootOnce
                                    }));
                                }
                            }
                            else
                            {
                                if (Entity is IMyLargeTurretBase)
                                {
                                    WeaponControlLayer.TerminalShootOnceActionTurretBase.Invoke(block);
                                }
                                else
                                {
                                    WeaponControlLayer.TerminalShootOnceActionGatlingGun.Invoke(block);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MyLog.Default.Warning($"Failed the shoot once action\n {e.ToString()}");
                        }
                    };
                }
                if (a.Id == "Shoot_On")
                {
                    a.Action = (block) =>
                    {
                        try
                        {
                            var logic = block.GameLogic.GetAs<WeaponControlLayer>();
                            if (logic != null)
                            {
                                WeaponBasic basic = logic.Weapon as WeaponBasic;

                                if (basic != null && !basic.TerminalShooting)
                                {
                                    if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                                    {
                                        basic.TerminalShooting = true;
                                    }

                                    NetworkAPI.Instance.SendCommand("sync", data: MyAPIGateway.Utilities.SerializeToBinary(new WeaponSync
                                    {
                                        BlockId = block.EntityId,
                                        State = TerminalState.Shoot_On
                                    }));
                                }
                            }
                            else
                            {
                                if (Entity is IMyLargeTurretBase)
                                {
                                    WeaponControlLayer.TerminalShootOnActionTurretBase.Invoke(block);
                                }
                                else
                                {
                                    WeaponControlLayer.TerminalShootOnActionGatlingGun.Invoke(block);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MyLog.Default.Warning($"Failed the shoot on action\n {e.ToString()}");
                        }
                    };

                    a.Writer = (block, text) =>
                    {
                        if (block.GameLogic.GetAs<WeaponControlLayer>() != null)
                        {
                            WeaponsFiringWriter(block, text);
                        }
                        else
                        {
                            if (Entity is IMyLargeTurretBase)
                            {
                                WeaponControlLayer.TerminalShootOnWriterTurretBase(block, text);
                            }
                            else
                            {
                                WeaponControlLayer.TerminalShootOnWriterGatlingGun(block, text);
                            }
                        }
                    };

                }
                else if (a.Id == "Shoot_Off")
                {
                    a.Action = (block) =>
                    {
                        try
                        {
                            var logic = block.GameLogic.GetAs<WeaponControlLayer>();
                            if (logic != null)
                            {
                                WeaponBasic basic = logic.Weapon as WeaponBasic;

                                if (basic != null && basic.TerminalShooting)
                                {
                                    if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                                    {
                                        basic.TerminalShooting = false;
                                    }

                                    NetworkAPI.Instance.SendCommand("sync", data: MyAPIGateway.Utilities.SerializeToBinary(new WeaponSync
                                    {
                                        BlockId = block.EntityId,
                                        State = TerminalState.Shoot_Off
                                    }));
                                }
                            }
                            else
                            {
                                if (Entity is IMyLargeTurretBase)
                                {
                                    WeaponControlLayer.TerminalShootOffActionTurretBase.Invoke(block);
                                }
                                else
                                {
                                    WeaponControlLayer.TerminalShootOffActionGatlingGun.Invoke(block);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MyLog.Default.Warning($"Failed the shoot off action\n {e.ToString()}");
                        }
                    };

                    a.Writer = (block, text) =>
                    {
                        if (block.GameLogic.GetAs<WeaponControlLayer>() != null)
                        {
                            WeaponsFiringWriter(block, text);
                        }
                        else
                        {
                            if (Entity is IMyLargeTurretBase)
                            {
                                WeaponControlLayer.TerminalShootOffWriterTurretBase(block, text);
                            }
                            else
                            {
                                WeaponControlLayer.TerminalShootOffWriterGatlingGun(block, text);
                            }
                        }
                    };
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
                            var logic = block.GameLogic.GetAs<WeaponControlLayer>();
                            if (logic != null)
                            {
                                WeaponBasic basic = logic.Weapon as WeaponBasic;

                                if (basic != null && basic.TerminalShooting != value)
                                {
                                    if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                                    {
                                        basic.TerminalShooting = value;
                                    }

                                    NetworkAPI.Instance.SendCommand("sync", data: MyAPIGateway.Utilities.SerializeToBinary(new WeaponSync
                                    {
                                        BlockId = block.EntityId,
                                        State = (value) ? TerminalState.Shoot_On : TerminalState.Shoot_Off
                                    }));
                                }
                            }
                            else
                            {
                                if (Entity is IMyLargeTurretBase)
                                {
                                    WeaponControlLayer.TerminalShootSetterTurretBase.Invoke(block, value);
                                }
                                else
                                {
                                    WeaponControlLayer.TerminalShootSetterGatlingGun.Invoke(block, value);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MyLog.Default.Warning($"Failed to toggle Shoot On/Off terminal control\n {e.ToString()}");
                        }
                    };

                    onoff.Getter = (block) =>
                    {
                        try
                        {
                            var logic = block.GameLogic.GetAs<WeaponControlLayer>();
                            if (logic != null)
                            {
                                return (logic.Weapon as WeaponBasic).TerminalShooting;
                            }
                            else
                            {
                                if (Entity is IMyLargeTurretBase)
                                {
                                    return WeaponControlLayer.TerminalShootGetterTurretBase.Invoke(block);
                                }
                                else
                                {
                                    return WeaponControlLayer.TerminalShootGetterGatlingGun.Invoke(block);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MyLog.Default.Warning($"Failed to get the Shoot On/Off terminal control\n {e.ToString()}");
                            return false;
                        }
                    };
                }
            }
        }

        protected void WeaponsFiringWriter(IMyTerminalBlock block, StringBuilder str)
        {
            if ((block.GameLogic.GetAs<WeaponControlLayer>()?.Weapon as WeaponBasic).TerminalShooting)
            {
                str.Append("On");
            }
            else
            {
                str.Append("Off");
            }
        }

        protected void InitializeBarrel()
        {
            //MyEntity ent = (MyEntity)Entity;

            //if (ent.Subparts.ContainsKey("GatlingTurretBase1"))
            //{
            //    if (ent.Subparts["GatlingTurretBase1"].Subparts.ContainsKey("GatlingTurretBase2"))
            //    {
            //        if (ent.Subparts["GatlingTurretBase1"].Subparts["GatlingTurretBase2"].Subparts.ContainsKey("GatlingBarrel"))
            //        {
            //            barrelSubpart = ent.Subparts["GatlingTurretBase1"].Subparts["GatlingTurretBase2"].Subparts["GatlingBarrel"];
            //        }
            //    }
            //}
            //else if (ent.Subparts.ContainsKey("InteriorTurretBase1"))
            //{
            //    if (ent.Subparts["InteriorTurretBase1"].Subparts.ContainsKey("InteriorTurretBase2"))
            //    {
            //        if (ent.Subparts["InteriorTurretBase1"].Subparts["InteriorTurretBase2"].Subparts.ContainsKey("Barrel"))
            //        {
            //            barrelSubpart = ent.Subparts["InteriorTurretBase1"].Subparts["InteriorTurretBase2"].Subparts["Barrel"];
            //        }
            //    }
            //}
            //else if (ent.Subparts.ContainsKey("Barrel"))
            //{
            //    barrelSubpart = ent.Subparts["Barrel"];
            //}

            //if (barrelSubpart != null)
            //{
            //    originalBarrelPostion = barrelSubpart.PositionComp.LocalMatrix.Translation;
            //}
        }

        protected void MakeShootSound()
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

        protected void MakeSecondaryShotSound()
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

        protected void StopShootingSound()
        {
            if (soundEmitter.Loop)
            {
                soundEmitter.StopSound(true, true);
            }

            if (secondarySoundEmitter.Loop)
            {
                secondarySoundEmitter.StopSound(true, true);
            }
        }

        protected void MakeNoAmmoSound()
        {
            if (gun.GunBase.NoAmmoSound != null)
            {
                soundEmitter.StopSound(true, true);
                soundEmitter.PlaySingleSound(gun.GunBase.NoAmmoSound, true, false, false, null);
            }
        }
    }
}
