using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Weapons;
using Il2CppAssets.Scripts.Models.Towers.Weapons.Behaviors;

namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixAircraftCarriers : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        if (tower.appliedUpgrades.Contains(UpgradeType.AircraftCarrier))
        {
            tower.GetDescendants<WeaponModel>().ForEach(weaponModel =>
            {
                var createTowerModel = weaponModel.GetDescendant<CreateTowerModel>();
                var filter = weaponModel.GetDescendant<SubTowerFilterModel>();
                if (createTowerModel != null && filter != null)
                {
                    filter.baseSubTowerId = createTowerModel.tower.baseId;
                    filter.baseSubTowerIds = new[] {filter.baseSubTowerId};
                }
            });
        }
    }
}