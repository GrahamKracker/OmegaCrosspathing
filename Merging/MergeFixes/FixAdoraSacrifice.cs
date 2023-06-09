namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixAdoraSacrifice : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        foreach(var ability in model.GetAbilities().Where(abilityModel => abilityModel.displayName == "Blood Sacrifice"))
        {
            model.RemoveBehavior(ability);
        }
    }
}