namespace OmegaCrosspathing.Merging.MergeFixes;

public class FixHeroVoicelines : MergeFix
{
    public override void Apply(TowerModel model)
    {
        model.RemoveBehaviors<CreateSoundOnBloonEnterTrackModel>();
        model.RemoveBehaviors<CreateSoundOnBloonLeakModel>();
        model.RemoveBehaviors<CreateSoundOnSelectedModel>();
        model.RemoveBehaviors<CreateSoundOnUpgradeModel>();
        model.RemoveBehaviors<CreateSoundOnTowerPlaceModel>();
        model.RemoveBehaviors<CreateSoundOnBloonDestroyedModel>();
    }
}