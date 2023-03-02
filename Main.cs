using System;
using System.Collections.Generic;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Towers;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppAssets.Scripts.Utils;
using OmegaCrosspathing;
using OmegaCrosspathing.SimulationFixes;
using UnityEngine;
using UnityEngine.UI;
using static BTD_Mod_Helper.Api.ModContent;
using Main = OmegaCrosspathing.Main;
using Object = UnityEngine.Object;
using TowerSet = Il2CppAssets.Scripts.Models.TowerSets.TowerSet;

[assembly: MelonInfo(typeof(Main), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace OmegaCrosspathing;

[HarmonyPatch]
public partial class Main : BloonsTD6Mod
{
    private static readonly Dictionary<TowerSet, string> backgroundSprites = new()
    {
        { TowerSet.Primary, GameData._instance.towerBackgroundSprites.primarySprite.guidRef },
        { TowerSet.Military, GameData._instance.towerBackgroundSprites.militarySprite.guidRef },
        { TowerSet.Magic, GameData._instance.towerBackgroundSprites.magicSprite.guidRef },
        { TowerSet.Support, GameData._instance.towerBackgroundSprites.supportSprite.guidRef },
    };

    private static ModHelperPanel _mainpanel;

    private static float totalcost;


    private static ModHelperScrollPanel towersetselect;
    private static ModHelperPanel pathselect;
    private static ModHelperPanel finalselect;
    private static ModHelperImage towerportrait;
    private static ModHelperText cost;
    private static ModHelperButton mergebutton;
    private static ModHelperText mergetext;
    private static ModHelperText invalidtext;

    private static readonly Dictionary<string, List<ModHelperButton>> TowerButtonsBySet = new();
    private static readonly Dictionary<ModHelperButton, ModHelperImage> SelectedImages = new();

    private static string towerselected = "";
    private static TowerModel? selectedtower;

    private static readonly ModHelperSlider[] Pathsliders = new ModHelperSlider[3];

    private static void SetUpPathInput()
    {
        Object.Destroy(pathselect.Background);
        for (var i = 1; i <= 3; i++)
        {
            var currentpath = pathselect.AddPanel(new Info($"Path{i}", 290, 300), VanillaSprites.BrownInsertPanel);
            currentpath.AddText(new Info($"Path{i}Text", 290, 100, new Vector2(.5f, .85f)), $"Path {i}", 50f);

            var slider = currentpath.AddSlider(new Info($"Path{i}Input", 180, 60, new Vector2(.5f, .35f)), 0, 0, 5, 1,
                new Vector2(85, 85), new Action<float>(
                    _ =>
                    {
                        selectedtower = InGame.instance.GetGameModel().GetTowerModel(towerselected,
                            (int)Pathsliders[0].CurrentValue, (int)Pathsliders[1].CurrentValue,
                            (int)Pathsliders[2].CurrentValue);
                        UpdateBottomBar();
                    }
                ));

            Object.Destroy(slider.DefaultNotch.gameObject);

            Pathsliders[i - 1] = slider;
        }

        LockInputFields();
    }

    private static void SetUpTowerButtons()
    {
        List<TowerSet> towersets = new();
        
        towersets.Add(TowerSet.Primary);
        towersets.Add(TowerSet.Military);
        towersets.Add(TowerSet.Magic);
        towersets.Add(TowerSet.Support);


        foreach (var towerSet in towersets)
            CreateTowerSetButton(towerSet.ToString(),
                "MainMenuUiAtlas[" + towerSet + "Btn]", backgroundSprites[towerSet], towerSet: towerSet);

        foreach (var modtowerset in GetContent<ModTowerSet>())
            CreateTowerSetButton(modtowerset.DisplayName, modtowerset.ButtonReference.guidRef,
                modtowerset.ContainerReference.guidRef, modtowerset);
    }

    private static void CreateTowerSetButton(string name, string icon, string background,
        ModTowerSet modtowerset = null, TowerSet towerSet = TowerSet.None)
    {
        const int width = 250;
        var towersetpanel = towersetselect.AddPanel(new Info(name, width, 300));
        Object.Destroy(towersetpanel.Background);

        towersetpanel.AddText(new Info("TowerSetName", 0, -17.5f, width, 100, new Vector2(.5f, .95f)),
            name, 50);

        towersetselect.AddScrollContent(towersetpanel);

        var towersinset = new List<ModHelperButton>();


        if (modtowerset != null)
            foreach (var tower in InGame.instance.GetGameModel().towerSet.Select(model => model.GetTower())
                         .Where(tower => tower.GetModTower()?.GetPropertyValue("ModTowerSet") == modtowerset))
            {
                var towerpanel = towersetpanel.AddButton(new Info(tower.name, width, 290),
                    background, new Action(() =>
                    {
                        if (towerselected == tower.name)
                        {
                            HideAllSelected();
                            selectedtower = null;
                            Pathsliders[0].SetCurrentValue(0);
                            Pathsliders[1].SetCurrentValue(0);
                            Pathsliders[2].SetCurrentValue(0);
                            UpdateBottomBar();
                            return;
                        }

                        HideAllSelected();

                        towersetpanel.transform.parent.FindChild(tower.name).FindChild("TowerSelected").gameObject
                            .SetActive(true);
                        towerselected = tower.baseId;

                        selectedtower = InGame.instance.GetGameModel().GetTowerModel(towerselected,
                            (int)Pathsliders[0].CurrentValue, (int)Pathsliders[1].CurrentValue,
                            (int)Pathsliders[2].CurrentValue);

                        UpdateBottomBar();
                        UnlockInputFields();
                    }));

                towerpanel.AddImage(new Info("TowerButton", width, width, new Vector2(.5f, .55f)),
                    tower.portrait.guidRef);

                SelectedImages[towerpanel] = towerpanel.AddImage(new Info("TowerSelected", width + 80, 370),
                    VanillaSprites.SmallSquareGlowOutline);

                towersetselect.AddScrollContent(towerpanel);
                towersinset.Add(towerpanel);
                towerpanel.SetActive(false);
            }
        else
            foreach (var tower in InGame.instance.GetGameModel().towerSet.Select(model => model.GetTower())
                         .Where(tower => tower.towerSet == towerSet))
            {
                var towerpanel = towersetpanel.AddButton(new Info(tower.name, width, 290),
                     background, new Action(() =>
                    {
                        if (towerselected == tower.name)
                        {
                            HideAllSelected();
                            selectedtower = null;
                            Pathsliders[0].SetCurrentValue(0);
                            Pathsliders[1].SetCurrentValue(0);
                            Pathsliders[2].SetCurrentValue(0);
                            UpdateBottomBar();
                            return;
                        }

                        HideAllSelected();

                        towersetpanel.transform.parent.FindChild(tower.name).FindChild("TowerSelected").gameObject
                            .SetActive(true);
                        towerselected = tower.baseId;

                        selectedtower = InGame.instance.GetGameModel().GetTowerModel(towerselected,
                            (int)Pathsliders[0].CurrentValue, (int)Pathsliders[1].CurrentValue,
                            (int)Pathsliders[2].CurrentValue);

                        UpdateBottomBar();
                        UnlockInputFields();
                    }));

                towerpanel.AddImage(new Info("TowerButton", width, width, new Vector2(.5f, .55f)),
                    tower.portrait.guidRef);

                SelectedImages[towerpanel] = towerpanel.AddImage(new Info("TowerSelected", width + 80, 370),
                    VanillaSprites.SmallSquareGlowOutline);

                towersetselect.AddScrollContent(towerpanel);
                towersinset.Add(towerpanel);
                towerpanel.SetActive(false);
            }


        var towersetButton = towersetpanel.AddButton(new Info("TowerSetButton", InfoPreset.FillParent),
            background, new Action(() =>
            {
                HideAllSelected();
                SwitchTowerSetVisibility(name);
            }));


        towersetButton.AddImage(new Info("TowerSetImage", 0, -20f, 230),
            icon);

        towersetButton.AddText(new Info("TowerSetName", 0, -17.5f, width, 100, new Vector2(.5f, .95f)),
            name, 50);

        towersetButton.AddImage(new Info("ExpandArrow", 100, 100, new Vector2(.925f, .5f)),
                GetSpriteReference<Main>("RoundSetSwitcherButton").guidRef).transform.rotation =
            Quaternion.Euler(0, 0, 90);


        TowerButtonsBySet[name] = towersinset;
    }

    private static void UpdateBottomBar()
    {
        if (selectedtower == null)
        {
            invalidtext.gameObject.SetActive(true);
            towerportrait.Image.enabled = false;
            cost.gameObject.SetActive(false);
            cost.SetText("");
            mergebutton.Button.interactable = false;
            mergebutton.Image.color = mergebutton.Button.colors.disabledColor;
            mergetext.Text.color = mergebutton.Button.colors.disabledColor;
            return;
        }

        invalidtext.gameObject.SetActive(false);
        cost.gameObject.SetActive(true);
        mergebutton.Button.interactable = true;

        totalcost = selectedtower.appliedUpgrades.Aggregate(selectedtower.cost,
            (current, up) => current + InGame.instance.GetGameModel().upgradesByName[up].cost);
        cost.SetText("$" + totalcost.ToString("N0"));


        mergebutton.Image.color = mergebutton.Button.colors.normalColor;
        mergetext.Text.color = mergebutton.Button.colors.normalColor;
        towerportrait.Image.enabled = true;
        towerportrait.Image.SetSprite(selectedtower.portrait.guidRef);
    }

    private static void UnlockInputFields()
    {
        foreach (var inputField in Pathsliders)
        {
            inputField.Slider.interactable = true;

            inputField.Label.transform.parent.gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
            inputField.Slider.transform.FindChild("Background").gameObject.GetComponent<Image>().color =
                new Color(0.219f, 0.125f, 0.058f);
        }
    }

    private static void LockInputFields()
    {
        foreach (var inputField in Pathsliders)
        {
            inputField.Slider.interactable = false;

            Pathsliders[0].SetCurrentValue(0);
            Pathsliders[1].SetCurrentValue(0);
            Pathsliders[2].SetCurrentValue(0);

            inputField.Label.transform.parent.gameObject.GetComponent<Image>().color =
                new Color(0.784f, 0.784f, 0.784f, 0.502f);

            inputField.Slider.transform.FindChild("Background").gameObject.GetComponent<Image>().color =
                new Color(0.171696f, 0.098f, 0.045472f, 0.502f);
        }
    }

    private static void SwitchTowerSetVisibility(string towerSet)
    {
        LockInputFields();
        foreach (var towerPanel in TowerButtonsBySet[towerSet]) towerPanel.SetActive(!towerPanel.isActiveAndEnabled);

        TowerButtonsBySet[towerSet][0].transform.parent.FindChild(towerSet).FindChild("TowerSetButton")
                .FindChild("ExpandArrow").transform.rotation =
            Quaternion.Euler(0, 0, TowerButtonsBySet[towerSet][0].isActiveAndEnabled ? 270 : 90);
    }

    private static void HideAllSelected()
    {
        foreach (var (_, image) in SelectedImages.Where(x => x.Value != null)) image.gameObject.SetActive(false);

        towerselected = "";
        selectedtower = null;
        LockInputFields();
        UpdateBottomBar();
    }

    public override void OnTowerSaved(Tower tower, TowerSaveDataModel saveData)
    {
        var OCMutator = tower.mutators.FirstOrDefault(x => x.mutator.id.Contains("OC:"));
        if (OCMutator != null)
        {
            saveData.metaData["OC"] = OCMutator.mutator.id;
        }
    }

    public override void OnTowerLoaded(Tower tower, TowerSaveDataModel saveData)
    {
        base.OnTowerLoaded(tower, saveData);

        if (!saveData.metaData.ContainsKey("OC")) return;
        
        var OCMutator = saveData.metaData["OC"];
        
        tower.AddMutator(new BehaviorMutator(OCMutator));
        var split = OCMutator.Split(',');

        for (var i = 1; i < split.Length; i++)
        {
            var newTower = Algorithm.Merge(tower.towerModel, InGame.instance.GetGameModel().GetTowerFromId(split[i]));
            tower.SetTowerModel(newTower);
            tower.towerModel = newTower;
            tower.rootModel = newTower;
            
            foreach (var simulationFix in ModContent.GetContent<SimulationFix>())
            {
                simulationFix.Apply(tower.GetTowerToSim());
            }
        }
    }
}