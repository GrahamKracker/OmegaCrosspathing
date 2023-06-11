namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixIceMonkeyRange : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        foreach (var slow in tower.GetDescendants<SlowBloonsZoneModel>().ToList())
        {
            slow.zoneRadius = tower.range + 5;
        }
    }
}
