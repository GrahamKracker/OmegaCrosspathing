namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixHeroes : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        foreach (var heroModel in model.GetBehaviors<HeroModel>())
        {
            model.RemoveBehavior(heroModel);
        }
        
        foreach (var heroXpPerRoundModel in model.GetBehaviors<HeroXpPerRoundModel>())
        {
            model.RemoveBehavior(heroXpPerRoundModel);
        }
        
        foreach (var churchillBaseRotationModel in model.GetBehaviors<ChurchillBaseRotationModel>())
        {
            model.RemoveBehavior(churchillBaseRotationModel);
        }
    }
}