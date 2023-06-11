using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixHomingProjectiles : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        foreach (var projectileModel in tower.GetDescendants<ProjectileModel>().ToList().Where(projectileModel => projectileModel.GetBehavior<RetargetOnContactModel>() != null))
        {
            projectileModel.RemoveBehavior<FollowPathModel>();
        }
    }
}
