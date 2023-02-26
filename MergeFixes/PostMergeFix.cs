﻿using BTD_Mod_Helper.Api;

namespace OmegaCrosspathing.MergeFixes;

public abstract class PostMergeFix : ModContent
{
    public override int RegisterPerFrame => 999;
    public sealed override void Register(){}

    public virtual void Apply(TowerModel model)
    {
        
    }

    public virtual void Apply(TowerModel first, TowerModel second, ref TowerModel combined)
    {
        
    }
}
