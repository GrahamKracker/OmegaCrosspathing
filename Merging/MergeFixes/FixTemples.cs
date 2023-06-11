using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixTemples : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        if (tower.appliedUpgrades.Contains(UpgradeType.SunTemple))
        {
            foreach (var attackModel in tower.GetAttackModels())
            {
                if (attackModel.HasBehavior<RotateToTargetModel>())
                {
                    attackModel.GetBehavior<RotateToTargetModel>().rotateTower = false;
                }
                
                attackModel.RemoveBehaviors<RotateToMiddleOfTargetsModel>();
            }
        }
    }
}
