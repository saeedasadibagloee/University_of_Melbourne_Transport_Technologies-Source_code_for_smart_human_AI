using System;
using Core;
using DataFormats;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

class VariablePresetHandler : MonoBehaviour
{
    [Header("General")]
    public InputField TimeStep;
    public InputField UIUpdate;
    public InputField PenaltyWeight;
    public InputField WallPadding;
    public InputField Theta;
    public InputField ReactionTime;
    public InputField DestinationLevelTheta1;
    public InputField DestinationLevelTheta2;
    [Header("Agent")]
    public InputField AgentSpeed;
    public InputField AgentSpeedDeviation;
    public InputField AgentRadius;
    public InputField AgentRadiusDeviation;
    public InputField AgentWeight;
    public InputField AgentWeightDeviation;
    public InputField FollowerLeadingMulti;
    [Header("Forces")]
    public InputField DensityFactor;
    public InputField a1NormalForce;
    public InputField a2NormalForce;
    public InputField FrictionalForce;
    public InputField RepulsionDistance;
    public InputField AttractiveForce;
    public InputField RepulsiveForce;
    public InputField NeighbourRadius;
    public InputField WallForce;
    public InputField SafeWallDistance;
    [Header("GateChoice")]
    public List<InputField> GC_Class1 = new List<InputField>();
    public List<InputField> GC_Class2 = new List<InputField>();
    public GameObject GateChoiceOverrideCover;
    [Header("Collision Avoidance")]
    public InputField MaxNeighbours;
    public InputField TimeHorizonAgent;
    public InputField TimeHorizonObstacles;
    [Header("Decision Update")]
    public InputField Congestion;
    public InputField GateVisibility;
    public InputField SocialInfluence;
    public InputField SocialInfluenceTime;
    public InputField InertiaUtility;
    public InputField UpdateCycle;
    public InputField DensityUpper;
    public InputField DensityLower;
    public InputField SpeedUpper;
    public InputField SpeedLower;
    [Header("Reaction Time")]
    public InputField ReactionTimeRadius;
    public InputField ReactionTimeBeta1;
    public InputField ReactionTimeBeta2;
    public InputField ExpMu;
    public InputField ExpLambda;
    public InputField WeibullMu;
    public InputField WeibullNu;
    public InputField WeibullLambda;

    private static VariablePresetHandler _instance;
    public static VariablePresetHandler Instance
    {
        get
        {
            return _instance ?? (_instance = FindObjectOfType<VariablePresetHandler>());
        }
    }

    public void ApplyVariables(Model model)
    {
        if (model == null)
            return;

        Debug.Log("Variables detected within model, applying.");

        Params.Current = model.savedParameters;

        UpdateAllFields();
    }

