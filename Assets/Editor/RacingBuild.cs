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
    public static void BuildAndroid() => BuildAndroidTo("Builds/Quest/RacingCar.apk");

    [MenuItem("RacingCar/Build Hand Steering Quest APK")]
    public static void BuildHandSteeringAndroid() => BuildAndroidTo("Builds/Quest/RacingCar-Hands.apk");

    public static void BuildHandSteeringTest2Android() => BuildAndroidTo("Builds/Quest/RacingCar-Hands-Test2.apk");

    public static void BuildHandSteeringTest3Android() => BuildAndroidTo("Builds/Quest/RacingCar-Hands-Test3.apk");

    static void BuildAndroidTo(string outputPath)
    {
        QuestProjectSetup.Validate();
        Directory.CreateDirectory("Builds/Quest");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { RacingPrototypeBuilder.ScenePath },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/android-build-summary.txt", $"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nSize: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\n");
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Quest APK build failed. Inspect the Unity build log.");
        Debug.Log("RACINGCAR_ANDROID_BUILD_OK: " + outputPath);
    }
}
