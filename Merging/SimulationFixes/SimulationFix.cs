using BTD_Mod_Helper.Api;
using Il2CppAssets.Scripts.Unity.Bridge;

namespace OmegaCrosspathing.Merging.SimulationFixes;

public abstract class SimulationFix : ModContent
{
    public override int RegisterPerFrame => 999;
    public sealed override void Register(){}
    public abstract void Apply(TowerToSimulation tts, TowerModel tower, TowerModel newTower);
}
