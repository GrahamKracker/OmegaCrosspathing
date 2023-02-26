using Il2CppAssets.Scripts.Models.Towers.Filters;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;

namespace TowerMerger.MergeFixes;

public class FixCamo : PostMergeFix
{
    public override void Apply(TowerModel first, TowerModel second, ref TowerModel combined)
    {
        if (first.GetDescendants<ProjectileModel>().Any(p => p.CanHitCamo()) ||
            first.GetDescendants<FilterInvisibleModel>().Any(x => !x.isActive) || first.GetAttackModels().Any(x =>
                x.GetDescendant<FilterInvisibleModel>() == null || !x.GetDescendant<FilterInvisibleModel>().isActive))
        {
            foreach (var projectile in combined.GetDescendants<ProjectileModel>().ToList())
            {
                projectile.SetHitCamo(true);
            }

            foreach (var filterInvisibleModel in combined.GetDescendants<FilterInvisibleModel>().ToList())
            {
                filterInvisibleModel.isActive = false;
            }

            foreach (var attackModel in combined.GetAttackModels().Where(attackModel => attackModel.GetDescendant<FilterInvisibleModel>() != null))
            {
                attackModel.GetDescendant<FilterInvisibleModel>().isActive = false;
            }
            
        }
        if (second.GetDescendants<ProjectileModel>().Any(p => p.CanHitCamo()) ||
            second.GetDescendants<FilterInvisibleModel>().Any(x => !x.isActive) || second.GetAttackModels().Any(x =>
                x.GetDescendant<FilterInvisibleModel>() == null || !x.GetDescendant<FilterInvisibleModel>().isActive))
        {
            foreach (var projectile in combined.GetDescendants<ProjectileModel>().ToList())
            {
                projectile.SetHitCamo(true);
            }

            foreach (var filterInvisibleModel in combined.GetDescendants<FilterInvisibleModel>().ToList())
            {
                filterInvisibleModel.isActive = false;
            }

            foreach (var attackModel in combined.GetAttackModels().Where(attackModel => attackModel.GetDescendant<FilterInvisibleModel>() != null))
            {
                attackModel.GetDescendant<FilterInvisibleModel>().isActive = false;
            }
        }
    }
}