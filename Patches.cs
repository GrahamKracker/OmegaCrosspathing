using System;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.AbilitiesMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using OmegaCrosspathing.SimulationFixes;
using UnityEngine;

namespace OmegaCrosspathing;

public partial class Main
{
    static void SwitchNormal(bool isOn)
    {
        for (int i = 0; i < TowerSelectionMenu.instance.towerDetails.transform.childCount; i++)
        {
            var child = TowerSelectionMenu.instance.towerDetails.transform.GetChild(i);
            child.gameObject.SetActive(isOn);
        }
    }

    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.Show))]
    [HarmonyPostfix]
    private static void TowerSelectionMenu_Show(TowerSelectionMenu __instance)
    {
        if (__instance.selectedTower != null && __instance.selectedTower.tower.towerModel.IsHero())
        {
            _mainpanel.gameObject.SetActive(false);
            SwitchNormal(true);
            return;
        }

        _mainpanel.gameObject.SetActive(true);
        SwitchNormal(false);
    }


    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.Initialise))]
    [HarmonyPostfix]
    private static void TowerSelectionMenu_Initialise(TowerSelectionMenu __instance)
    {
        try
        {
            var rect = __instance.towerDetails.GetComponent<RectTransform>().rect;
            var parent = __instance.towerDetails.transform.parent;

            _mainpanel = parent.gameObject.AddModHelperPanel(new Info("MergingPanel", InfoPreset.FillParent));


            towersetselect = _mainpanel.AddScrollPanel(new Info("TowersetSelect",
                    rect.width,
                    rect.height / 3, new Vector2(.5f, .85f)), RectTransform.Axis.Horizontal,
                VanillaSprites.BrownInsertPanel, 15, 50);

            pathselect = _mainpanel.AddPanel(new Info("PathSelect",
                rect.width,
                rect.height / 3), null, RectTransform.Axis.Horizontal, 15f);

            finalselect = _mainpanel.AddPanel(new Info("UpgradeSelect",
                rect.width,
                rect.height / 3, new Vector2(.5f, .15f)), VanillaSprites.BrownPanel);

            const int buttonSize = 255;
            cost = finalselect.AddText(new Info("MergeCost", buttonSize, buttonSize, new Vector2(.765f, .15f)), "",
                45f);

            cost.enabled = false;
            mergebutton =
                finalselect.AddButton(new Info("MergeButton", 350, 200, new Vector2(.765f, .5f)),
                    VanillaSprites.GreenBtnLong, new Action(() =>
                    {
                        try
                        {
                            if (InGame.instance.GetCash() < totalcost)
                            {
                                return;
                            }

                            var newTower = Algorithm.Merge(TowerSelectionMenu.instance.selectedTower.Def,
                                selectedtower);
                            TowerSelectionMenu.instance.selectedTower.tower.SetTowerModel(newTower);
                            TowerSelectionMenu.instance.selectedTower.tower.towerModel = newTower;
                            TowerSelectionMenu.instance.selectedTower.tower.model = newTower;
                            
                            
                            
                            foreach (var simulationFix in ModContent.GetContent<SimulationFix>())
                            {
                                simulationFix.Apply(TowerSelectionMenu.instance.selectedTower);
                            }

                            InGame.instance.AddCash(-totalcost);
                            TowerSelectionMenu.instance.selectedTower.tower.worth += totalcost;
                            
                            HideAllSelected();
                            selectedtower = null;
                            Pathsliders[0].SetCurrentValue(0);
                            Pathsliders[1].SetCurrentValue(0);
                            Pathsliders[2].SetCurrentValue(0);
                            UpdateBottomBar();

                            AbilityMenu.instance.AbilitiesChanged();
                            AbilityMenu.instance.TowerChanged(TowerSelectionMenu.instance.selectedTower);
                            AbilityMenu.instance.Update();
                            
                            TowerSelectionMenu.instance.themeManager.UpdateTheme(TowerSelectionMenu.instance
                                .selectedTower);
                            TowerSelectionMenu.instance.UpdateTower();
                            
                            
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Error(e);
                        }
                    }));
            
            mergetext = mergebutton.AddText(new Info("MergeText", buttonSize, buttonSize, new Vector2(.5f, .55f)),
                "Merge",
                65f);


            towerportrait = finalselect.AddImage(new Info("TowerPortrait", 300, 300, new Vector2(.2f, .5f)), "");

            invalidtext = finalselect.AddText(new Info("InvalidText", 500, 100, new Vector2(.5f, .875f)),
                "Invalid Tower",
                55f);
            invalidtext.Text.color = Color.red;

            UpdateBottomBar();

            SetUpTowerButtons();
            SetUpPathInput();
        }
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }
    }
}
