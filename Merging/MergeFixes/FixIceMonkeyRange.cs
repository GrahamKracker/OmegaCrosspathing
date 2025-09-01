namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixIceMonkeyRange : MergeFix
{
    public override void Apply(TowerModel tower)
    {
        foreach (var slow in tower.GetDescendants<SlowBloonsZoneModel>().ToList())
        {
            slow.zoneRadius = tower.range + 5;
        }
    }
}
