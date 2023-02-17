using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;

namespace OmegaCrosspathing.MergeFixes;

public class FixAdvancedIntel : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        if (model.appliedUpgrades.Contains(UpgradeType.AdvancedIntel))
        {
            model.GetDescendants<TargetFirstSharedRangeModel>().ForEach(m => m.isSharedRangeEnabled = true);
            model.GetDescendants<TargetLastSharedRangeModel>().ForEach(m => m.isSharedRangeEnabled = true);
            model.GetDescendants<TargetCloseSharedRangeModel>().ForEach(m => m.isSharedRangeEnabled = true);
            model.GetDescendants<TargetStrongSharedRangeModel>().ForEach(m => m.isSharedRangeEnabled = true);
        }
    }
}
