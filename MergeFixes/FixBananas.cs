using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

namespace OmegaCrosspathing.MergeFixes;

public class FixBananas : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        if (model.appliedUpgrades.Contains(UpgradeType.Marketplace) ||
            model.appliedUpgrades.Contains(UpgradeType.MonkeyBank)) // TODO smartly leave this out in MergeArray
        {
            model.GetWeapon().projectile.RemoveBehaviors<PickupModel>();
            model.GetWeapon().projectile.RemoveBehaviors<ArriveAtTargetModel>();
            model.GetWeapon().projectile.RemoveBehaviors<ScaleProjectileModel>();
            if (model.GetWeapon().projectile.GetBehavior<AgeModel>() is AgeModel ageModel)
            {
                ageModel.Lifespan = 0;
            }

            if (model.GetDescendant<CreateTextEffectModel>() is CreateTextEffectModel createTextEffectModel)
            {
                createTextEffectModel.useTowerPosition = true;
            }
        }
    }
}
