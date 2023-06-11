using System;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack;
using System.Linq;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixSmallRanges : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        // TODO: are there any cases where this shouldn't be the case?
        foreach (var attackModel in tower.GetDescendants<AttackModel>().ToList())
        {
            attackModel.range = Math.Max(attackModel.range, tower.range);
        }
    }
}
