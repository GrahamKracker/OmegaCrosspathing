using System;
using System.Collections.Generic;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Towers;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using PathsPlusPlus;
using UnityEngine;
using UnityEngine.UI;
using static BTD_Mod_Helper.Api.ModContent;
using Object = UnityEngine.Object;

namespace OmegaCrosspathing;

public class UI
{
    public static readonly Dictionary<TowerSet, string> BackgroundSprites = new()
    {
        { TowerSet.Primary, GameData._instance.towerBackgroundSprites.primarySprite.guidRef },
        { TowerSet.Military, GameData._instance.towerBackgroundSprites.militarySprite.guidRef },
        { TowerSet.Magic, GameData._instance.towerBackgroundSprites.magicSprite.guidRef },
        { TowerSet.Support, GameData._instance.towerBackgroundSprites.supportSprite.guidRef },
        { TowerSet.Hero, GameData._instance.towerBackgroundSprites.heroSprite.guidRef },
    };

    public static ModHelperPanel _mainpanel;
    public static ModHelperScrollPanel towersetselect;
    public static ModHelperScrollPanel pathselect;
    public static ModHelperPanel finalselect;
    public static ModHelperImage towerportrait;
    public static ModHelperText cost;
    public static ModHelperButton mergebutton;
    public static ModHelperText mergetext;
    public static ModHelperText invalidtext;
    public static ModHelperPanel levelselect;
    public static ModHelperSlider levelslider;
    public static ModHelperPanel OCTogglePanel;

    public static readonly Dictionary<string, List<ModHelperButton>> TowerButtonsBySet = new();
    public static readonly Dictionary<ModHelperButton, ModHelperImage> SelectedImages = new();
    
    public static readonly ModHelperSlider[] Pathsliders = new ModHelperSlider[3];
    public static readonly Dictionary<int, ModHelperSlider> Pathsplusplussliders = new();
    
    public static bool ValidTiers(IReadOnlyCollection<int> tiers) =>
        ModHelper.HasMod("UltimateCrosspathing") || tiers.Count(i => i > 2) <= 1 && tiers.Count(i => i > 0) <= 2;
    
    public static void SetUpLevelInput()
    {
        Object.Destroy(levelselect.Background);
        var level = levelselect.AddPanel(new Info("HeroLevelSelect", 290 * 3, 300), VanillaSprites.BrownInsertPanel);
        level.AddText(new Info("HeroLevelSelectText", 290 * 3, 100, new Vector2(.5f, .85f)), "Level", 50f);
        levelslider = level.AddSlider(new Info("HeroLevelSelectInput", 180 * 3, 60, new Vector2(.5f, .35f)), 1, 1, 20,
            1,
            new Vector2(85 * 3, 85), new Action<float>(
                _ =>
                {
                    Main.selectedtower = InGame.instance.GetGameModel()
                        .GetTowerModel(Main.selectedBaseID, (int)levelslider.Slider.value).Duplicate();
                    UpdateBottomBar();
                }
            ));
        Object.Destroy(levelslider.DefaultNotch.gameObject);

        levelslider.Label.transform.localScale = new Vector3(2.45f, 1, 1);
        levelslider.Label.transform.parent.localScale = new Vector3(.35f, 1, 1);

        levelselect.SetActive(false);
    }


