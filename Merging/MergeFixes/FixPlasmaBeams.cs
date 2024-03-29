﻿using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Emissions;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixPlasmaBeams : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        if (tower.appliedUpgrades.Contains(UpgradeType.PlasmaAccelerator))
        {
            tower.GetWeapon().projectile.radius = 2;
            //model.GetWeapon().projectile.RemoveBehavior<TravelStraightSlowdownModel>();
            //model.GetWeapon().projectile.RemoveBehavior<KnockbackModel>();
        }

        var lineProjectileAttacks = tower.GetAttackModels().Where(attackModel =>
                attackModel.weapons.Any(weaponModel => weaponModel.emission.IsType<LineProjectileEmissionModel>())).ToList();
        
        if (lineProjectileAttacks.Count > 1)
        {
            var behaviors = Enumerable.ToList(tower.behaviors);
            behaviors.RemoveAll(m => m.IsType<AttackModel>(out var attackModel) &&
                                     attackModel.weapons.Any(weaponModel =>
                                         weaponModel.emission.IsType<LineProjectileEmissionModel>()));
            behaviors.Add(lineProjectileAttacks[0]);
            tower.behaviors = behaviors.ToIl2CppReferenceArray();
        }
    }
}
