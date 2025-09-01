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
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once ConvertToConstant.Global
    public static readonly string MutatorName = "OmegaCrosspathing"; //public and static so it can be used in other mods and also isnt inlined into the IL

    public static float totalcost;
    public static bool HasPathsPlusPlus => ModHelper.HasMod("PathsPlusPlus");

    public static string selectedBaseID = "";
    public static TowerModel? selectedtower;

    public override void OnMainMenu()
    {
        selectedBaseID = "";
        selectedtower = null;
    }

    public override void OnTowerSaved(Tower tower, TowerSaveDataModel saveData)
    {
        var mutator = tower.GetMutator(MutatorName)?.TryCast<SupportRemoveFilterOutTag.MutatorTower>();
        if (mutator != null)
            saveData.metaData[MutatorName] = mutator.removeScriptsWithSupportMutatorId;
    }

    public override void OnTowerLoaded(Tower tower, TowerSaveDataModel saveData)
    {
        if (!saveData.metaData.TryGetValue(MutatorName, out var mutator))
            return;

        if (tower.mutators != null)
            tower.RemoveMutatorsById(MutatorName);

        tower.AddMutator(new SupportRemoveFilterOutTag.MutatorTower(MutatorName, mutator, null));
    }
}