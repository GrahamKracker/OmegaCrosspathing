using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixAbilities : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        foreach (var ability in tower.GetAbilities().Where(abilityModel => abilityModel.displayName is "Supply Drop" or "Bomb Blitz"))
        {
            var activateAttackModel = ability.GetBehavior<ActivateAttackModel>();
            activateAttackModel.isOneShot = true;
        }

        if (tower.appliedUpgrades.Contains(UpgradeType.EliteSniper))
        {
            tower.RemoveBehavior<TargetSupplierSupportModel>();
        }
    }
}
