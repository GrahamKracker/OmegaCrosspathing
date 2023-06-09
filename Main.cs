using BTD_Mod_Helper;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using OmegaCrosspathing;
using Main = OmegaCrosspathing.Main;

[assembly: MelonInfo(typeof(Main), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
[assembly: MelonOptionalDependencies("PathsPlusPlus")]

namespace OmegaCrosspathing;

[HarmonyPatch]
public partial class Main : BloonsTD6Mod
{
    public static float totalcost;
    
    public static bool HasPathsPlusPlus => ModHelper.HasMod("PathsPlusPlus");

    public static string selectedBaseID = "";
    public static TowerModel? selectedtower;

    public override void OnTowerSelected(Tower tower)
    {
        PortraitManager.SetUpPortrait(tower.GetTowerToSim());
    }

    public override void OnMainMenu()
    {
        selectedBaseID = "";
        selectedtower = null;
    }

    public override void OnTowerSaved(Tower tower, TowerSaveDataModel saveData)
    {
        var OCMutator = tower.GetMutator("OC")?.TryCast<SupportRemoveFilterOutTag.MutatorTower>();
        if (OCMutator != null)
            saveData.metaData["OC"] = OCMutator.removeScriptsWithSupportMutatorId;
    }

    public override void OnTowerLoaded(Tower tower, TowerSaveDataModel saveData)
    {
        if (!saveData.metaData.TryGetValue("OC", out var ocMutator))
            return;

        if (tower.mutators != null)
            tower.RemoveMutatorsById("OC");

        tower.AddMutator(new SupportRemoveFilterOutTag.MutatorTower("OC", ocMutator, null));
    }
}