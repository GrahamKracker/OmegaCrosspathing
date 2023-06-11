namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixAutoCollectings : PostMergeFix
{
    public override void Apply(TowerModel tower)
    {
        if (tower.appliedUpgrades.Contains(UpgradeType.BananaSalvage))
        {
            tower.GetDescendants<BankModel>().ForEach(bankModel => { bankModel.autoCollect = true; });
        }
    }
}
