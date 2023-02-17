using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

namespace OmegaCrosspathing.MergeFixes;

public class FixLifespans : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        model.GetDescendants<AgeModel>().ForEach(ageModel =>
        {
            ageModel.Lifespan = System.Math.Max(ageModel.Lifespan, ageModel.lifespanFrames / 60f);
        });
    }
}
