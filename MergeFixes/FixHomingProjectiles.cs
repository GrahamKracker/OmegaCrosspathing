using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

namespace OmegaCrosspathing.MergeFixes;

public class FixHomingProjectiles : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        foreach (var projectileModel in model.GetDescendants<ProjectileModel>().ToList().Where(projectileModel => projectileModel.GetBehavior<RetargetOnContactModel>() != null))
        {
            projectileModel.RemoveBehavior<FollowPathModel>();
        }
    }
}
