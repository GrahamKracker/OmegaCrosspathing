using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Unity.Bridge;

namespace OmegaCrosspathing.SimulationFixes;

public class AnotherAirUnitModelFix : SimulationFix
{
    public override void Apply(TowerToSimulation tts)
    {
        if (tts.tower.HasTowerBehavior<AirUnit>())
        {
            var airUnit = tts.tower.GetTowerBehavior<AirUnit>();
            //airUnit.modelBehaviors.GetItemOfType<RootBehavior, AttackAirUnit>();
        }
    }
}
