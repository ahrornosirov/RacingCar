using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class RacingBuild
{
    public static void PreparePrototype()
    {
        QuestProjectSetup.Configure();
        if (!File.Exists(RacingPrototypeBuilder.ScenePath)) RacingPrototypeBuilder.Create();
        RacingPrototypeChecks.Run();
    }

    [MenuItem("RacingCar/Build Quest APK")]
    public static void BuildAndroid()
    {
        QuestProjectSetup.Validate();
        Directory.CreateDirectory("Builds/Quest");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { RacingPrototypeBuilder.ScenePath },
            locationPathName = "Builds/Quest/RacingCar.apk",
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/android-build-summary.txt", $"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nSize: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\n");
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Quest APK build failed. Inspect the Unity build log.");
        Debug.Log("RACINGCAR_ANDROID_BUILD_OK: Builds/Quest/RacingCar.apk");
    }
}
