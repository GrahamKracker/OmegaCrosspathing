using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixAdvancedIntel : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        //if (tower.appliedUpgrades.Contains(UpgradeType.AdvancedIntel))
        {
            tower.GetDescendants<TargetFirstSharedRangeModel>().ForEach(m => m.isSharedRangeEnabled = true);
            tower.GetDescendants<TargetLastSharedRangeModel>().ForEach(m => m.isSharedRangeEnabled = true);
            tower.GetDescendants<TargetCloseSharedRangeModel>().ForEach(m => m.isSharedRangeEnabled = true);
            tower.GetDescendants<TargetStrongSharedRangeModel>().ForEach(m => m.isSharedRangeEnabled = true);
        }
    }
}
