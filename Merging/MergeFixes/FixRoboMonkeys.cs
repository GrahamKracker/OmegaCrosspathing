using BTD_Mod_Helper.Api;
using Il2CppAssets.Scripts.Models.GenericBehaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Simulation.SMath;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixRoboMonkeys : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        if(tower.appliedUpgrades.Contains(UpgradeType.RoboMonkey))
        {
            foreach (var attackModel in tower.GetAttackModels().Where(x => x.HasBehavior<TargetLeftHandModel>()))
            {
                attackModel.RemoveBehavior<TargetLeftHandModel>();
                
                var newAttackModel = attackModel.Duplicate();

                foreach (var weapon in newAttackModel.weapons)
                {
                    weapon.ejectX *= -1;
                }
                
                var displayModel = newAttackModel.GetBehavior<DisplayModel>();

                if (displayModel.display.guidRef == "b86ee66981bfcc14b964b9731264cddf")
                {
                    displayModel.display = CreatePrefabReference("63df1a60aed70cc49acadb98e3190bf3");
                }

                if (displayModel.display.guidRef == "c7974835e6380b741b18d9282e249fe5")
                {
                    displayModel.display = CreatePrefabReference("c8384b47962cb5547b85f0a0e4fba047");
                }
                
                if (displayModel.display.guidRef == "8a151c6c111ff5641882e51afc28c740")
                    displayModel.display = CreatePrefabReference("fdff998beaa71ee45977df86cfda6d96");
                
                
                tower.AddBehavior(newAttackModel);
            }
        }
    }
}