    public static void SetUpPathInput()
    {
        for (var i = 1; i <= 3; i++)
        {
            var currentpath = pathselect.AddPanel(new Info($"Path{i}", 290, 300), VanillaSprites.BrownInsertPanel);
            currentpath.AddText(new Info($"Path{i}Text", 290, 100, new Vector2(.5f, .85f)), $"Path {i}", 50f);

            var slider = currentpath.AddSlider(new Info($"Path{i}Input", 180, 60, new Vector2(.5f, .35f)), 0, 0, 5, 1,
                new Vector2(85, 85), new Action<float>(
                    _ =>
                    {
                        Main.selectedtower = InGame.instance.GetGameModel().GetTowerModel(Main.selectedBaseID,
                            (int)Pathsliders[0].Slider.value, (int)Pathsliders[1].Slider.value,
                            (int)Pathsliders[2].Slider.value)?.Duplicate();
                        
                        if (Main.HasPathsPlusPlus)
                            ApplyPathsPlusPlusSliders(Main.selectedtower);
                        
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

    public static void ApplyPathPlusPlus(PathPlusPlus path, int tier, TowerModel? tower)
    {
        if (!Main.HasPathsPlusPlus)
            return;

        if (tower == null)
        {
            return;
        }
        var list = tower.appliedUpgrades.ToList();
        
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
            
            if (!list.Contains(upgrade.Id))
            {
                list.Add(upgrade.Id);
            }
        }      
        
        tower.appliedUpgrades = list.ToArray();
    }


    public static void SetUpTowerButtons()
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
        if (Main.HasPathsPlusPlus)
        {
            pathselect.ScrollRect.StopMovement();
            pathselect.ScrollRect.SetNormalizedPosition(.5f, 0);
            pathselect.ScrollRect.enabled = false;
            foreach (var slider in Pathsplusplussliders.Values)
                Object.Destroy(slider.transform.parent.gameObject);
            Pathsplusplussliders.Clear();
        }
    }
    
    public static void ApplyPathsPlusPlusSliders(TowerModel? tower)
    {
        if (!Main.HasPathsPlusPlus) return;
        if (tower == null) return;
        foreach (var path in GetContent<PathPlusPlus>().Where(p => p.Tower == tower.baseId))
        {
            if (Pathsplusplussliders.All(p => p.Value.name.Split(':')[1] != path.Id)) continue;
            ApplyPathPlusPlus(path, (int)Pathsplusplussliders.First(p => p.Value.name.Split(':')[1] == path.Id).Value.Slider.value, tower);
        }
    }

    public static void GeneratePathsPlusPlusSliders(string baseId)
    {
        if (!Main.HasPathsPlusPlus) return;
        
        DestroyPathsPlusPlusSliders();
        
        if(GetContent<PathPlusPlus>().All(p => p.Tower != baseId))
            return;
        
        foreach (var path in GetContent<PathPlusPlus>().Where(p => p.Tower == baseId))
        {
            var i = path.Path + 1;
            var currentpath = pathselect.AddPanel(new Info($"Path{i}", 290, 300), VanillaSprites.BrownInsertPanel);
            currentpath.AddText(new Info($"Path{i}Text", 290, 100, new Vector2(.5f, .85f)), $"Path {i}", 50f);

            var slider = currentpath.AddSlider(new Info($"Path{i}Input:{path.Id}", 180, 60, new Vector2(.5f, .35f)), 0, 0, 5,
                1,
                new Vector2(85, 85), new Action<float>(
                    _ =>
                    {
                        Main.selectedtower = InGame.instance.GetGameModel().GetTowerModel(Main.selectedBaseID,
                            (int)Pathsliders[0].Slider.value, (int)Pathsliders[1].Slider.value,
                            (int)Pathsliders[2].Slider.value)?.Duplicate();
                        
                        ApplyPathsPlusPlusSliders(Main.selectedtower);
                        
                        UpdateBottomBar();
                    }
                ));

            Object.Destroy(slider.DefaultNotch.gameObject);
            pathselect.AddScrollContent(currentpath);
            Pathsplusplussliders[i] = slider;
        }
        pathselect.ScrollRect.enabled = true;
        pathselect.ScrollRect.horizontalNormalizedPosition = 0f;
    }

    public static void CreateTowerSetButton(string name, string icon, string background,
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
            foreach (var tower in InGame.instance.GetGameModel().towerSet.Select(model => model.GetTower()).Where(tower => tower.GetModTower()?.GetPropertyValue("ModTowerSet") == modtowerset))
            {
                var towerpanel = towersetpanel.AddButton(new Info(tower.name, width, 290),
                    background, new Action(() =>
                    {
                        DestroyPathsPlusPlusSliders();  
                        
                        levelselect.SetActive(false);
                        pathselect.SetActive(true);

                        if (Main.selectedBaseID == tower.name)
                        {
                            HideAllSelected();
                            Main.selectedtower = null;
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
                        Main.selectedBaseID = tower.baseId;

                        Main.selectedtower = InGame.instance.GetGameModel().GetTowerModel(Main.selectedBaseID,
                            (int)Pathsliders[0].Slider.value, (int)Pathsliders[1].Slider.value,
                            (int)Pathsliders[2].Slider.value)?.Duplicate();
                        
                        ApplyPathsPlusPlusSliders(Main.selectedtower);

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
                if (tower.baseId is "BeastHandler" or "Geraldo")
                {
                    continue;
                }

                var towerpanel = towersetpanel.AddButton(new Info(tower.name, width, 290),
                    background, new Action(() =>
                    {
                        if (Main.HasPathsPlusPlus)
                        {
                            GeneratePathsPlusPlusSliders(tower.baseId);
                        }

                        if (Main.selectedBaseID == tower.name)
                        {
                            HideAllSelected();
                            foreach (var slider in Pathsliders)
                                slider.SetCurrentValue(0);
                            foreach (var slider in Pathsplusplussliders.Values)
                                slider.SetCurrentValue(0);
                            levelslider.SetCurrentValue(1);
                            Main.selectedtower = null;
                            UpdateBottomBar();
                            return;
                        }

                        HideAllSelected();

                        Main.selectedBaseID = tower.baseId;

                        if (towerSet == TowerSet.Hero)
                        {
                            Main.selectedtower = InGame.instance.GetGameModel()
                                .GetTowerModel(tower.baseId, (int)levelslider.Slider.value)?.Duplicate();
                            pathselect.SetActive(false);
                            levelselect.SetActive(true);
                        }
                        else
                        {
                            levelselect.SetActive(false);
                            pathselect.SetActive(true);
                            Main.selectedtower = InGame.instance.GetGameModel().GetTowerModel(tower.baseId,
                                (int)Pathsliders[0].Slider.value, (int)Pathsliders[1].Slider.value,
                                (int)Pathsliders[2].Slider.value)?.Duplicate();
                            
                            if (Main.HasPathsPlusPlus)
                                ApplyPathsPlusPlusSliders(Main.selectedtower);
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
    
    public static void UpdateBottomBar()
    {
        if (Main.selectedtower == null || !ValidTiers(Main.selectedtower.tiers.Concat(Pathsplusplussliders.Values.Select(slider => (int)slider.Slider.value)).ToList()))
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

        Main.totalcost = Main.selectedtower.appliedUpgrades.Aggregate(Main.selectedtower.cost,
            (current, up) => current + InGame.instance.GetGameModel().upgradesByName[up].cost);

        if (Main.selectedtower.towerSet == TowerSet.Hero)
        {
            Main.totalcost += Main.selectedtower.appliedUpgrades.Aggregate(Main.selectedtower.cost,
                (current, up) => current + InGame.instance.GetGameModel().upgradesByName[up].xpCost);
        }

        cost.SetText("$" + Main.totalcost.ToString("N0"));
        
        mergebutton.Image.color = mergebutton.Button.colors.normalColor;
        mergetext.Text.color = mergebutton.Button.colors.normalColor;
        towerportrait.Image.enabled = true;
        towerportrait.Image.SetSprite(Main.selectedtower.portrait);
    }
    
    public static void UnlockInputFields()
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

    public static void LockInputFields()
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

    public static void SwitchTowerSetVisibility(string towerSet)
    {
        DestroyPathsPlusPlusSliders();
        LockInputFields();
        foreach (var towerPanel in TowerButtonsBySet[towerSet]) towerPanel.SetActive(!towerPanel.isActiveAndEnabled);

        TowerButtonsBySet[towerSet][0].transform.parent.FindChild(towerSet).FindChild("TowerSetButton")
                .FindChild("ExpandArrow").transform.rotation =
            Quaternion.Euler(0, 0, TowerButtonsBySet[towerSet][0].isActiveAndEnabled ? 270 : 90);
        
    }

    public static void HideAllSelected()
    {
        foreach (var (_, image) in SelectedImages.Where(x => x.Value != null)) image.gameObject.SetActive(false);
        Main.selectedBaseID = "";
        Main.selectedtower = null;
        LockInputFields();
        UpdateBottomBar();
    }
}