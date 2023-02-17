using System;
using System.Collections.Generic;
using BTD_Mod_Helper.Api;
using Il2Cpp;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.GenericBehaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Emissions;
using Il2CppAssets.Scripts.Models.Towers.Filters;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Weapons;
using Il2CppInterop.Runtime;
using Il2CppSystem.Reflection;
using OmegaCrosspathing.MergeFixes;
using Array = Il2CppSystem.Array;
using Exception = System.Exception;
using Math = System.Math;
using Object = Il2CppSystem.Object;
using Type = Il2CppSystem.Type;

namespace OmegaCrosspathing;

public static class Algorithm
{
    public static TowerModel Merge(TowerModel first, TowerModel? second)
    {
        var combined = first.Duplicate();
        if (second == null)
        {
            return combined;
        }
        second = second.Duplicate();
        
        combined.range = (first.range+second.range)/2;
        
        combined.mods = first.mods.Union(second.mods).ToArray();

        var behaviorsInfo = Il2CppType.Of<TowerModel>().GetField("behaviors");
        MergeField(behaviorsInfo, combined, second);
        
        combined.appliedUpgrades = first.appliedUpgrades.Union(second.appliedUpgrades).ToArray();

        
        foreach (var postMergeFix in ModContent.GetContent<PostMergeFix>())
        {
            postMergeFix.Apply(combined);
        }
        
        return combined;
    }
    
    
    
    private static readonly HashSet<string> Multiplicative = new()
    {
        "pierce",
        "range"
    };

    private static readonly HashSet<string> DontMerge = new()
    {
        "animation",
        "offsetX",
        "offsetY",
        "offsetZ",
        "ejectX",
        "ejectY",
        "ejectZ",
        "rate",
        "rateFrames",
        "isPowerTower",
        "isGeraldoItem"
    };

    private static readonly Dictionary<string, string> StringOverrides = new()
    {
        { "fcddee8a92f5d2e4d8605a8924566620", "69bf8d5932f2bea4f9ce36f861240d2e" }, //DartMonkey-340
        { "0ddd8752be0d3554cb0db6abe6686e8e", "69bf8d5932f2bea4f9ce36f861240d2e" } //DartMonkey-043
    };

    private static readonly Dictionary<(string, Type), bool> BetterBooleans = new()
    {
        { ("isActive", Il2CppType.Of<FilterModel>()), false },
        { ("ignoreBlockers", Il2CppType.Of<ProjectileModel>()), true },
        { ("isSharedRangeEnabled", Il2CppType.Of<TargetSupplierModel>()), true },
    };


    internal static Object DeepMerge(Object left, Object right, History history,
        bool shallow = false)
    {
        if (right == null)
        {
            return left;
        }

        if (left == null)
        {
            return right;
        }

        history.Push(left, right);

        try
        {
            // Without this, there is inconsistent handling of the WeaponModels rate and rateFrames fields
            if (left.IsType<WeaponModel>(out var leftWeapon)
                && right.IsType<WeaponModel>(out var rightWeapon)
               )
            {
                leftWeapon.Rate *= rightWeapon.Rate;
            }

            if (left.IsType<Model>(out var leftModel) && right.IsType<Model>(out var rightModel) &&
                !(ModelsAreTheSame(leftModel, rightModel, false) || shallow))
            {
                return MergeDifferentModels(left, right, history);
            }

            var leftFields = left.GetIl2CppType().GetFields();
            foreach (var fieldInfo in leftFields)
            {
                var fieldName = fieldInfo.Name;

                if (DontMerge.Any(s => fieldName.Contains(s))) continue;

                MergeField(fieldInfo, left, right, history, shallow);
            }

            var leftProperties = left.GetIl2CppType().GetProperties();
            foreach (var propertyInfo in leftProperties)
            {
                var propertyName = propertyInfo.Name;

                if (propertyName.ToUpper()[0] == propertyName[0] || DontMerge.Any(s => propertyName.Contains(s)))
                {
                    //skip capitalized ones, they seem like weird ones
                    continue;
                }

                MergeField(propertyInfo, left, right, history, shallow);
            }

            return left;
        }
        finally
        {
            history.Pop();
        }
    }

