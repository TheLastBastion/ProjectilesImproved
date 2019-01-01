using ProjectilesImproved.Definitions;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;

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
        public IWeapon Weapon = new WeaponBasic();

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
            Weapon.Update();
            Weapon.Spawn();
            Weapon.Draw();
        }

        public override void Close()
        {
            Core.OnSettingsUpdate -= OnSettingsUpdate;
            Weapon.Close();
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
    }
}
