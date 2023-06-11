using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixClusterMauling : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        if (tower.appliedUpgrades.Contains(UpgradeType.MOABMauler))
        {
            var damageModifierForTagModels = tower.GetDescendants<DamageModifierForTagModel>().ToList();
            var moabage = damageModifierForTagModels.FirstOrDefault(m => m.tag == "Moabs");
            var ceramage = damageModifierForTagModels.FirstOrDefault(m => m.tag == "Ceramic");

            foreach (var projectileModel in tower.GetDescendants<ProjectileModel>().ToList()
                         .Where(p => p.id == "Explosion"))
            {
                projectileModel.RemoveBehaviors<DamageModifierForTagModel>();
                if (moabage != null)
                {
                    projectileModel.AddBehavior(moabage.Duplicate());
                }

                if (ceramage != null)
                {
                    projectileModel.AddBehavior(ceramage.Duplicate());
                }
            }
        }
    }
}
