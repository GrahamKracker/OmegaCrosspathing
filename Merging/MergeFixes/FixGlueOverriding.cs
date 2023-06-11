using System;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Filters;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixGlueOverriding : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        if (tower.appliedUpgrades.Contains(UpgradeType.MOABGlue))
        {
            tower.GetDescendants<ProjectileModel>().ForEach(projectileModel =>
            {
                var behaviors = projectileModel.behaviors.ToList();
                behaviors.RemoveAll(m => m.name == "SlowModifierForTagModel_");
                projectileModel.behaviors = behaviors.ToIl2CppReferenceArray();
            });

            var lifeSpan = 0f;
            tower.GetDescendants<SlowForBloonModel>().ForEach(m => { lifeSpan = Math.Max(lifeSpan, m.Lifespan); });
            tower.GetDescendants<SlowModel>().ForEach(m => { lifeSpan = Math.Max(lifeSpan, m.Lifespan); });
            tower.GetDescendants<AddBehaviorToBloonModel>().ForEach(m =>
            {
                lifeSpan = Math.Max(lifeSpan, m.lifespan);
            });

            tower.GetDescendants<SlowForBloonModel>().ForEach(m => { m.Lifespan = lifeSpan; });
            tower.GetDescendants<SlowModel>().ForEach(m => { m.Lifespan = lifeSpan; });
            tower.GetDescendants<AddBehaviorToBloonModel>().ForEach(m => { m.lifespan = lifeSpan; });

            tower.GetDescendants<AttackFilterModel>().ForEach(filterModel =>
            {
                var models = filterModel.filters.ToList();
                models.RemoveAll(m => m.IsType<FilterOutTagModel>());
                filterModel.filters = models.ToIl2CppReferenceArray();
            });

            tower.GetDescendants<ProjectileFilterModel>().ForEach(filterModel =>
            {
                var models = filterModel.filters.ToList();
                models.RemoveAll(m => m.IsType<FilterOutTagModel>());
                filterModel.filters = models.ToIl2CppReferenceArray();
            });
        }
    }
}
