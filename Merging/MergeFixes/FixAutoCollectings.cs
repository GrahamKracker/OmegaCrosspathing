namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixAutoCollectings : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        if (model.appliedUpgrades.Contains(UpgradeType.BananaSalvage))
        {
            model.GetDescendants<BankModel>().ForEach(bankModel => { bankModel.autoCollect = true; });
        }
    }
}
