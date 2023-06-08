using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixSpikeStorm : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        if (model.appliedUpgrades.Contains(UpgradeType.SpikeStorm))
        {
            var realProjectile = model.GetWeapon().projectile;
            model.GetAbility().GetBehavior<ActivateAttackModel>().attacks.ForEach(attackModel =>
            {
                var projectileModel = attackModel.weapons[0].projectile;
                var ageRandomModel = projectileModel.GetBehavior<AgeRandomModel>().Duplicate();

                var newProjectile = realProjectile.Duplicate();
                newProjectile.RemoveBehaviors<AgeModel>();
                newProjectile.AddBehavior(ageRandomModel);

                attackModel.weapons[0].projectile = newProjectile;
            });
        }
    }
}
