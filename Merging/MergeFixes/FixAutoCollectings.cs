namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixAutoCollectings : MergeFix
{
    public override void Apply(TowerModel tower)
    {
        if (tower.appliedUpgrades.Contains(UpgradeType.BananaSalvage))
        {
            tower.GetDescendants<BankModel>().ForEach(bankModel => { bankModel.autoCollect = true; });
        }
    }
}
