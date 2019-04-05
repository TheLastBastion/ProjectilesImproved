using ProjectilesImproved.Definitions;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace ProjectilesImproved.Weapons
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeGatlingTurret), false)]
    public class LargeTurret2 : WeaponControlLayer
    {
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SmallGatlingGun), false)]
    public class SmallGatling2 : WeaponControlLayer
    {
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_InteriorTurret), false)]
    public class InteriorTurret2 : WeaponControlLayer
    {
    }

    public class WeaponControlLayer : MyGameLogicComponent
    {
        public static bool DefaultTerminalControlsInitialized = false;
        public static Action<IMyTerminalBlock> TerminalShootActionTurretBase;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootWriterTurretBase;
        public static Action<IMyTerminalBlock> TerminalShootOnceActionTurretBase;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootOnceWriterTurretBase;
        public static Action<IMyTerminalBlock> TerminalShootOnActionTurretBase;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootOnWriterTurretBase;
        public static Action<IMyTerminalBlock> TerminalShootOffActionTurretBase;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootOffWriterTurretBase;
        public static Action<IMyTerminalBlock, bool> TerminalShootSetterTurretBase;
        //public static Action<IMyTerminalBlock, StringBuilder> TerminalShootSetterWriterTurretBase;
        public static Func<IMyTerminalBlock, bool> TerminalShootGetterTurretBase;
        //public static Action<IMyTerminalBlock, StringBuilder> TerminalShootGetterWriterTurretBase;

        public static Action<IMyTerminalBlock> TerminalShootActionGatlingGun;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootWriterGatlingGun;
        public static Action<IMyTerminalBlock> TerminalShootOnceActionGatlingGun;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootOnceWriterGatlingGun;
        public static Action<IMyTerminalBlock> TerminalShootOnActionGatlingGun;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootOnWriterGatlingGun;
        public static Action<IMyTerminalBlock> TerminalShootOffActionGatlingGun;
        public static Action<IMyTerminalBlock, StringBuilder> TerminalShootOffWriterGatlingGun;
        public static Action<IMyTerminalBlock, bool> TerminalShootSetterGatlingGun;
        //public static Action<IMyTerminalBlock, StringBuilder> TerminalShootSetterWriterGatlingGun;
        public static Func<IMyTerminalBlock, bool> TerminalShootGetterGatlingGun;
        //public static Action<IMyTerminalBlock, StringBuilder> TerminalShootGetterWriterGatlingGun;

        public IWeapon Weapon = new WeaponBasic();

        public bool HasDefinition => (Entity as IMyFunctionalBlock).SlimBlock.BlockDefinition != null;

        private bool IsBlackListed = false;

        private bool SettingsJustUpdated;

        // this appears to fire before the init function so i am using it instead of init
        public override void OnAddedToContainer()
        {
            if (!Weapon.IsInitialized)
            {
                Weapon.Init((MyEntity)Entity);
                Core.OnSettingsUpdate -= OnSettingsUpdate;
                Core.OnSettingsUpdate += OnSettingsUpdate;
            }

            Weapon.OnAddedToContainer();

            if (Entity.InScene)
            {
                OnAddedToScene();
            }
        }

        public override void OnAddedToScene()
        {
            if (IsThisBlockBlacklisted(Entity))
            {
                this.MarkForClose();
                IsBlackListed = true;
                return;
            }

            DisableNormalWeaponsFire();
            Weapon.OnAddedToScene();

            if (Core.IsInitialized())
            {
                OnSettingsUpdate();
            }
        }

        public void OnSettingsUpdate()
        {
            if (SettingsJustUpdated)
            {
                SettingsJustUpdated = false;
                return;
            }

            SettingsJustUpdated = true;

            MyWeaponDefinition w = MyDefinitionManager.Static.GetWeaponDefinition(((Entity as IMyFunctionalBlock).SlimBlock.BlockDefinition as MyWeaponBlockDefinition).WeaponDefinitionId);
            WeaponDefinition definition = Settings.GetWeaponDefinition(w.Id.SubtypeId.String);

            WeaponBasic basic = new WeaponBasic();
            definition.Clone(basic);

            switch (definition.Type())
            {
                case WeaponType.Ramping:
                    Weapon = new WeaponRamping();
                    (Weapon as WeaponRamping).Set(definition);
                    break;
                case WeaponType.Basic:
                    Weapon = new WeaponBasic();
                    (Weapon as WeaponBasic).Set(definition);
                    break;
            }

            Weapon.Init((MyEntity)Entity);
            OnAddedToContainer();

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateBeforeSimulation()
        {
            if (IsBlackListed) return;

            Weapon.Update();
            Weapon.Spawn();
            Weapon.Draw();
        }

        public override void OnRemovedFromScene()
        {
            Weapon.OnRemovedFromScene();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            Weapon.OnRemovedFromContainer();
        }

        public override void Close()
        {
            Core.OnSettingsUpdate -= OnSettingsUpdate;
            Weapon.Close();
            base.Close();
        }

        // TODO: move this to the Core class and make it run once
        private void DisableNormalWeaponsFire()
        {
            MyWeaponDefinition Weapon = MyDefinitionManager.Static.GetWeaponDefinition(((Entity as IMyFunctionalBlock).SlimBlock.BlockDefinition as MyWeaponBlockDefinition).WeaponDefinitionId);

            //Thanks for the help Digi
            for (int i = 0; i < Weapon.WeaponAmmoDatas.Length; i++)
            {
                var ammoData = Weapon.WeaponAmmoDatas[i];

                if (ammoData == null)
                    continue;

                ammoData.ShootIntervalInMiliseconds = int.MaxValue;
            }
        }

        public static bool IsThisBlockBlacklisted(IMyEntity Entity)
        {
            MyDefinitionBase def = (Entity as IMyTerminalBlock).SlimBlock.BlockDefinition;
            //MyLog.Default.Info($"Type: {def.Id.SubtypeName}");
            //MyLog.Default.Flush();
            return Settings.BlackList.Contains(def.Id.SubtypeName);
        }

        public static void TerminalIntitalize()
        {
            List<IMyTerminalAction> actions = new List<IMyTerminalAction>();
            MyAPIGateway.TerminalControls.GetActions<IMyLargeTurretBase>(out actions);

            foreach (IMyTerminalAction a in actions)
            {
                if (a.Id == "Shoot")
                {
                    TerminalShootActionTurretBase = a.Action;
                    TerminalShootWriterTurretBase = a.Writer;
                }
                else if (a.Id == "ShootOnce")
                {
                    TerminalShootOnceActionTurretBase = a.Action;
                    TerminalShootOnceWriterTurretBase = a.Writer;
                }
                if (a.Id == "Shoot_On")
                {
                    TerminalShootOnActionTurretBase = a.Action;
                    TerminalShootOnWriterTurretBase = a.Writer;
                }
                else if (a.Id == "Shoot_Off")
                {
                    TerminalShootOffActionTurretBase = a.Action;
                    TerminalShootOffWriterTurretBase = a.Writer;
                }
            }

            actions.Clear();
            MyAPIGateway.TerminalControls.GetActions<IMySmallGatlingGun>(out actions);
            foreach (IMyTerminalAction a in actions)
            {
                if (a.Id == "Shoot")
                {
                    TerminalShootActionGatlingGun = a.Action;
                    TerminalShootWriterGatlingGun = a.Writer;
                }
                else if (a.Id == "ShootOnce")
                {
                    TerminalShootOnceActionGatlingGun = a.Action;
                    TerminalShootOnceWriterGatlingGun = a.Writer;
                }
                if (a.Id == "Shoot_On")
                {
                    TerminalShootOnActionGatlingGun = a.Action;
                    TerminalShootOnWriterGatlingGun = a.Writer;
                }
                else if (a.Id == "Shoot_Off")
                {
                    TerminalShootOffActionGatlingGun = a.Action;
                    TerminalShootOffWriterGatlingGun = a.Writer;
                }
            }

            List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
            MyAPIGateway.TerminalControls.GetControls<IMyLargeTurretBase>(out controls);
            foreach (IMyTerminalControl c in controls)
            {
                if (c.Id == "Shoot")
                {
                    IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;
                    TerminalShootGetterTurretBase = onoff.Getter;
                    TerminalShootSetterTurretBase = onoff.Setter;
                }
            }


            controls.Clear();
            MyAPIGateway.TerminalControls.GetControls<IMySmallGatlingGun>(out controls);
            foreach (IMyTerminalControl c in controls)
            {
                if (c.Id == "Shoot")
                {
                    IMyTerminalControlOnOffSwitch onoff = c as IMyTerminalControlOnOffSwitch;
                    TerminalShootGetterGatlingGun = onoff.Getter;
                    TerminalShootSetterGatlingGun = onoff.Setter;
                }
            }

            DefaultTerminalControlsInitialized = true;
        }
    }
}
