using System;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Helpers;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.AbilitiesMenu;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using OmegaCrosspathing.Merging;
using OmegaCrosspathing.Merging.SimulationFixes;
using PathsPlusPlus;
using UnityEngine;
using static OmegaCrosspathing.UI;

namespace OmegaCrosspathing;

public partial class Main
{
    private static bool OCUIenabled = true;

    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.Show))]
    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.UpdateTower))]
    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.UpdateAbilities))]
    [HarmonyPostfix]
    public static void TowerSelectionMenu_Show(TowerSelectionMenu __instance)
    {
        if (__instance.selectedTower is null) return;

        TaskScheduler.ScheduleTask(() =>
        {
            if (__instance.selectedTower is null) return;

            OCTogglePanel.gameObject.SetActive(true);
            
            if (__instance.selectedTower.owner != InGame.instance.UnityToSimulation.MyPlayerNumber)
            {
                _mainpanel.gameObject.SetActive(false);
                OCTogglePanel.gameObject.SetActive(false);
                return;
            }

            if (__instance.selectedTower.tower.towerModel.IsHero())
            {
                _mainpanel.gameObject.SetActive(false);
                OCTogglePanel.gameObject.SetActive(false);
                return;
            }

            if (__instance.selectedTower.tower.towerModel.isParagon)
            {
                _mainpanel.gameObject.SetActive(false);
                OCTogglePanel.gameObject.SetActive(false);
                return;
            }

            if (__instance.selectedTower.tower.towerModel.dontDisplayUpgrades || __instance.selectedTower.tower.towerModel.isSubTower || __instance.selectedTower.tower.towerModel.isPowerTower)
            {
                _mainpanel.gameObject.SetActive(false);
                OCTogglePanel.gameObject.SetActive(false);
            }

            UpdateVanillaUI();
        });
    }

    private static void UpdateVanillaUI()
    {
        if (_mainpanel == null) return;

        _mainpanel.gameObject.SetActive(OCUIenabled);

        var count = TowerSelectionMenu.instance.upgradeButtons.Count;
        if (count > 3)
        {
            count = 3;
        }

        for (var index = 0; index < count; index++)
        {
            var upgradeObject = TowerSelectionMenu.instance.upgradeButtons[index];
            upgradeObject.gameObject.SetActive(!OCUIenabled);
        }

        TowerSelectionMenu.instance.towerDetails.SetActive(!OCUIenabled);
    }


    [HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.Initialise))]
    [HarmonyPostfix]
    public static void TowerSelectionMenu_Initialise(TowerSelectionMenu __instance)
    {
        for (var index = 0; index < Pathsliders.Length; index++)
        {
            Pathsliders[index] = null;
        }

        Pathsplusplussliders.Clear();

        var rect = __instance.towerDetails.GetComponent<RectTransform>().rect;

        var parent = __instance.towerDetails.transform.parent;
        OCTogglePanel = parent.parent.gameObject.AddModHelperPanel(
            new Info("ToggleButtonPanel")
            {
                AnchorMin = new Vector2(.75f, .905f),
                AnchorMax = new Vector2(.975f, .945f)
            }, null, RectTransform.Axis.Horizontal, 10);

        OCTogglePanel.AddText(new Info("ToggleButtonLabel", 100), "OC:", 69);

        OCTogglePanel.AddCheckbox(new Info("ToggleButton", 80), OCUIenabled, VanillaSprites.BlueInsertPanelRound,
            new Action<bool>(toggle =>
            {
                OCUIenabled = toggle;
                UpdateVanillaUI();
            }));


        _mainpanel =
            parent.gameObject.AddModHelperPanel(new Info("MergingPanel",
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

        mergebutton = finalselect.AddButton(new Info("MergeButton", 350, 200, new Vector2(.765f, .5f)),
            VanillaSprites.GreenBtnLong, new Action(() =>
            {
                if (InGame.instance.GetCash() < totalcost) return;

                var OCMutator = TowerSelectionMenu.instance.selectedTower.tower.GetMutator("OC")
                    ?.Cast<SupportRemoveFilterOutTag.MutatorTower>();


                if (TowerSelectionMenu.instance.selectedTower.tower.mutators != null)
                    TowerSelectionMenu.instance.selectedTower.tower.RemoveMutatorsById("OC");

                var savedata = selectedtower.GetBaseId() + ":" + selectedtower.tiers[0] + "-" + selectedtower.tiers[1] +
                               "-" +
                               selectedtower.tiers[2];

                if (selectedtower.IsHero())
                    savedata = "hero:" + selectedtower.GetBaseId() + ":" + selectedtower.tiers[0];

                TowerSelectionMenu.instance.selectedTower.tower.AddMutator(
                    new SupportRemoveFilterOutTag.MutatorTower("OC",
                        OCMutator?.removeScriptsWithSupportMutatorId + savedata + ",",
                        null));
                
                foreach(var simulationFix in ModContent.GetContent<SimulationFix>())
                {
                    try
                    {
                        simulationFix.Apply(TowerSelectionMenu.instance.selectedTower, TowerSelectionMenu.instance.selectedTower.tower.towerModel, selectedtower.Duplicate());
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Error("Error during simulationfix: " + simulationFix.Name + ", but thanks to this message, the crisis has been averted :)"); 
                        MelonLogger.Warning(e);
                    }
                }

                if (Main.HasPathsPlusPlus)
                {
                    ApplyPaths();
                }

                var owner = TowerSelectionMenu.instance.selectedTower.tower.owner;

                if (owner == -1)
                    owner = 0;
                else
                    owner--;

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

                TowerSelectionMenu.instance.themeManager.UpdateTheme(TowerSelectionMenu.instance.selectedTower);
                TowerSelectionMenu.instance.UpdateTower();
                TowerSelectionMenu.instance.selectedTower.tower.Hilight();
                TowerSelectionMenu_Show(TowerSelectionMenu.instance);
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

    private static void ApplyPaths()
    {
        if (selectedtower == null) return;
        if (!Main.HasPathsPlusPlus) return;
        foreach (var path in ModContent.GetContent<PathPlusPlus>().Where(p => p.Tower == selectedtower.baseId))
        {
            if (Pathsplusplussliders.All(p => p.Value.name.Split(':')[1] != path.Id)) continue;
            TowerSelectionMenu.instance.selectedTower.tower.SetTier(path.Id,
                (int)Pathsplusplussliders.First(p => p.Value.name.Split(':')[1] == path.Id).Value.CurrentValue);
        }
    }

    [HarmonyPatch(typeof(SupportRemoveFilterOutTag.MutatorTower),
        nameof(SupportRemoveFilterOutTag.MutatorTower.Mutate))]
    [HarmonyPrefix]
    private static bool SupportRemoveFilterOutTag_MutatorTower_Mutate(SupportRemoveFilterOutTag.MutatorTower __instance,
        Model model,
        ref bool __result)
    {
        if (__instance.id != "OC") return true;

        var tower = model.Cast<TowerModel>();

        var split = __instance.removeScriptsWithSupportMutatorId.Split(',');
        foreach (var id in split.Where(x => !string.IsNullOrEmpty(x)))
        {
            TowerModel towerToMerge;
            if (id.StartsWith("hero:"))
            {
                var nonheroid = id.Replace("hero:", "");
                towerToMerge = InGame.instance.GetGameModel()
                    .GetTowerModel(nonheroid.Split(':')[0], int.Parse(nonheroid.Split(':')[1])).Duplicate();
            }
            else
            {
                var tiers = id.Split(':')[1];
                towerToMerge = InGame.instance.GetGameModel().GetTowerModel(id.Split(':')[0],
                        int.Parse(tiers.Split('-')[0]), int.Parse(tiers.Split('-')[1]), int.Parse(tiers.Split('-')[2]))
                    .Duplicate();
            }

            Algorithm.Merge(tower, towerToMerge);            
            //GameModelExporter.Export(tower, "selected_tower.json");
        }

        __result = true;
        return false;
    }

    [HarmonyPatch(typeof(Tower), nameof(Tower.AddMutator))]
    [HarmonyPrefix]
    private static bool Tower_AddMutator(Tower __instance, BehaviorMutator mutator)
    {
        return !(__instance.towerModel.isSubTower && mutator.id == "OC");
    }
}