using System;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers.TowerFilters;
using Il2CppAssets.Scripts.Unity;

namespace OmegaCrosspathing.MergeFixes;

public class FixGenericEnums : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        model.areaTypes = Game.instance.model.GetTower(model.baseId).areaTypes;

        FixType<AddMakeshiftAreaModel>(model, (model1, model2) => model1.filterInTowerSets = model2.filterInTowerSets);
        FixType<FilterInSetModel>(model, (model1, model2) => model1.towerSets = model2.towerSets);
    }

    private static void FixType<T>(TowerModel model, Action<T, T> fix) where T : Model
    {
        T model2 = null;
        model.GetDescendants<T>().ForEach(model1 =>
        {
            if (model2 == null)
            {
                foreach (var towerModel in Game.instance.model.GetTowersWithBaseId(model.baseId))
                {
                    if (towerModel.GetDescendant<T>().Is(out T descendant))
                    {
                        model2 = descendant;
                        break;
                    }
                }
            }

            if (model2 != null)
            {
                fix(model1, model2);
                // ModHelper.Msg<UltimateCrosspathingMod>($"Fixed {Il2CppType.Of<T>().Name} for {model.name}");
                return;
            }

            //ModHelper.Warning<Main>($"Couldn't fix {Il2CppType.Of<T>().Name} for {model.name}");
        });
    }
}
