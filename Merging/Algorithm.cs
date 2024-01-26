using System.Collections.Generic;
using BTD_Mod_Helper.Api;
using Il2Cpp;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.GenericBehaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Filters;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Weapons;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Reflection;
using OmegaCrosspathing.Merging.MergeFixes;
using static OmegaCrosspathing.Merging.Logger;

namespace OmegaCrosspathing.Merging;

public static class Algorithm
{
    public static void Merge(TowerModel first, TowerModel second)
    {
        first.range = Average(first.range, second.range);

        if (second.mods is not null)
        {
            first.mods = first.mods.Union(second.mods).ToArray();
        }

        if (second.targetTypes is not null)
        {
            first.targetTypes = first.targetTypes.Union(second.targetTypes).ToArray();
        }

        first.towerSize = (TowerModel.TowerSize)Math.Max((int)first.towerSize, (int)second.towerSize);

        if (second.appliedUpgrades is not null)
        {
            first.appliedUpgrades = first.appliedUpgrades.Union(second.appliedUpgrades).ToArray();
        }

        foreach (var child in first.behaviors)
        {
            first.RemoveChildDependant(child);
        }
        
        first.behaviors = MergeBehaviors(first.behaviors, second.behaviors);
        
        foreach (var child in first.behaviors)
        {
            first.AddChildDependant(child);
        }

        foreach (var postMergeFix in ModContent.GetContent<PostMergeFix>())
        {
            try
            {
                postMergeFix.Apply(first);
                postMergeFix.Apply(first, second);
            }
            catch (System.Exception e)
            {
                MelonLogger.Error("Error during postmergefix: " + postMergeFix.Name + ", but thanks to this message, the crisis has been averted :)");
                MelonLogger.Warning(e);
            }
        }
    }

    private static readonly HashSet<string> DontMerge =
    [
        "animation",
        "offsetX",
        "offsetY",
        "offsetZ",
        "ejectX",
        "ejectY",
        "ejectZ",
        "rateFrames",
        "isPowerTower",
        "isGeraldoItem"
    ];

    private static readonly Dictionary<Tuple<string, Type>, bool> BoolOverrides = new()
    {
        { new Tuple<string, Type>("isActive", Il2CppType.Of<FilterModel>()), false },
        { new Tuple<string, Type>("ignoreBlockers", Il2CppType.Of<ProjectileModel>()), true },
        { new Tuple<string, Type>("isSharedRangeEnabled", Il2CppType.Of<TargetSupplierModel>()), true },
    };

    public static Il2CppReferenceArray<Model> MergeBehaviors(Il2CppReferenceArray<Model> first, Il2CppReferenceArray<Model> second)
    {
        var firstBehaviors = first.ToList();

        foreach (var secondbehavior in second.Where(x => firstBehaviors.Find(y => IsSameModel(y, x)) is not null))
        {
            Log("Merging same " + secondbehavior.name + " of type " + secondbehavior.GetType().FullName);
            var firstBehavior = firstBehaviors.First(x => IsSameModel(x, secondbehavior));
            foreach (var field in secondbehavior.GetIl2CppType().GetFields().Where(x=> !DontMerge.Any(dontmerge=> x.Name.Contains(dontmerge))))
            {
                MergeTypes(field.FieldType, field, secondbehavior, firstBehavior);
            }

            foreach (var property in secondbehavior.GetIl2CppType().GetProperties().Where(property => property.CanWrite && !DontMerge.Any(dontmerge => property.Name.Contains(dontmerge) && property.Name.ToUpper()[0] != property.Name[0])))
            {
                MergeTypes(property.PropertyType, property, secondbehavior, firstBehavior);
            }
        }
        
        foreach (var behavior in second.Where(x => firstBehaviors.Find(y => IsSameModel(y, x)) is null))
        {
            Log("Adding " + behavior.name + " of type " + behavior.GetType().FullName);
            firstBehaviors.Add(behavior);
        }
        
        return firstBehaviors.ToIl2CppReferenceArray();
    }

    private static bool IsType<T>(this Type typ)
    {
        var ty = Il2CppType.From(typeof(T));
        return ty.IsAssignableFrom(typ);
    }

