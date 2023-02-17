namespace OmegaCrosspathing.MergeFixes;

public class FixAirUnits : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        if (!model.HasBehavior<AirUnitModel>(out var airunit))
            return;
        
        var airunits = model.GetBehaviors<AirUnitModel>();
        
        if (airunits.Count > 1)
        {
            Algorithm.DeepMerge(airunit, airunits[1], new Algorithm.History());
            model.RemoveBehavior(airunits[1]);
        }

        if (airunit.HasBehavior<HeliMovementModel>())
        {
            airunit.RemoveBehavior<PathMovementModel>();
            airunit.RemoveBehavior<CircleMovementModel>();
            airunit.RemoveBehavior<FigureEightMovementModel>();
            //airunit.RemoveBehavior<>();
        }
        
    }
}
