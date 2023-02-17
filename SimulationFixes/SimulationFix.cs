using BTD_Mod_Helper.Api;
using Il2CppAssets.Scripts.Unity.Bridge;

namespace OmegaCrosspathing.SimulationFixes;

public abstract class SimulationFix : ModContent
{
    protected override float RegistrationPriority => 0;
    public override int RegisterPerFrame => 999;
    public sealed override void Register(){}

    public abstract void Apply(TowerToSimulation tower);
}
