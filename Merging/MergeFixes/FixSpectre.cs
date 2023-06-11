using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixSpectre : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        if (tower.appliedUpgrades.Contains(UpgradeType.NevaMissTargeting))
        {
            var trackTargetModel = tower.GetDescendant<TrackTargetModel>();
            tower.GetAttackModels().ForEach(attackModel =>
            {
                attackModel.GetDescendants<ProjectileModel>().ForEach(projectileModel =>
                {
                    if (projectileModel.HasBehavior<TravelStraitModel>())
                    {
                        var travelStraitModel = projectileModel.GetBehavior<TravelStraitModel>();
                        if (!projectileModel.HasBehavior<TrackTargetModel>())
                        {
                            var targetModel = trackTargetModel.Duplicate();
                            projectileModel.AddBehavior(targetModel);
                        }

                        projectileModel.GetBehavior<TrackTargetModel>().TurnRate = travelStraitModel.Speed * 2;
                    }
                });
            });
        }
    }
}