    private static void MergeTypes(Type? memberType, MemberInfo member, Object firstBehavior, Object secondbehavior)
    {
        Log("Merging " + member.Name + " of type " + memberType?.FullName);

        switch (memberType)
        {
            case not null when memberType.IsType<float>():
                if (secondbehavior.IsType<WeaponModel>(out var secondWeaponModel) && firstBehavior.IsType<WeaponModel>(out var firstWeaponModel) && member.Name == "rate")
                {
                    member.SetValue(firstBehavior,firstWeaponModel.rate * secondWeaponModel.rate);
                    break;
                }
                member.SetValue(firstBehavior,
                    Average(member.GetValue(firstBehavior).Unbox<float>(),
                        member.GetValue(secondbehavior).Unbox<float>()));
                break;
            case not null when memberType.IsType<int>():
                member.SetValue(firstBehavior,
                    (int)Average(member.GetValue(firstBehavior).Unbox<int>(),
                        member.GetValue(secondbehavior).Unbox<int>()));
                break;
            case not null when memberType.IsType<bool>():
                var firstBool = member.GetValue(firstBehavior).Unbox<bool>();
                var secondBool = member.GetValue(secondbehavior).Unbox<bool>();
                foreach (((string? name, var type), bool value) in BoolOverrides)
                {
                    if (member.Name.Contains(name))
                    {
                        if (firstBehavior.GetIl2CppType().IsSubclassOf(type))
                        {
                            member.SetValue(firstBehavior, value.ToIl2Cpp());
                            break;
                        }
                    }
                    else
                    {
                        member.SetValue(firstBehavior, firstBool || secondBool);
                        break;
                    }
                }
                break;
            case not null when memberType.IsType<Il2CppReferenceArray<Model>>():
            {
                var secondArray = member.GetValue(secondbehavior)?.Cast<Il2CppReferenceArray<Model>>();
                var firstArray = member.GetValue(firstBehavior)?.Cast<Il2CppReferenceArray<Model>>();
                if (secondArray is null || firstArray is null)
                {
                    return;
                }

                var mergedArray = MergeBehaviors(firstArray, secondArray);

                var result = Array.CreateInstance(memberType.GetElementType(), mergedArray.Count);
                for (var i = 0; i < mergedArray.Count; i++)
                {
                    result.SetValue(mergedArray[i], i);
                }

                member.SetValue(firstBehavior, result);
                break;
            }
            //todo maybe merge models?
            /*case not null when memberType.IsType<Model>():
            {           
                Log("Merging model " + member.Name + " of type " + memberType.FullName);
                
                var firstModel = member.GetValue(firstBehavior);
                var secondModel = member.GetValue(secondbehavior);
                
                foreach (var field in memberType.GetFields().Where(x=> !DontMerge.Any(dontmerge=> x.Name.Contains(dontmerge))))
                {
                    MergeTypes(field.FieldType, field, firstBehavior, secondbehavior);
                }

                foreach (var property in memberType.GetProperties().Where(property => property.CanWrite && !DontMerge.Any(dontmerge => property.Name.Contains(dontmerge) && property.Name.ToUpper()[0] != property.Name[0])))
                {
                    MergeTypes(property.PropertyType, property, firstBehavior, secondbehavior);
                }
                
                
                break;
            }*/
            case not null when memberType.IsType<BloonProperties>():
            {
                var result = (int)(member.GetValue(firstBehavior).Unbox<BloonProperties>() & member.GetValue(secondbehavior).Unbox<BloonProperties>());
                member.SetValue(firstBehavior, result.ToIl2Cpp());
                break;
            }
            case not null when memberType.IsType<DisplayCategory>():
            {
                var result = (ushort)(member.GetValue(firstBehavior).Unbox<DisplayCategory>() | member.GetValue(secondbehavior).Unbox<DisplayCategory>());
                member.SetValue(firstBehavior, (Object) result);
                break;
            }
        }
    }

    private static Object GetValue(this MemberInfo memberInfo, Object obj)
    {
        if (memberInfo.GetIl2CppType().IsType<FieldInfo>())
        {
            return memberInfo.Cast<FieldInfo>().GetValue(obj);
        }

        return memberInfo.GetIl2CppType().IsType<PropertyInfo>() ? memberInfo.Cast<PropertyInfo>().GetValue(obj) : null!;
    }

    public static void SetValue(this MemberInfo memberInfo, Object obj, Object newValue)
    {
        if (memberInfo.GetIl2CppType().IsType<FieldInfo>())
        {
            memberInfo.Cast<FieldInfo>().SetValue(obj, newValue);
        }
        if (memberInfo.GetIl2CppType().IsType<PropertyInfo>())
        {
            memberInfo.Cast<PropertyInfo>().SetValue(obj, newValue, null);
        }
    }

    private static bool IsSameModel(Model first, Model second)
    {
        if (first.IsType<AbilityModel>(out var firstAbility) && second.IsType<AbilityModel>(out var secondAbility))
        {
            return firstAbility.displayName == secondAbility.displayName;
        }
        
        if (first.IsType<ProjectileModel>(out var firstProjectile) && second.IsType<ProjectileModel>(out var secondProjectile))
        {
            return firstProjectile.id == secondProjectile.id || firstProjectile.display?.guidRef == secondProjectile.display?.guidRef;
        }
        
        if(first.name.Contains("Create") && second.name.Contains("Create") && first.name.Contains("On") && second.name.Contains("On") && first.GetIl2CppType().Name == second.GetIl2CppType().Name)
            return true;
        
        if (first.IsType<AttackModel>(out var firstAttack) && second.IsType<AttackModel>(out var secondAttack))
        {
            var firstWeapon = firstAttack.weapons.FirstOrDefault();
            var secondWeapon = secondAttack.weapons.FirstOrDefault();
            if (firstWeapon == null || secondWeapon == null)
            {
                return false;
            }
            if (firstWeapon.projectile.id == secondWeapon.projectile.id)
            {
                return true;
            }
            
            var leftDisplay = firstAttack.GetBehavior<DisplayModel>();
            var rightDisplay = secondAttack.GetBehavior<DisplayModel>();
            if (leftDisplay == null && rightDisplay != null)
            {
                return false;
            }
            if (leftDisplay != null && rightDisplay == null)
            {
                return false;
            }
            if (leftDisplay == null && rightDisplay == null)
            {
                return firstAttack.behaviors.Count == secondAttack.behaviors.Count;
            }
            return leftDisplay.display.guidRef == rightDisplay.display.guidRef;
        }
        
        
        return first.name == second.name && first.GetIl2CppType().Name == second.GetIl2CppType().Name;
    }

    private static float Average(float? first, float? second)
    {
        if (first is null && second is not null)
            return second.Value;
        if (first is not null && second is null)
            return first.Value;
        if (first is null && second is null)
            return 0;

        return (first!.Value + second!.Value) / 2;
    }
}

public static class Logger
{
    public static void Log(string message)
    {
        MelonLogger.Msg(message);
    }
}