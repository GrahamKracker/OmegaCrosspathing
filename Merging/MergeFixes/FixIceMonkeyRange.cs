namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixIceMonkeyRange : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        if (model.GetDescendant<SlowBloonsZoneModel>().IsType<SlowBloonsZoneModel>(out var slow))
        {
            slow.zoneRadius = model.range + 5;
        }
    }
}
