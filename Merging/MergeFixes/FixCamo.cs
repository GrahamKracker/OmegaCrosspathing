using Il2CppAssets.Scripts.Models.Towers.Filters;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixCamo : PostMergeFix
{
    public static void SetHitCamo(ProjectileModel projectileModel)
    {
        if (projectileModel.HasBehavior<ProjectileFilterModel>(out var projectileFilterModel) && projectileFilterModel.filters.GetItemOfType<FilterModel, FilterInvisibleModel>() is { } filterInvisibleModel)
        {
            filterInvisibleModel.isActive = false;
        }
    }
    public override void Apply(TowerModel tower, TowerModel second)
    {
        if (tower.GetDescendants<ProjectileModel>().Any(p => p.CanHitCamo()) ||
            tower.GetDescendants<FilterInvisibleModel>().Any(x => !x.isActive) || tower.GetAttackModels().Any(x =>
                x.GetDescendant<FilterInvisibleModel>() == null ||
                !x.GetDescendant<FilterInvisibleModel>().isActive))
        {
            foreach (var projectile in tower.GetDescendants<ProjectileModel>().ToList().Where(x => x is not null))
            {
                SetHitCamo(projectile);
            }

            foreach (var filterInvisibleModel in tower.GetDescendants<FilterInvisibleModel>().ToList().Where(x => x is not null))
            {
                filterInvisibleModel.isActive = false;
            }

            foreach (var attackModel in tower.GetAttackModels().Where(attackModel => attackModel.GetDescendant<FilterInvisibleModel>() != null))
            {
                attackModel.GetDescendant<FilterInvisibleModel>().isActive = false;
            }
        }

        if (second.GetDescendants<ProjectileModel>().Any(p => p.CanHitCamo()) ||
            second.GetDescendants<FilterInvisibleModel>().Any(x => !x.isActive) || second.GetAttackModels().Any(x =>
                x.GetDescendant<FilterInvisibleModel>() == null ||
                !x.GetDescendant<FilterInvisibleModel>().isActive))
        {
            foreach (var projectile in tower.GetDescendants<ProjectileModel>().ToList().Where(x => x is not null))
            {
                SetHitCamo(projectile);
            }

            foreach (var filterInvisibleModel in tower.GetDescendants<FilterInvisibleModel>().ToList()
                         .Where(x => x is not null))
            {
                filterInvisibleModel.isActive = false;
            }

            foreach (var attackModel in tower.GetAttackModels()
                         .Where(attackModel => attackModel.GetDescendant<FilterInvisibleModel>() != null))
            {
                attackModel.GetDescendant<FilterInvisibleModel>().isActive = false;
            }
        }
    }
}