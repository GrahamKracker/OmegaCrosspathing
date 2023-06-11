namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixCannonShips : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        if (tower.appliedUpgrades.Contains(UpgradeType.CannonShip))
        {
            if (tower.appliedUpgrades.Contains(UpgradeType.Destroyer)) // TODO apply rate buffs to all weapons
            {
                tower.GetWeapon(4).Rate /= 5f;
                tower.GetWeapon(5).Rate /= 5f;
            }
        }
    }
}
