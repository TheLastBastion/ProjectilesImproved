using ProjectilesImproved.Effects.Collision;
using ProjectilesImproved.Effects.Flight;
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
    public class LargeTurret : ProjectileWeapons
    {
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallGatlingGun), false)]
    public class SmallGatling : ProjectileWeapons
    {
    }

    public class ProjectileWeapons : MyGameLogicComponent
    {
        public const float MillisecondPerFrame = 1000f / 60f;
        public const double FireRateMultiplayer = 1d / 60d / 60d;

        public bool ControlsUpdated = false;

        public IMyGunObject<MyGunBase> gun { get; private set; } = null;
        public bool IsShooting => gun.IsShooting || TerminalShooting || (IsFixedGun && (Entity.NeedsUpdate & MyEntityUpdateEnum.EACH_FRAME) == MyEntityUpdateEnum.EACH_FRAME);

        public double TimeTillNextShot { get; set; } = 1d;
        public int CurrentShotInBurst { get; set; } = 0;
        public float CooldownTime { get; set; } = 0;

        public MyDefinitionId Id { get; private set; }
        public int ReloadTime { get; set; }
        public float DeviateShotAngle { get; set; }
        public float ReleaseTimeAfterFire { get; set; }
        public int MuzzleFlashLifeSpan { get; set; }
        public float DamageMultiplier { get; set; }

        public int RateOfFire { get; set; }
        public int ShotsInBurst { get; set; }
        public MySoundPair ShootSound { get; set; }

        public IWeapon WeaponEffect { get; private set; }

        public MyWeaponDefinition Weapon { get; private set; } = null;
        public IMyFunctionalBlock Block { get; private set; } = null;
        public IMyCubeBlock Cube { get; private set; } = null;
        public MyAmmoDefinition Ammo { get; private set; }

        public bool IsFixedGun { get; private set; } = false;
        public bool TerminalShooting { get; set; } = false;

        private MyEntity3DSoundEmitter soundEmitter;
        private int FirstTimeCooldown = 0;

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
                Core.OnLoadComplete += Init;
            }
            else
            {
                OverrideDefaultControls();
                getWeaponDef();
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }

            MyLog.Default.Info($"{objectBuilder.TypeId}/{objectBuilder.SubtypeName}");
        }

        private void Init()
        {
            Core.OnLoadComplete -= Init;
            OverrideDefaultControls();
            getWeaponDef();
            WeaponEffect = new WeaponEffect();

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        private void getWeaponDef()
        {
            if (gun != null)
            {
                Weapon = MyDefinitionManager.Static.GetWeaponDefinition((Block.SlimBlock.BlockDefinition as MyWeaponBlockDefinition).WeaponDefinitionId);

                Id = Weapon.Id;
                ReloadTime = Weapon.ReloadTime;
                DeviateShotAngle = Weapon.DeviateShotAngle;
                ReleaseTimeAfterFire = Weapon.ReleaseTimeAfterFire;
                MuzzleFlashLifeSpan = Weapon.MuzzleFlashLifeSpan;
                DamageMultiplier = Weapon.DamageMultiplier;

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

            RateOfFire = moreDetails.RateOfFire;
            ShotsInBurst = moreDetails.ShotsInBurst;
            ShootSound = moreDetails.ShootSound;
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
                            ProjectileWeapons weps = block.GameLogic as ProjectileWeapons;
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
                            ProjectileWeapons weapon = (block.GameLogic as ProjectileWeapons);
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
                            (block.GameLogic as ProjectileWeapons).TerminalShooting = true;
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
                            MyAPIGateway.Utilities.ShowNotification($"Shoot off {block.GameLogic is ProjectileWeapons}", 500);
                            (block.GameLogic as ProjectileWeapons).TerminalShooting = false;
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
                            (block.GameLogic as ProjectileWeapons).TerminalShooting = value;
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
                            return (block.GameLogic as ProjectileWeapons).TerminalShooting;
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
            if ((block.GameLogic as ProjectileWeapons).TerminalShooting)
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

            if (WeaponEffect.Update(this))
            {
                FireWeapon();
            }

            //if (TimeTillNextShot < 1)
            //{            
            //    TimeTillNextShot += RateOfFire * FireRateMultiplayer;
            //}

            //if (FirstTimeCooldown > 0)
            //{
            //    FirstTimeCooldown--;
            //    shouldReturn = true;
            //}

            //if (CooldownTime > 0)
            //{
            //    CooldownTime -= MillisecondPerFrame;
            //    shouldReturn = true;
            //}

            //if (!cube.IsFunctional)
            //{
            //    terminalShooting = false;
            //    shouldReturn = true;
            //}

            //if (gun.IsShooting)
            //{
            //    terminalShooting = false;
            //}

            //if (!IsShooting ||
            //    cube?.CubeGrid?.Physics == null ||
            //    !gun.GunBase.HasEnoughAmmunition() ||
            //    shouldReturn)
            //{
            //    return;
            //}

            //
        }

        private void FireWeapon()
        {
            if (TimeTillNextShot >= 1)
            {
                MatrixD muzzleMatrix = gun.GunBase.GetMuzzleWorldMatrix();
                Vector3D bonus = (Block.CubeGrid.Physics.LinearVelocity * Tools.Tick);

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
                    GridId = Cube.CubeGrid.EntityId,
                    BlockId = Entity.EntityId,
                    WeaponId = Id,
                    MagazineId = gun.GunBase.CurrentAmmoMagazineId,
                    AmmoId = gun.GunBase.CurrentAmmoDefinition.Id,
                    InitialGridVelocity = Block.CubeGrid.Physics.LinearVelocity,
                    Velocity = Block.CubeGrid.Physics.LinearVelocity + (positionMatrix.Forward * gun.GunBase.CurrentAmmoDefinition.DesiredSpeed),
                    PositionMatrix = positionMatrix,
                    CollisionEffect = collisionEffect,
                    FlightEffect = flightEffect
                };

                while (TimeTillNextShot >= 1)
                {
                    Core.SpawnProjectile(fireData);
                    gun.GunBase.ConsumeAmmo();
                    TimeTillNextShot--;

                    soundEmitter.PlaySound(gun.GunBase.ShootSound, false, false, false, false, false, null);

                    CurrentShotInBurst++;
                    if (CurrentShotInBurst == gun.GunBase.ShotsInBurst)
                    {
                        TimeTillNextShot = 0;
                        CurrentShotInBurst = 0;
                        CooldownTime = ReloadTime;
                        break;
                    }

                    //positionMatrix.Translation += positionMatrix.Forward * (timeTillNextShot * 0.03);
                }

                var forceVector = -positionMatrix.Forward * gun.GunBase.CurrentAmmoDefinition.BackkickForce;
                Block.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, forceVector, Block.WorldAABB.Center, null);
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
