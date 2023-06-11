using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixLifespans : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        tower.GetDescendants<AgeModel>().ForEach(ageModel =>
        {
            ageModel.Lifespan = System.Math.Max(ageModel.Lifespan, ageModel.lifespanFrames / 60f);
            ageModel.lifespan = System.Math.Max(ageModel.Lifespan, ageModel.lifespanFrames / 60f);
        });
    }
}
