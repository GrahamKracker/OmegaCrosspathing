using BTD_Mod_Helper.Api;

namespace OmegaCrosspathing.MergeFixes;

public abstract class PostMergeFix : ModContent
{
    protected override float RegistrationPriority => 0;
    public override int RegisterPerFrame => 999;

    public sealed override void Register(){}

    public abstract void Apply(TowerModel model);
}
