using System;
using System.Collections.Generic;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Towers;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using OmegaCrosspathing;
using PathsPlusPlus;
using UnityEngine;
using UnityEngine.UI;
using static BTD_Mod_Helper.Api.ModContent;
using Main = OmegaCrosspathing.Main;
using Object = UnityEngine.Object;
using TowerSet = Il2CppAssets.Scripts.Models.TowerSets.TowerSet;

[assembly: MelonInfo(typeof(Main), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
[assembly: MelonOptionalDependencies("PathsPlusPlus")]

namespace OmegaCrosspathing;

[HarmonyPatch]
public partial class Main : BloonsTD6Mod
{
    private static readonly Dictionary<TowerSet, string> BackgroundSprites = new()
    {
        { TowerSet.Primary, GameData._instance.towerBackgroundSprites.primarySprite.guidRef },
        { TowerSet.Military, GameData._instance.towerBackgroundSprites.militarySprite.guidRef },
        { TowerSet.Magic, GameData._instance.towerBackgroundSprites.magicSprite.guidRef },
        { TowerSet.Support, GameData._instance.towerBackgroundSprites.supportSprite.guidRef },
        { TowerSet.Hero, GameData._instance.towerBackgroundSprites.heroSprite.guidRef },
    };

    private static ModHelperPanel _mainpanel;

    private static float totalcost;
    
    public static bool HasPathsPlusPlus => ModHelper.HasMod("PathsPlusPlus");
    private static ModHelperScrollPanel towersetselect;
    private static ModHelperScrollPanel pathselect;
    private static ModHelperPanel finalselect;
    private static ModHelperImage towerportrait;
    private static ModHelperText cost;
    private static ModHelperButton mergebutton;
    private static ModHelperText mergetext;
    private static ModHelperText invalidtext;
    private static ModHelperPanel levelselect;
    private static ModHelperSlider levelslider;

    private static readonly Dictionary<string, List<ModHelperButton>> TowerButtonsBySet = new();
    private static readonly Dictionary<ModHelperButton, ModHelperImage> SelectedImages = new();

    private static string selectedBaseID = "";
    private static TowerModel? selectedtower;

    private static readonly ModHelperSlider[] Pathsliders = new ModHelperSlider[3];
    private static readonly Dictionary<int, ModHelperSlider> Pathsplusplussliders = new();

    public override bool CheatMod => false;

    private static void SetUpLevelInput()
    {
        Object.Destroy(levelselect.Background);
        var level = levelselect.AddPanel(new Info("HeroLevelSelect", 290 * 3, 300), VanillaSprites.BrownInsertPanel);
        level.AddText(new Info("HeroLevelSelectText", 290 * 3, 100, new Vector2(.5f, .85f)), "Level", 50f);
        levelslider = level.AddSlider(new Info("HeroLevelSelectInput", 180 * 3, 60, new Vector2(.5f, .35f)), 1, 1, 20,
            1,
            new Vector2(85 * 3, 85), new Action<float>(
                _ =>
                {
                    selectedtower = InGame.instance.GetGameModel()
                        .GetTowerModel(selectedBaseID, (int)levelslider.CurrentValue);
                    UpdateBottomBar();
                }
            ));
        Object.Destroy(levelslider.DefaultNotch.gameObject);

        levelslider.Label.transform.localScale = new Vector3(2.45f, 1, 1);
        levelslider.Label.transform.parent.localScale = new Vector3(.35f, 1, 1);

        levelselect.SetActive(false);
    }


    private static void SetUpPathInput()
    {
        for (var i = 1; i <= 3; i++)
        {
            var currentpath = pathselect.AddPanel(new Info($"Path{i}", 290, 300), VanillaSprites.BrownInsertPanel);
            currentpath.AddText(new Info($"Path{i}Text", 290, 100, new Vector2(.5f, .85f)), $"Path {i}", 50f);

            var slider = currentpath.AddSlider(new Info($"Path{i}Input", 180, 60, new Vector2(.5f, .35f)), 0, 0, 5, 1,
                new Vector2(85, 85), new Action<float>(
                    _ =>
                    {
                        selectedtower = InGame.instance.GetGameModel().GetTowerModel(selectedBaseID,
                            (int)Pathsliders[0].CurrentValue, (int)Pathsliders[1].CurrentValue,
                            (int)Pathsliders[2].CurrentValue);
                        UpdateBottomBar();
                    }
                ));

            Object.Destroy(slider.DefaultNotch.gameObject);
            pathselect.AddScrollContent(currentpath);
            Pathsliders[i - 1] = slider;
        }
        pathselect.ScrollRect.StopMovement();
        pathselect.ScrollRect.enabled = false;
    }

    public static void ApplyPathPlusPlus(PathPlusPlus path, int tier, ref TowerModel tower)
    {
        var list = tower.appliedUpgrades.ToList();
        foreach (var pathPlusPlus in GetContent<PathPlusPlus>().SelectMany(p=> p.Upgrades))
        {
            list.RemoveAll(p => p == pathPlusPlus.Id);
        }
        tower.appliedUpgrades = list.ToArray();
        
        tower.tier = Math.Max(tower.tier, tier);
        for (var i = 0; i < tier; i++)
        {
            var upgrade = path.Upgrades[i];
            upgrade.ApplyUpgrade(tower);
            upgrade.ApplyUpgrade(tower, tier);
            if (upgrade.IsHighestUpgrade(tower))
            {
                tower.portrait = upgrade.PortraitReference;
            }
            
            if (!tower.appliedUpgrades.Contains(upgrade.Id))
            {
                tower.appliedUpgrades = tower.appliedUpgrades.AddTo(upgrade.Id);
            }
        }
    }


    private static void SetUpTowerButtons()
    {
        foreach (var towerSet in BackgroundSprites.Keys)
        {
            var icon = "MainMenuUiAtlas[" + towerSet + "Btn]";
            var name = towerSet.ToString();

            if (towerSet == TowerSet.Hero)
            {
                icon = VanillaSprites.HeroIconQuincy;
                name = "Heroes";
            }

            CreateTowerSetButton(name,
                icon, BackgroundSprites[towerSet], towerSet: towerSet);
        }

        foreach (var modtowerset in GetContent<ModTowerSet>())
            CreateTowerSetButton(modtowerset.DisplayName, modtowerset.ButtonReference.guidRef,
                modtowerset.ContainerReference.guidRef, modtowerset);
    }

    public static void DestroyPathsPlusPlusSliders()
    {
        if (HasPathsPlusPlus)
        {
            pathselect.ScrollRect.StopMovement();
            pathselect.ScrollRect.SetNormalizedPosition(.5f, 0);
            pathselect.ScrollRect.enabled = false;
            foreach (var slider in Pathsplusplussliders.Values)
                Object.Destroy(slider.transform.parent.gameObject);
            Pathsplusplussliders.Clear();
        }
    }

    public static void GeneratePathsPlusPlusSliders(string baseId)
    {
        if (!HasPathsPlusPlus) return;
        
        DestroyPathsPlusPlusSliders();

        foreach (var path in GetContent<PathPlusPlus>().Where(p => p.Tower == baseId))
        {
            var i = path.Path + 1;
            var currentpath = pathselect.AddPanel(new Info($"Path{i}", 290, 300), VanillaSprites.BrownInsertPanel);
            currentpath.AddText(new Info($"Path{i}Text", 290, 100, new Vector2(.5f, .85f)), $"Path {i}", 50f);

            var slider = currentpath.AddSlider(new Info($"Path{i}Input", 180, 60, new Vector2(.5f, .35f)), 0, 0, 5,
                1,
                new Vector2(85, 85), new Action<float>(
                    tier =>
                    {
                        selectedtower = InGame.instance.GetGameModel().GetTowerModel(selectedBaseID,
                            (int)Pathsliders[0].CurrentValue, (int)Pathsliders[1].CurrentValue,
                            (int)Pathsliders[2].CurrentValue);
                        ApplyPathPlusPlus(path,(int)tier, ref selectedtower);
                        UpdateBottomBar();
                    }
                ));

            Object.Destroy(slider.DefaultNotch.gameObject);
            pathselect.AddScrollContent(currentpath);
            Pathsplusplussliders[i] = slider;
                
            pathselect.ScrollRect.enabled = true;
            pathselect.ScrollRect.horizontalNormalizedPosition = 0f;
        }
    }

    private static void CreateTowerSetButton(string name, string icon, string background,
        ModTowerSet? modtowerset = null, TowerSet towerSet = TowerSet.None)
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
                        DestroyPathsPlusPlusSliders();  
                        
                        levelselect.SetActive(false);
                        pathselect.SetActive(true);

                        if (selectedBaseID == tower.name)
                        {
                            HideAllSelected();
                            selectedtower = null;
                            foreach (var slider in Pathsliders)
                                slider.SetCurrentValue(0);
                            foreach (var slider in Pathsplusplussliders.Values)
                                slider.SetCurrentValue(0);
                            UpdateBottomBar();
                            return;
                        }

                        HideAllSelected();

                        towersetpanel.transform.parent.FindChild(tower.name).FindChild("TowerSelected").gameObject
                            .SetActive(true);
                        selectedBaseID = tower.baseId;

                        selectedtower = InGame.instance.GetGameModel().GetTowerModel(selectedBaseID,
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
            foreach (var tower in InGame.instance.GetGameModel().GetDescendants<TowerDetailsModel>().ToList()
                         .Select(model => model.GetTower()).Where(tower => tower.towerSet == towerSet))
            {
                if (tower.baseId == "BeastHandler")
                {
                    continue;
                }

                var towerpanel = towersetpanel.AddButton(new Info(tower.name, width, 290),
                    background, new Action(() =>
                    {
                        if (HasPathsPlusPlus)
                        {
                            GeneratePathsPlusPlusSliders(tower.baseId);
                        }

                        if (selectedBaseID == tower.name)
                        {
                            HideAllSelected();
                            foreach (var slider in Pathsliders)
                                slider.SetCurrentValue(0);
                            foreach (var slider in Pathsplusplussliders.Values)
                                slider.SetCurrentValue(0);
                            levelslider.SetCurrentValue(1);
                            selectedtower = null;
                            UpdateBottomBar();
                            return;
                        }

                        HideAllSelected();

                        selectedBaseID = tower.baseId;

                        if (towerSet == TowerSet.Hero)
                        {
                            selectedtower = InGame.instance.GetGameModel()
                                .GetTowerModel(tower.baseId, (int)levelslider.CurrentValue);
                            pathselect.SetActive(false);
                            levelselect.SetActive(true);
                        }
                        else
                        {
                            levelselect.SetActive(false);
                            pathselect.SetActive(true);
                            selectedtower = InGame.instance.GetGameModel().GetTowerModel(tower.baseId,
                                (int)Pathsliders[0].CurrentValue, (int)Pathsliders[1].CurrentValue,
                                (int)Pathsliders[2].CurrentValue);
                        }

                        towersetpanel.transform.parent.FindChild(tower.name).FindChild("TowerSelected").gameObject
                            .SetActive(true);

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

    private static bool ValidTiers(IReadOnlyCollection<int> tiers) =>
        ModHelper.HasMod("UltimateCrosspathing") || tiers.Count(i => i > 2) <= 1 && tiers.Count(i => i > 0) <= 2;
    
    
    private static void UpdateBottomBar()
    {
        
        if (selectedtower == null || !ValidTiers(selectedtower.tiers.Concat(Pathsplusplussliders.Values.Select(slider => (int)slider.CurrentValue)).ToList()))
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
        
        if (Pathsplusplussliders.Values.All(x => x.CurrentValue == 0) && selectedtower.tiers.All(x => x == 0))
        {
            selectedtower.portrait = CreateSpriteReference(Game.instance.model.GetTower(selectedtower.baseId).portrait.guidRef);
        }
        
        invalidtext.gameObject.SetActive(false);
        cost.gameObject.SetActive(true);
        mergebutton.Button.interactable = true;

        totalcost = selectedtower.appliedUpgrades.Aggregate(selectedtower.cost,
            (current, up) => current + InGame.instance.GetGameModel().upgradesByName[up].cost);

        if (selectedtower.towerSet == TowerSet.Hero)
        {
            totalcost += selectedtower.appliedUpgrades.Aggregate(selectedtower.cost,
                (current, up) => current + InGame.instance.GetGameModel().upgradesByName[up].xpCost);
        }

        cost.SetText("$" + totalcost.ToString("N0"));
        
        mergebutton.Image.color = mergebutton.Button.colors.normalColor;
        mergetext.Text.color = mergebutton.Button.colors.normalColor;
        towerportrait.Image.enabled = true;
        towerportrait.Image.SetSprite(selectedtower.portrait);
    }

    public override void OnTowerSelected(Tower tower)
    {
        PortraitManager.SetUpPortrait(tower.GetTowerToSim());
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

        foreach (var inputField in Pathsplusplussliders.Values)
        {
            inputField.Slider.interactable = true;

            inputField.Label.transform.parent.gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
            inputField.Slider.transform.FindChild("Background").gameObject.GetComponent<Image>().color =
                new Color(0.219f, 0.125f, 0.058f);
        }

        levelslider.Slider.interactable = true;

        levelslider.Label.transform.parent.gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        levelslider.Slider.transform.FindChild("Background").gameObject.GetComponent<Image>().color =
            new Color(0.219f, 0.125f, 0.058f);
    }

    private static void LockInputFields()
    {
        foreach (var inputField in Pathsliders)
        {
            inputField.Slider.interactable = false;

            inputField.SetCurrentValue(0);

            inputField.Label.transform.parent.gameObject.GetComponent<Image>().color =
                new Color(0.784f, 0.784f, 0.784f, 0.502f);

            inputField.Slider.transform.FindChild("Background").gameObject.GetComponent<Image>().color =
                new Color(0.171696f, 0.098f, 0.045472f, 0.502f);
        }

        foreach (var inputField in Pathsplusplussliders.Values)
        {
            inputField.Slider.interactable = false;

            inputField.SetCurrentValue(0);

            inputField.Label.transform.parent.gameObject.GetComponent<Image>().color =
                new Color(0.784f, 0.784f, 0.784f, 0.502f);

            inputField.Slider.transform.FindChild("Background").gameObject.GetComponent<Image>().color =
                new Color(0.171696f, 0.098f, 0.045472f, 0.502f);
        }


        levelslider.Slider.interactable = false;

        levelslider.Label.transform.parent.gameObject.GetComponent<Image>().color =
            new Color(0.784f, 0.784f, 0.784f, 0.502f);

        levelslider.Slider.transform.FindChild("Background").gameObject.GetComponent<Image>().color =
            new Color(0.171696f, 0.098f, 0.045472f, 0.502f);

        levelslider.SetCurrentValue(1);
    }

    private static void SwitchTowerSetVisibility(string towerSet)
    {
        DestroyPathsPlusPlusSliders();
        LockInputFields();
        foreach (var towerPanel in TowerButtonsBySet[towerSet]) towerPanel.SetActive(!towerPanel.isActiveAndEnabled);

        TowerButtonsBySet[towerSet][0].transform.parent.FindChild(towerSet).FindChild("TowerSetButton")
                .FindChild("ExpandArrow").transform.rotation =
            Quaternion.Euler(0, 0, TowerButtonsBySet[towerSet][0].isActiveAndEnabled ? 270 : 90);
        
    }

    private static void HideAllSelected()
    {
        foreach (var (_, image) in SelectedImages.Where(x => x.Value != null)) image.gameObject.SetActive(false);
        selectedBaseID = "";
        selectedtower = null;
        LockInputFields();
        UpdateBottomBar();
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