    public void UpdateAllFields()
    {
        // General
        TimeStep.text = Params.Current.TimeStep == Params.CurrentDefaults.TimeStep ? "" : Params.Current.TimeStep.ToString();
        TimeStep.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.TimeStep.ToString();
        UIUpdate.text = Params.Current.UiUpdateCycle == Params.CurrentDefaults.UiUpdateCycle ? "" : Params.Current.UiUpdateCycle.ToString();
        UIUpdate.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UiUpdateCycle.ToString();
        PenaltyWeight.text = Params.Current.PenaltyWeight == Params.CurrentDefaults.PenaltyWeight ? "" : Params.Current.PenaltyWeight.ToString();
        PenaltyWeight.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.PenaltyWeight.ToString();
        WallPadding.text = Params.Current.AgentWallPaddingDistance == Params.CurrentDefaults.AgentWallPaddingDistance ? "" : Params.Current.AgentWallPaddingDistance.ToString();
        WallPadding.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.AgentWallPaddingDistance.ToString();
        Theta.text = Params.Current.Theta == Params.CurrentDefaults.Theta ? "" : Params.Current.Theta.ToString();
        Theta.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.Theta.ToString();
        ReactionTime.text = Params.Current.DefaultReactionTime == Params.CurrentDefaults.DefaultReactionTime ? "" : Params.Current.DefaultReactionTime.ToString();
        ReactionTime.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DefaultReactionTime.ToString();
        DestinationLevelTheta1.text = Params.Current.DestinationLevelTheta1 == Params.CurrentDefaults.DestinationLevelTheta1 ? "" : Params.Current.DestinationLevelTheta1.ToString();
        DestinationLevelTheta1.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DestinationLevelTheta1.ToString();
        DestinationLevelTheta2.text = Params.Current.DestinationLevelTheta2 == Params.CurrentDefaults.DestinationLevelTheta2 ? "" : Params.Current.DestinationLevelTheta2.ToString();
        DestinationLevelTheta2.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DestinationLevelTheta2.ToString();
        // Agent
        AgentSpeed.text = Params.Current.AgentMaxspeed == Params.CurrentDefaults.AgentMaxspeed ? "" : Params.Current.AgentMaxspeed.ToString();
        AgentSpeed.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.AgentMaxspeed.ToString();
        AgentSpeedDeviation.text = Params.Current.AgentMaxspeedDeviation == Params.CurrentDefaults.AgentMaxspeedDeviation ? "" : Params.Current.AgentMaxspeedDeviation.ToString();
        AgentSpeedDeviation.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.AgentMaxspeedDeviation.ToString();
        AgentRadius.text = Params.Current.AgentRadius == Params.CurrentDefaults.AgentRadius ? "" : Params.Current.AgentRadius.ToString();
        AgentRadius.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.AgentRadius.ToString();
        AgentRadiusDeviation.text = Params.Current.AgentRadiusDeviation == Params.CurrentDefaults.AgentRadiusDeviation ? "" : Params.Current.AgentRadiusDeviation.ToString();
        AgentRadiusDeviation.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.AgentRadiusDeviation.ToString();
        AgentWeight.text = Params.Current.AgentWeight == Params.CurrentDefaults.AgentWeight ? "" : Params.Current.AgentWeight.ToString();
        AgentWeight.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.AgentWeight.ToString();
        AgentWeightDeviation.text = Params.Current.AgentWeightDeviation == Params.CurrentDefaults.AgentWeightDeviation ? "" : Params.Current.AgentWeightDeviation.ToString();
        AgentWeightDeviation.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.AgentWeightDeviation.ToString();
        FollowerLeadingMulti.text = Params.Current.AgentFollowerInFrontSpeed == Params.CurrentDefaults.AgentFollowerInFrontSpeed ? "" : Params.Current.AgentFollowerInFrontSpeed.ToString();
        FollowerLeadingMulti.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.AgentFollowerInFrontSpeed.ToString();
        // Forces
        DensityFactor.text = Params.Current.DensityFactor == Params.CurrentDefaults.DensityFactor ? "" : Params.Current.DensityFactor.ToString();
        DensityFactor.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DensityFactor.ToString();
        a1NormalForce.text = Params.Current.DefaultAlpha1NormalForce == Params.CurrentDefaults.DefaultAlpha1NormalForce ? "" : Params.Current.DefaultAlpha1NormalForce.ToString();
        a1NormalForce.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DefaultAlpha1NormalForce.ToString();
        a2NormalForce.text = Params.Current.DefaultAlpha2NormalForce == Params.CurrentDefaults.DefaultAlpha2NormalForce ? "" : Params.Current.DefaultAlpha2NormalForce.ToString();
        a2NormalForce.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DefaultAlpha2NormalForce.ToString();
        FrictionalForce.text = Params.Current.DefaultFrictionalForce == Params.CurrentDefaults.DefaultFrictionalForce ? "" : Params.Current.DefaultFrictionalForce.ToString();
        FrictionalForce.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DefaultFrictionalForce.ToString();
        RepulsionDistance.text = Params.Current.DefaultRepultionDistance == Params.CurrentDefaults.DefaultRepultionDistance ? "" : Params.Current.DefaultRepultionDistance.ToString();
        RepulsionDistance.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DefaultRepultionDistance.ToString();
        AttractiveForce.text = Params.Current.DefaultAttractiveForce == Params.CurrentDefaults.DefaultAttractiveForce ? "" : Params.Current.DefaultAttractiveForce.ToString();
        AttractiveForce.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DefaultAttractiveForce.ToString();
        RepulsiveForce.text = Params.Current.DefaultRepulsiveForce == Params.CurrentDefaults.DefaultRepulsiveForce ? "" : Params.Current.DefaultRepulsiveForce.ToString();
        RepulsiveForce.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DefaultRepulsiveForce.ToString();
        NeighbourRadius.text = Params.Current.NeighborRadius == Params.CurrentDefaults.NeighborRadius ? "" : Params.Current.NeighborRadius.ToString();
        NeighbourRadius.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.NeighborRadius.ToString();
        WallForce.text = Params.Current.DefaultWallForce == Params.CurrentDefaults.DefaultWallForce ? "" : Params.Current.DefaultWallForce.ToString();
        WallForce.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DefaultWallForce.ToString();
        SafeWallDistance.text = Params.Current.DefaultSafeWallDistance == Params.CurrentDefaults.DefaultSafeWallDistance ? "" : Params.Current.DefaultSafeWallDistance.ToString();
        SafeWallDistance.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DefaultSafeWallDistance.ToString();
        // GateChoice
        GC_Class1[(int)Def.GcValues.Dist].text = Params.Current.UtilitiesClass1._distanceUtility == Params.CurrentDefaults.UtilitiesClass1._distanceUtility ? "" : Params.Current.UtilitiesClass1._distanceUtility.ToString();
        GC_Class1[(int)Def.GcValues.Dist].placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UtilitiesClass1._distanceUtility.ToString();
        GC_Class1[(int)Def.GcValues.Cong].text = Params.Current.UtilitiesClass1._congestionUtility == Params.CurrentDefaults.UtilitiesClass1._congestionUtility ? "" : Params.Current.UtilitiesClass1._congestionUtility.ToString();
        GC_Class1[(int)Def.GcValues.Cong].placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UtilitiesClass1._congestionUtility.ToString();
        GC_Class1[(int)Def.GcValues.Flow].text = Params.Current.UtilitiesClass1._flowExitUtility == Params.CurrentDefaults.UtilitiesClass1._flowExitUtility ? "" : Params.Current.UtilitiesClass1._flowExitUtility.ToString();
        GC_Class1[(int)Def.GcValues.Flow].placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UtilitiesClass1._flowExitUtility.ToString();
        GC_Class1[(int)Def.GcValues.Tovis].text = Params.Current.UtilitiesClass1._fltovis == Params.CurrentDefaults.UtilitiesClass1._fltovis ? "" : Params.Current.UtilitiesClass1._fltovis.ToString();
        GC_Class1[(int)Def.GcValues.Tovis].placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UtilitiesClass1._fltovis.ToString();
        GC_Class1[(int)Def.GcValues.Toinvis].text = Params.Current.UtilitiesClass1._fltoinvis == Params.CurrentDefaults.UtilitiesClass1._fltoinvis ? "" : Params.Current.UtilitiesClass1._fltoinvis.ToString();
        GC_Class1[(int)Def.GcValues.Toinvis].placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UtilitiesClass1._fltoinvis.ToString();
        GC_Class1[(int)Def.GcValues.Vis].text = Params.Current.UtilitiesClass1._visibilityUtility == Params.CurrentDefaults.UtilitiesClass1._visibilityUtility ? "" : Params.Current.UtilitiesClass1._visibilityUtility.ToString();
        GC_Class1[(int)Def.GcValues.Vis].placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UtilitiesClass1._visibilityUtility.ToString();
        GC_Class2[(int)Def.GcValues.Dist].text = Params.Current.UtilitiesClass2._distanceUtility == Params.CurrentDefaults.UtilitiesClass2._distanceUtility ? "" : Params.Current.UtilitiesClass2._distanceUtility.ToString();
        GC_Class2[(int)Def.GcValues.Dist].placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UtilitiesClass2._distanceUtility.ToString();
        GC_Class2[(int)Def.GcValues.Cong].text = Params.Current.UtilitiesClass2._congestionUtility == Params.CurrentDefaults.UtilitiesClass2._congestionUtility ? "" : Params.Current.UtilitiesClass2._congestionUtility.ToString();
        GC_Class2[(int)Def.GcValues.Cong].placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UtilitiesClass2._congestionUtility.ToString();
        GC_Class2[(int)Def.GcValues.Flow].text = Params.Current.UtilitiesClass2._flowExitUtility == Params.CurrentDefaults.UtilitiesClass2._flowExitUtility ? "" : Params.Current.UtilitiesClass2._flowExitUtility.ToString();
        GC_Class2[(int)Def.GcValues.Flow].placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UtilitiesClass2._flowExitUtility.ToString();
        GC_Class2[(int)Def.GcValues.Tovis].text = Params.Current.UtilitiesClass2._fltovis == Params.CurrentDefaults.UtilitiesClass2._fltovis ? "" : Params.Current.UtilitiesClass2._fltovis.ToString();
        GC_Class2[(int)Def.GcValues.Tovis].placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UtilitiesClass2._fltovis.ToString();
        GC_Class2[(int)Def.GcValues.Toinvis].text = Params.Current.UtilitiesClass2._fltoinvis == Params.CurrentDefaults.UtilitiesClass2._fltoinvis ? "" : Params.Current.UtilitiesClass2._fltoinvis.ToString();
        GC_Class2[(int)Def.GcValues.Toinvis].placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UtilitiesClass2._fltoinvis.ToString();
        GC_Class2[(int)Def.GcValues.Vis].text = Params.Current.UtilitiesClass2._visibilityUtility == Params.CurrentDefaults.UtilitiesClass2._visibilityUtility ? "" : Params.Current.UtilitiesClass2._visibilityUtility.ToString();
        GC_Class2[(int)Def.GcValues.Vis].placeholder.GetComponent<Text>().text = Params.CurrentDefaults.UtilitiesClass2._visibilityUtility.ToString();
        GateChoiceOverrideCover.SetActive(false);
        // Collision Avoidance
        MaxNeighbours.text = Params.Current.RvoMaxNeighbours == Params.CurrentDefaults.RvoMaxNeighbours ? "" : Params.Current.RvoMaxNeighbours.ToString();
        MaxNeighbours.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.RvoMaxNeighbours.ToString();
        TimeHorizonAgent.text = Params.Current.RvoTimeHorizonAgent == Params.CurrentDefaults.RvoTimeHorizonAgent ? "" : Params.Current.RvoTimeHorizonAgent.ToString();
        TimeHorizonAgent.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.RvoTimeHorizonAgent.ToString();
        TimeHorizonObstacles.text = Params.Current.RvoTimeHorizonObstacle == Params.CurrentDefaults.RvoTimeHorizonObstacle ? "" : Params.Current.RvoTimeHorizonObstacle.ToString();
        TimeHorizonObstacles.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.RvoTimeHorizonObstacle.ToString();
        // Decision Update
        Congestion.text = Params.Current.CongestionVarianceUtility == Params.CurrentDefaults.CongestionVarianceUtility ? "" : Params.Current.CongestionVarianceUtility.ToString();
        Congestion.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.CongestionVarianceUtility.ToString();
        GateVisibility.text = Params.Current.GateVisibityUtility == Params.CurrentDefaults.GateVisibityUtility ? "" : Params.Current.GateVisibityUtility.ToString();
        GateVisibility.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.GateVisibityUtility.ToString();
        SocialInfluence.text = Params.Current.SocialInfluenceUtility == Params.CurrentDefaults.SocialInfluenceUtility ? "" : Params.Current.SocialInfluenceUtility.ToString();
        SocialInfluence.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.SocialInfluenceUtility.ToString();
        SocialInfluenceTime.text = Params.Current.SocialInfluenceTime == Params.CurrentDefaults.SocialInfluenceTime ? "" : Params.Current.SocialInfluenceTime.ToString();
        SocialInfluenceTime.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.SocialInfluenceTime.ToString();
        InertiaUtility.text = Params.Current.InertiaUtility == Params.CurrentDefaults.InertiaUtility ? "" : Params.Current.InertiaUtility.ToString();
        InertiaUtility.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.InertiaUtility.ToString();
        UpdateCycle.text = Params.Current.DecisionUpdateCycle == Params.CurrentDefaults.DecisionUpdateCycle ? "" : Params.Current.DecisionUpdateCycle.ToString();
        UpdateCycle.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DecisionUpdateCycle.ToString();
        DensityLower.text = Params.Current.DUDensityLower == Params.CurrentDefaults.DUDensityLower ? "" : Params.Current.DUDensityLower.ToString();
        DensityLower.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DUDensityLower.ToString();
        DensityUpper.text = Params.Current.DUDensityUpper == Params.CurrentDefaults.DUDensityUpper ? "" : Params.Current.DUDensityUpper.ToString();
        DensityUpper.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DUDensityUpper.ToString();
        SpeedLower.text = Params.Current.DUSpeedLower == Params.CurrentDefaults.DUSpeedLower ? "" : Params.Current.DUSpeedLower.ToString();
        SpeedLower.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DUSpeedLower.ToString();
        SpeedUpper.text = Params.Current.DUSpeedUpper == Params.CurrentDefaults.DUSpeedUpper ? "" : Params.Current.DUSpeedUpper.ToString();
        SpeedUpper.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.DUSpeedUpper.ToString();
        // Reaction Time
        ReactionTimeRadius.text = Params.Current.Radius_RT == Params.CurrentDefaults.Radius_RT ? "" : Params.Current.Radius_RT.ToString();
        ReactionTimeRadius.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.Radius_RT.ToString();
        ReactionTimeBeta1.text = Params.Current.Beta1 == Params.CurrentDefaults.Beta1 ? "" : Params.Current.Beta1.ToString();
        ReactionTimeBeta1.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.Beta1.ToString();
        ReactionTimeBeta2.text = Params.Current.Beta2 == Params.CurrentDefaults.Beta2 ? "" : Params.Current.Beta2.ToString();
        ReactionTimeBeta2.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.Beta2.ToString();
        ExpMu.text = Params.Current.Exp_Hazard_Mu == Params.CurrentDefaults.Exp_Hazard_Mu ? "" : Params.Current.Exp_Hazard_Mu.ToString();
        ExpMu.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.Exp_Hazard_Mu.ToString();
        ExpLambda.text = Params.Current.Exp_Hazard_Lambda == Params.CurrentDefaults.Exp_Hazard_Lambda ? "" : Params.Current.Exp_Hazard_Lambda.ToString();
        ExpLambda.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.Exp_Hazard_Lambda.ToString();
        WeibullMu.text = Params.Current.Weibull_Hazard_Mu == Params.CurrentDefaults.Weibull_Hazard_Mu ? "" : Params.Current.Weibull_Hazard_Mu.ToString();
        WeibullMu.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.Weibull_Hazard_Mu.ToString();
        WeibullNu.text = Params.Current.Weibull_Hazard_Nu == Params.CurrentDefaults.Weibull_Hazard_Nu ? "" : Params.Current.Weibull_Hazard_Nu.ToString();
        WeibullNu.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.Weibull_Hazard_Nu.ToString();
        WeibullLambda.text = Params.Current.Weibull_Hazard_Lambda == Params.CurrentDefaults.Weibull_Hazard_Lambda ? "" : Params.Current.Weibull_Hazard_Lambda.ToString();
        WeibullLambda.placeholder.GetComponent<Text>().text = Params.CurrentDefaults.Weibull_Hazard_Lambda.ToString();
    }

    internal void ResetPresetToDefault()
    {
        Params.Current = ObjectExtensions.Copy(Params.CurrentDefaults);
        UpdateAllFields();
    }
}