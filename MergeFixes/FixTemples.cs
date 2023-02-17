using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;

namespace OmegaCrosspathing.MergeFixes;

public class FixTemples : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        if (model.appliedUpgrades.Contains(UpgradeType.SunTemple))
        {
            foreach (var attackModel in model.GetAttackModels())
            {
                var rotateToTargetModel = attackModel.GetBehavior<RotateToTargetModel>();
                if (rotateToTargetModel != null)
                {
                    rotateToTargetModel.rotateTower = false;
                }

                attackModel.RemoveBehaviors<RotateToMiddleOfTargetsModel>();
            }
        }
    }
}
