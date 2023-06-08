using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Weapons.Behaviors;

namespace OmegaCrosspathing.MergeFixes;

public class FixAirUnits : PostMergeFix
{
    public override void Apply(TowerModel model)
    {
        try
        {
            if (!model.HasBehavior<AirUnitModel>(out var airunit))
                return;
            
            model.towerSelectionMenuThemeId = "HeliPilot";
            
            var airunits = model.GetBehaviors<AirUnitModel>();

            if (airunits.Count > 1)
            {
                Algorithm.DeepMerge(airunit, airunits[1], new Algorithm.History());
                model.RemoveBehavior(airunits[1]);
            }
            
            foreach (var attackModel in model.GetAttackModels().Where(x=>x.HasBehavior<FireFromAirUnitModel>()))
            {
                foreach (var weaponModel in attackModel.weapons)
                {
                    if (!weaponModel.HasBehavior<FireFromAirUnitModel>()) continue;
                    MelonLogger.Msg("Fixing FireFromAirUnitModel for " + model.name);
                    
                    if (model.HasBehavior<AttackAirUnitModel>(out var attackAirUnitModel))
                    {
                        attackAirUnitModel.AddBehavior(weaponModel);
                    }
                    
                    if (attackModel.HasBehavior<FollowTouchSettingModel>())
                    {
                        var followTouchSettingModel = attackModel.GetBehavior<FollowTouchSettingModel>();
                        followTouchSettingModel.isSelectable = false;
                    }
                        
                    attackModel.RemoveBehavior<PatrolPointsSettingModel>();
                        
                    attackModel.RemoveBehavior<PatrolPointsSettingModel>();
                    
                    if (attackModel.HasBehavior<PursuitSettingModel>())
                    {
                        var pursuitSettingModel = attackModel.GetBehavior<PursuitSettingModel>();
                        pursuitSettingModel.isSelectable = false;
                        if (attackModel.HasBehavior<FollowTouchSettingModel>())
                        {
                            attackModel.RemoveBehavior<FollowTouchSettingModel>();
                        }
                    }
                    if (attackModel.HasBehavior<PursuitSettingCustomModel>())
                    {
                        var pursuitSettingModel = attackModel.GetBehavior<PursuitSettingCustomModel>();
                        pursuitSettingModel.isSelectable = false;
                        attackModel.RemoveBehavior<PursuitSettingModel>();
                        attackModel.RemoveBehavior<FollowTouchSettingModel>();
                    }
                    
                    attackModel.RemoveBehavior(weaponModel);
                }
            }
            
            if (!airunit.HasBehavior<HeliMovementModel>())
            {
                MelonLogger.Msg("No HeliMovementModel for " + model.name);
                foreach (var behavior in airunit.behaviors)
                {
                    MelonLogger.Msg(behavior.name);
                }
                foreach (var behavior in model.behaviors)
                {
                    MelonLogger.Msg(behavior.name);
                }
                return;
            }

            MelonLogger.Msg("Fixing AirUnitModel for " + model.name);
            
            airunit.RemoveBehavior<PathMovementModel>();
            airunit.RemoveBehavior<CircleMovementModel>();
            airunit.RemoveBehavior<FigureEightMovementModel>();

            

            foreach (var airattack in model.GetBehaviors<AttackAirUnitModel>())
            {
                airattack.RemoveBehavior<CirclePatternModel>();
                airattack.RemoveBehaviors<FigureEightPatternModel>();
                airattack.RemoveBehavior<WingmonkeyPatternModel>();
                airattack.RemoveBehavior<CenterElipsePatternModel>();
            }

            model.UpdateTargetProviders();
        }
        catch (System.Exception e)
        {
            MelonLogger.Error(e);
        }
    }
}
