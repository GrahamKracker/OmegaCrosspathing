namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixHeroes : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        foreach (var heroModel in tower.GetBehaviors<HeroModel>())
        {
            tower.RemoveBehavior(heroModel);
        }
        
        foreach (var heroXpPerRoundModel in tower.GetBehaviors<HeroXpPerRoundModel>())
        {
            tower.RemoveBehavior(heroXpPerRoundModel);
        }
        
        foreach (var churchillBaseRotationModel in tower.GetBehaviors<ChurchillBaseRotationModel>())
        {
            tower.RemoveBehavior(churchillBaseRotationModel);
        }
    }
}