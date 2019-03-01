using ProjectilesImproved.Definitions;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace ProjectilesImproved.Effects.Collision
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Shrapnel : MySessionComponentBase
    {
        private Queue<ShrapnelData> queue = new Queue<ShrapnelData>();

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(9, ProcessDamage);
        }

        public void ProcessDamage(object target, ref MyDamageInformation info)
        {
            if (!(target is IMySlimBlock)) return;

            string id = $"{info.Type.String}";
            if (info.Type == MyDamageType.Bullet && Settings.ProjectileDefinitionLookup.ContainsKey(id))
            {
                ProjectileDefinition ammo = Settings.ProjectileDefinitionLookup[id];
                if (ammo.UseOverKillSpread)
                {
                    IMySlimBlock slim = target as IMySlimBlock;
                    if (slim.Integrity >= info.Amount) return;

                    float overkill = info.Amount - slim.Integrity;
                    info.Amount = slim.Integrity;

                    queue.Enqueue(new ShrapnelData()
                    {
                        Ammo = ammo,
                        Neighbours = slim.Neighbours,
                        OverKill = overkill,
                        Type = info.Type,
                        AttackerId = info.AttackerId
                    });
                }
            }
        }

        public override void UpdateBeforeSimulation()
        {
            while (queue.Count > 0)
            {
                ShrapnelData data = queue.Dequeue();
                int count = data.Neighbours.Count;
                foreach (IMySlimBlock neighbour in data.Neighbours)
                {
                    float damage = ((data.OverKill / (float)count) * data.Ammo.OverKillSpreadScaler);
                    neighbour.DoDamage(damage, data.Type, true, default(MyHitInfo), data.AttackerId);
                }
            }
        }
    }

    internal class ShrapnelData
    {
        public ProjectileDefinition Ammo { get; set; }
        public float OverKill { get; set; }
        public List<IMySlimBlock> Neighbours { get; set; }
        public long AttackerId;
        public MyStringHash Type;
    }
}
