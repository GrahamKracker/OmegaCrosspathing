namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixAdoraSacrifice : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        foreach(var ability in tower.GetAbilities().Where(abilityModel => abilityModel.displayName == "Blood Sacrifice"))
        {
            tower.RemoveBehavior(ability);
        }
    }
}