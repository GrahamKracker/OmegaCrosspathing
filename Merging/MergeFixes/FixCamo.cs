using Il2CppAssets.Scripts.Models.Towers.Filters;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixCamo : PostMergeFix
{
    public override void Apply(TowerModel model, TowerModel second)
    {
        if (model.GetDescendants<ProjectileModel>().Any(p => p.CanHitCamo()) ||
            model.GetDescendants<FilterInvisibleModel>().Any(x => !x.isActive) || model.GetAttackModels().Any(x =>
                x.GetDescendant<FilterInvisibleModel>() == null ||
                !x.GetDescendant<FilterInvisibleModel>().isActive))
        {
            foreach (var projectile in model.GetDescendants<ProjectileModel>().ToList()
                         .Where(x => x is not null))
            {
                projectile?.SetHitCamo(true);
            }

            foreach (var filterInvisibleModel in model.GetDescendants<FilterInvisibleModel>().ToList()
                         .Where(x => x is not null))
            {
                filterInvisibleModel.isActive = false;
            }

            foreach (var attackModel in model.GetAttackModels()
                         .Where(attackModel => attackModel.GetDescendant<FilterInvisibleModel>() != null))
            {
                attackModel.GetDescendant<FilterInvisibleModel>().isActive = false;
            }
        }

        if (second.GetDescendants<ProjectileModel>().Any(p => p.CanHitCamo()) ||
            second.GetDescendants<FilterInvisibleModel>().Any(x => !x.isActive) || second.GetAttackModels().Any(x =>
                x.GetDescendant<FilterInvisibleModel>() == null ||
                !x.GetDescendant<FilterInvisibleModel>().isActive))
        {
            foreach (var projectile in model.GetDescendants<ProjectileModel>().ToList()
                         .Where(x => x is not null))
            {
                projectile?.SetHitCamo(true);
            }

            foreach (var filterInvisibleModel in model.GetDescendants<FilterInvisibleModel>().ToList()
                         .Where(x => x is not null))
            {
                filterInvisibleModel.isActive = false;
            }

            foreach (var attackModel in model.GetAttackModels()
                         .Where(attackModel => attackModel.GetDescendant<FilterInvisibleModel>() != null))
            {
                attackModel.GetDescendant<FilterInvisibleModel>().isActive = false;
            }
        }
    }
}