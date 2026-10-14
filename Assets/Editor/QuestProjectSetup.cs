using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

public static class QuestProjectSetup
{
    [MenuItem("RacingCar/Configure Quest Project")]
    public static void Configure()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Install Android Build Support for this Unity editor in Unity Hub, then restart Unity before configuring Quest.");

        PlayerSettings.companyName = "ahrornosirov";
        PlayerSettings.productName = "RacingCar";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ahrornosirov.racingcar");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });

        var mobilePipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if (mobilePipeline == null)
            throw new InvalidOperationException("The Universal 3D mobile rendering asset is missing.");
        mobilePipeline.msaaSampleCount = 4;
        GraphicsSettings.defaultRenderPipeline = mobilePipeline;
        QualitySettings.renderPipeline = mobilePipeline;
        EditorUtility.SetDirty(mobilePipeline);

        Directory.CreateDirectory("Assets/XR/Settings");
        AssetDatabase.Refresh();
        if (!EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(XRGeneralSettings.settingsKey, out var targets))
        {
            targets = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(targets, "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, targets, true);
        }
        if (!targets.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
            targets.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

        var general = targets.SettingsForBuildTarget(BuildTargetGroup.Android);
        general.InitManagerOnStart = true;
        if (!XRPackageMetadataStore.AssignLoader(general.Manager, typeof(OpenXRLoader).FullName, BuildTargetGroup.Android))
            throw new InvalidOperationException("Could not assign the Android OpenXR loader.");

        FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
        var xr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        Enable<MetaQuestFeature>(xr);
        Enable<OculusTouchControllerProfile>(xr);
        Enable<MetaQuestTouchPlusControllerProfile>(xr);
        xr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;

        EditorUtility.SetDirty(general);
        EditorUtility.SetDirty(general.Manager);
        EditorUtility.SetDirty(targets);
        EditorUtility.SetDirty(xr);
        AssetDatabase.SaveAssets();
        Validate();
    }

    static void Enable<T>(OpenXRSettings settings) where T : OpenXRFeature
    {
        var feature = settings.GetFeature<T>();
        if (feature == null)
            throw new InvalidOperationException($"Missing OpenXR feature {typeof(T).Name}.");
        feature.enabled = true;
        EditorUtility.SetDirty(feature);
    }

    [MenuItem("RacingCar/Validate Quest Setup")]
    public static void Validate()
    {
        var issues = new List<OpenXRFeature.ValidationRule>();
        OpenXRProjectValidation.GetCurrentValidationIssues(issues, BuildTargetGroup.Android);
        var lines = new List<string>
        {
            $"Unity: {Application.unityVersion}",
            $"Android module available: {BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android)}",
            $"Active build target: {EditorUserBuildSettings.activeBuildTarget}",
            $"Android backend: {PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)}",
            $"Android architecture: {PlayerSettings.Android.targetArchitectures}"
        };
        int errors = 0;
        foreach (var issue in issues)
        {
            lines.Add($"{(issue.error ? "ERROR" : "WARNING")}: {issue.message}");
            if (issue.error) errors++;
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/quest-setup-validation.txt", lines);
        Debug.Log(string.Join("\n", lines));
        if (errors > 0)
            throw new InvalidOperationException($"Quest setup has {errors} validation errors. See Logs/quest-setup-validation.txt.");
        Debug.Log("RACINGCAR_QUEST_SETUP_OK");
    }
}
