using System;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.AbilitiesMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using OmegaCrosspathing.Merging;
using UnityEngine;

namespace OmegaCrosspathing;
public partial class Main
{
    private static void SwitchToNormalUpgrades(bool isOn)
    {        
        _mainpanel.gameObject.SetActive(!isOn);
        TowerSelectionMenu.instance.towerDetails.SetActive(isOn);

        foreach (var button in TowerSelectionMenu.instance.upgradeButtons)
        {
            button.gameObject.SetActive(isOn);
        }
        
        for(var i = 0; i < TowerSelectionMenu.instance.towerDetails.transform.childCount; i++)
        {
            TowerSelectionMenu.instance.towerDetails.transform.GetChild(i).gameObject.SetActive(isOn);
        }

        
    }

    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.Show))]
    [HarmonyPostfix]
    private static void TowerSelectionMenu_Show(TowerSelectionMenu __instance)
    {
        if (__instance.selectedTower is null)
        {
            return;
        }

        TaskScheduler.ScheduleTask(() => //required for paths++ support
        {
            if (__instance.selectedTower.owner != InGame.instance.UnityToSimulation.MyPlayerNumber || __instance.selectedTower.tower.towerModel.IsHero())
            {
                SwitchToNormalUpgrades(true);
                return;
            }
            
            if (__instance.selectedTower.tower.towerModel.isParagon)
            {
                _mainpanel.gameObject.SetActive(false);
                __instance.paragonDetails.SetActive(true);
                
                
                return;
            }

            SwitchToNormalUpgrades(false);
        });
    }


    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.Initialise))]
    [HarmonyPostfix]
    private static void TowerSelectionMenu_Initialise(TowerSelectionMenu __instance)
    {
        var rect = __instance.towerDetails.GetComponent<RectTransform>().rect;

        _mainpanel =
            __instance.towerDetails.transform.parent.gameObject.AddModHelperPanel(new Info("MergingPanel",
                InfoPreset.FillParent));

        towersetselect = _mainpanel.AddScrollPanel(new Info("TowersetSelect",
                rect.width,
                rect.height / 3, new Vector2(.5f, .85f)), RectTransform.Axis.Horizontal,
            VanillaSprites.BrownInsertPanel, 15, 50);
        
        towersetselect.ScrollContent.transform.Cast<RectTransform>().pivot = new Vector2(0, 0);
        
        pathselect = _mainpanel.AddScrollPanel(new Info("PathSelect",
            rect.width, rect.height / 3), RectTransform.Axis.Horizontal, "", 15f, 0);
        pathselect.Mask.showMaskGraphic = false;
        
        levelselect = _mainpanel.AddPanel(new Info("LevelSelect",
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
                    if (InGame.instance.GetCash() < totalcost)
                    {
                        return;
                    }

                    var OCMutator = TowerSelectionMenu.instance.selectedTower.tower.GetMutator("OC")?.Cast<SupportRemoveFilterOutTag.MutatorTower>();
                    
                    if (TowerSelectionMenu.instance.selectedTower.tower.mutators != null)
                        TowerSelectionMenu.instance.selectedTower.tower.RemoveMutatorsById("OC");

                    var savedata = selectedtower.GetBaseId() + ":" + selectedtower.tiers[0] + "-" + selectedtower.tiers[1] + "-" + selectedtower.tiers[2];

                    if (selectedtower.IsHero())
                    {
                        savedata = "hero:" + selectedtower.GetBaseId() + ":" + selectedtower.tiers[0];
                    }
                    
                    
                    TowerSelectionMenu.instance.selectedTower.tower.AddMutator(
                        new SupportRemoveFilterOutTag.MutatorTower("OC",
                            OCMutator?.removeScriptsWithSupportMutatorId + savedata + ",", 
                            null));

                    var owner = TowerSelectionMenu.instance.selectedTower.tower.owner;

                    if (owner == -1)
                    {
                        owner = 0;
                    }
                    else
                    {
                        owner--;
                    }

                    InGame.instance.GetCashManager(owner).cash.Value -= totalcost;
                    InGame.instance.bridge.OnCashChangedSim();


                    TowerSelectionMenu.instance.selectedTower.tower.worth += totalcost;


                    HideAllSelected();
                    selectedtower = null;
                    foreach (var slider in Pathsliders)
                        slider.SetCurrentValue(0);
                    foreach (var slider in Pathsplusplussliders.Values)
                        slider.SetCurrentValue(0);
                    UpdateBottomBar();

                    AbilityMenu.instance.AbilitiesChanged();
                    AbilityMenu.instance.TowerChanged(TowerSelectionMenu.instance.selectedTower);
                    AbilityMenu.instance.Update();

                    TowerSelectionMenu.instance.themeManager.UpdateTheme(TowerSelectionMenu.instance
                        .selectedTower);
                    TowerSelectionMenu.instance.UpdateTower();
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
        SetUpLevelInput();
        LockInputFields();
    }

    [HarmonyPatch(typeof(SupportRemoveFilterOutTag.MutatorTower),
        nameof(SupportRemoveFilterOutTag.MutatorTower.Mutate))]
    [HarmonyPrefix]
    static bool SupportRemoveFilterOutTag_MutatorTower_Mutate(SupportRemoveFilterOutTag.MutatorTower __instance,
        Model model,
        ref bool __result)
    {
        if (__instance.id != "OC")
        {
            return true;
        }

        var tower = model.Cast<TowerModel>();

        var split = __instance.removeScriptsWithSupportMutatorId.Split(',');
        foreach (var id in split.Where(x => !string.IsNullOrEmpty(x)))
        {
            TowerModel towerToMerge;
            if (id.StartsWith("hero:"))
            {
                var nonheroid = id.Replace("hero:", "");
                towerToMerge = InGame.instance.GetGameModel().GetTowerModel(nonheroid.Split(':')[0], int.Parse(nonheroid.Split(':')[1]));
            }
            else
            {
                var tiers = id.Split(':')[1];
                towerToMerge = InGame.instance.GetGameModel().GetTowerModel(id.Split(':')[0], int.Parse(tiers.Split('-')[0]), int.Parse(tiers.Split('-')[1]), int.Parse(tiers.Split('-')[2]));
            }

            Algorithm.Merge(tower, towerToMerge);
        }

        __result = true;
        return false;
    }
}