    internal static void MergeField(MemberInfo memberInfo, Object left, Object right,
        History history = null, bool shallow = false)
    {
        try
        {
            var memberType = memberInfo.Type();
            var leftValue = memberInfo.GetValue(left);
            var rightValue = memberInfo.GetValue(right);

            if (history == null)
            {
                history = new History();
                history.Push(left, right);
            }
            
            
            if (leftValue == null && rightValue == null)
            {
                return;
            }

            if (memberType.IsArray)
            {
                memberInfo.SetValue(left,
                    MergeArray(memberInfo, leftValue, rightValue, history, shallow));
            }
            else if (memberType.IsType<float>())
            {
                memberInfo.SetValue(left, MergeFloat(memberInfo, leftValue, rightValue));
            }
            else if (memberType.IsType<int>())
            {
                memberInfo.SetValue(left, MergeInt(memberInfo, leftValue, rightValue));
            }
            else if (memberType.IsType<bool>())
            {
                memberInfo.SetValue(left, MergeBool(memberInfo, leftValue, rightValue, history));
            }
            else if (memberType.IsType<Model>())
            {
                memberInfo.SetValue(left, DeepMerge(leftValue, rightValue, history));
            }
            else if (memberType.IsType<string>())
            {
                var fieldInfo = memberInfo.TryCast<FieldInfo>();
                if (fieldInfo == null || (!fieldInfo.IsLiteral && !fieldInfo.IsInitOnly))
                {
                    memberInfo.SetValue(left, MergeString(memberInfo, leftValue, rightValue));
                }
            }
            else if (memberType.IsEnum && memberType.Name == nameof(BloonProperties))
            {
                try
                {
                    var leftProps = leftValue.Unbox<int>();
                    var rightProps = rightValue.Unbox<int>();
                    var result = leftProps & rightProps;
                    memberInfo.SetValue(left, result.ToIl2Cpp());
                }
                catch (InvalidCastException)
                {
                    memberInfo.SetValue(left, 0.ToIl2Cpp());
                }
            }
        }
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }
    }

    static Object MergeArray(MemberInfo memberInfo, Object left, Object right, History history, bool shallow = false)
    {
        try
        {
            if (left == null && right != null)
            {
                return right;
            }

            if (right == null && left != null)
            {
                return left;
            }

            var elementType = memberInfo.Type().GetElementType();
            var stuff = new List<Object>();

            var leftStuff = new List<Object>();
            foreach (var o in left.Cast<Array>())
            {
                leftStuff.Add(o);
            }

            var rightStuff = new List<Object>();
            foreach (var o in right.Cast<Array>())
            {
                rightStuff.Add(o);
            }

            


            if (elementType.IsType<Model>())
            {
                foreach (var leftThing in leftStuff)
                {
                    var leftModel = leftThing.Cast<Model>();

                    var rightModel = rightStuff
                        .Select(rightThing => rightThing.Cast<Model>())
                        .FirstOrDefault(model => ModelsAreTheSame(leftModel, model, true));

                    if (rightModel != null && !shallow)
                    {
                        DeepMerge(leftModel, rightModel, history);
                    }

                    stuff.Add(leftModel);


                    if (leftModel.IsType<AttackModel>(out var attackModel)) // Newly added attacks 
                    {
                        var leftTowerModel = history.GetLeft<TowerModel>();
                        var rightTowerModel = history.GetRight<TowerModel>();
                        if (Math.Abs(rightTowerModel.range - attackModel.range) < 1e7 &&
                            leftTowerModel.range > rightTowerModel.range)
                        {
                            attackModel.range = Math.Max(leftTowerModel.range, attackModel.range);
                        }
                    }
                }

                foreach (var rightThing in rightStuff)
                {
                    var rightModel = rightThing.Cast<Model>();

                    var leftModel = leftStuff
                        .Select(leftThing => leftThing.Cast<Model>())
                        .FirstOrDefault(model => ModelsAreTheSame(model, rightModel, true));

                    if (leftModel == null)
                    {
                        stuff.Add(rightModel);
                        var peek = history.left.Peek();
                        if (peek != null && peek.IsType<Model>(out var model))
                        {
                            model.AddChildDependant(rightModel);
                        }


                        if (rightModel.IsType<AttackModel>(out var attackModel)) // Newly added attacks 
                        {
                            var leftTowerModel = history.GetLeft<TowerModel>();
                            var rightTowerModel = history.GetRight<TowerModel>();
                            if (Math.Abs(rightTowerModel.range - attackModel.range) < 1e7 &&
                                leftTowerModel.range > rightTowerModel.range)
                            {
                                attackModel.range = Math.Max(leftTowerModel.range, attackModel.range);
                            }
                        }
                    }
                }
            }
            else if (memberInfo.Name == "collisionPasses")
            {
                stuff = rightStuff.Count > leftStuff.Count ? rightStuff : leftStuff;
            }
            else
            {
                //what to do with arrays that aren't just more models?
                
                stuff = rightStuff;
                
            }


            var result = Array.CreateInstance(memberInfo.Type().GetElementType(), stuff.Count);
            for (var i = 0; i < stuff.Count; i++)
            {
                result.SetValue(stuff[i], i);
            }
            
            return result;
        }
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }

        return null;
    }

    
    private static Object MergeInt(MemberInfo memberInfo, Object leftValue, Object rightValue)
    {
        try
        {
            if (leftValue == null)
            {
                return rightValue;
            }

            if (rightValue == null)
            {
                return leftValue;
            }

            var leftInt = leftValue.Unbox<int>();
            var rightInt = rightValue.Unbox<int>();
            
            if (leftInt != rightInt)
            {
                if (Multiplicative.Any(s => memberInfo.Name.Contains(s)))
                {
                    leftInt = (leftInt + rightInt) / 2; 
                    MelonLogger.Msg($"Multiplicative: {memberInfo.Name} {leftInt} {rightInt}");
                }
                else
                {
                    leftInt += rightInt - leftInt;
                }
            }
            
            return leftInt.ToIl2Cpp();
        }
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }

        return null;
    }
    
    private static Object MergeFloat(MemberInfo memberInfo, Object leftValue, Object rightValue)
    {
        try
        {
            if (leftValue == null)
            {
                return rightValue;
            }

            if (rightValue == null)
            {
                return leftValue;
            }

            var leftFloat = leftValue.Unbox<float>();
            var rightFloat = rightValue.Unbox<float>();

            
            if (Math.Abs(leftFloat - rightFloat) > 1e-7)
            {
                if (Multiplicative.Any(s => memberInfo.Name.Contains(s)))
                {
                    leftFloat = (rightFloat + leftFloat)/2;     
                    MelonLogger.Msg($"Multiplicative: {memberInfo.Name} {leftFloat} {rightFloat}");
                }
                else
                {
                    leftFloat += rightFloat - leftFloat;
                }
            }
            
            

            return leftFloat.ToIl2Cpp();
        }
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }

        return null;
    }

    
    private static Object MergeBool(MemberInfo memberInfo, Object leftValue, Object rightValue, History history)
    {
        try
        {
            if (leftValue == null)
            {
                return rightValue;
            }

            if (rightValue == null)
            {
                return leftValue;
            }

            var fieldName = memberInfo.Name;
            var leftBool = leftValue.Unbox<bool>();
            var rightBool = rightValue.Unbox<bool>();

            if (leftBool != rightBool)
            {
                foreach (var ((name, type), value) in BetterBooleans)
                {
                    if (fieldName.Contains(name as string))
                    {
                        if (history.GetLeft<Model>().GetIl2CppType().IsSubclassOf(type))
                        {
                            return value.ToIl2Cpp();
                        }
                    }
                }
            }

            return leftBool.ToIl2Cpp();
        }
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }

        return null;
    }


    
    private static Object MergeString(MemberInfo memberInfo, Object leftValue, Object rightValue)
    {
        try
        {
            if (leftValue == null)
            {
                return rightValue;
            }

            if (rightValue == null)
            {
                return leftValue;
            }

            var leftString = leftValue.ToString();
            var rightString = rightValue.ToString();


            if (StringOverrides.ContainsKey(leftString) && StringOverrides[leftString] == rightString)
            {
                return leftString;
            }

            if (StringOverrides.ContainsKey(rightString) && StringOverrides[rightString] == leftString)
            {
                return rightString;
            }
            
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to merge strings for {memberInfo.Name}", e);
        }

        return leftValue;
    }

    
    static Object MergeDifferentModels(Object left, Object right, History history)
    {
        try
        {
            var leftModel = left.Cast<Model>();
            var rightModel = right.Cast<Model>();
            
            if (leftModel.IsType<EmissionModel>(out var leftEmission) &&
                rightModel.IsType<EmissionModel>(out var rightEmission)) 
            {
                if (rightModel.IsType<SendToBankModel>())
                {
                    return rightModel;
                }

                var leftCount = GetCountForEmissionModel(left, leftEmission);
                var rightCount = GetCountForEmissionModel(right, rightEmission);

                //Ring of Fire type things
                if (history.GetLeft<TowerModel>()?.GetBehavior<LinkProjectileRadiusToTowerRangeModel>() != null)
                {
                    var leftWeapon = history.GetLeft<WeaponModel>();
                    if (leftWeapon != null)
                    {

                        leftWeapon.projectile.GetDamageModel().damage =
                            (float)Math.Round(
                                leftWeapon.projectile.GetDamageModel().damage * rightCount);

                        history.GetLeft<TowerModel>().GetBehavior<LinkProjectileRadiusToTowerRangeModel>()
                            .projectileModel = leftWeapon.projectile;
                    }
                    
                    return leftModel;
                }

                return rightCount > leftCount ? rightModel : leftModel;
            }

            if (leftModel.IsType<ProjectileModel>(out var leftProjectile) &&
                rightModel.IsType<ProjectileModel>(out var rightProjectile))
            {
                // Try out a shallow merge
                return DeepMerge(leftModel, rightModel, history, true);

            }

            //ModHelper.Msg<UltimateCrosspathingMod>($"Default merge for {leftModel.GetIl2CppType().Name} and {rightModel.GetIl2CppType().Name}");
            return leftModel;
        }
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }

        return null;
    }

    private static int GetCountForEmissionModel(Object methodInfo, EmissionModel emissionModel)
    {
        try
        {
            var count = 1;
            if (emissionModel.IsType<LineProjectileEmissionModel>())
            {
                count = 10000; // Lines should always take priority
            }

            var maybeCount = emissionModel.GetIl2CppType().GetProperty("count");
            if (maybeCount != null)
            {
                var value = maybeCount.GetValue(methodInfo);
                if (value != null) count = value.Unbox<int>();
            }

            var maybeCount2 = emissionModel.GetIl2CppType().GetField("count");
            if (maybeCount2 != null)
            {
                var value = maybeCount2.GetValue(methodInfo);
                if (value != null) count = value.Unbox<int>();
            }

            return count;
        }
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }

        return 1;
    }

    internal static bool ModelsAreTheSame(Model leftModel, Model rightModel, bool array)
    {
        try
        {
            if (leftModel.IsType<AbilityModel>(out var leftAbility) &&
                rightModel.IsType<AbilityModel>(out var rightAbility))
            {
                return leftAbility.displayName == rightAbility.displayName;
            }

            if (leftModel.IsType<TowerBehaviorModel>() && rightModel.IsType<TowerBehaviorModel>() &&
                leftModel.name.Contains("Create") && rightModel.name.Contains("Create") &&
                leftModel.name.Contains("On") && rightModel.name.Contains("On") &&
                leftModel.GetIl2CppType().Name == rightModel.GetIl2CppType().Name)
            {
                return true;
            }

            if (leftModel.IsType<ProjectileModel>() && rightModel.IsType<ProjectileModel>())
            {
                var leftProjectile = leftModel.Cast<ProjectileModel>();
                var rightProjectile = rightModel.Cast<ProjectileModel>();
                return leftProjectile.id == rightProjectile.id || array;
            }

            if (leftModel.IsType<AttackModel>(out var leftAttack) &&
                rightModel.IsType<AttackModel>(out var rightAttack))
            {
                var lineLeft = leftAttack.weapons.Any(weapon => weapon.emission.IsType<LineProjectileEmissionModel>());
                var line = rightAttack.weapons.Any(weapon => weapon.emission.IsType<LineProjectileEmissionModel>());
                if (line != lineLeft)
                {
                    return false;
                }

                if (leftAttack.HasBehavior<RotateToTargetModel>() != rightAttack.HasBehavior<RotateToTargetModel>())
                {
                    return false;
                }

                var leftDisplay = leftAttack.GetBehavior<DisplayModel>();
                var rightDisplay = rightAttack.GetBehavior<DisplayModel>();
                if (leftDisplay != null && rightDisplay != null)
                {
                    if (leftDisplay.display != rightDisplay.display)
                    {
                        return false;
                    }
                }
                else if (!(leftDisplay == null && rightDisplay == null))
                {
                    
                    return false;
                }
            }

            return rightModel.name == leftModel.name
                   && rightModel.GetIl2CppType().Name == leftModel.GetIl2CppType().Name;
        }
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }

        return false;
    }

    private static bool IsType<T>(this Object objectBase)
    {
        return objectBase.GetIl2CppType().IsType<T>();
    }

    private static bool IsType<T>(this Type typ)
    {
        try
        {
            var ty = Il2CppType.From(typeof(T));
            return ty.IsAssignableFrom(typ);
        }
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }

        return false;
    }

    public static Type Type(this MemberInfo memberInfo)
    {
        try
        {
            if (memberInfo.GetIl2CppType().IsType<FieldInfo>())
            {
                return memberInfo.Cast<FieldInfo>().FieldType;
            }

            if (memberInfo.GetIl2CppType().IsType<PropertyInfo>())
            {
                return memberInfo.Cast<PropertyInfo>().PropertyType;
            }
        }
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }

        return null;
    }

    public static Object? GetValue(this MemberInfo memberInfo, Object obj)
    {
        try
        {
            if (memberInfo.GetIl2CppType().IsType<FieldInfo>())
            {
                return memberInfo.Cast<FieldInfo>().GetValue(obj);
            }

            if (memberInfo.GetIl2CppType().IsType<PropertyInfo>())
            {
                return memberInfo.Cast<PropertyInfo>().GetValue(obj);
            }
        }
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }

        return null;
    }

    public static void SetValue(this MemberInfo memberInfo, Object obj, Object newValue)
    {
        try
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
        catch (Exception e)
        {
            MelonLogger.Error(e);
        }
    }
    
    static int Average(int[] values)
    {
        return (int)values.Average();
    }

    public class History
    {
        public readonly Stack<Object> left;
        private readonly Stack<Object> right;

        public History()
        {
            left = new Stack<Object>();
            right = new Stack<Object>();
        }

        public void Push(Object l, Object r)
        {
            left.Push(l);
            right.Push(r);
        }

        public void Pop()
        {
            left.Pop();
            right.Pop();
        }

        public T GetLeft<T>() where T : Object
        {
            return (from o in left where o.IsType<T>() select o.Cast<T>()).FirstOrDefault();
        }

        public T GetRight<T>() where T : Object
        {
            return (from o in right where o.IsType<T>() select o.Cast<T>()).FirstOrDefault();
        }

    }
}
