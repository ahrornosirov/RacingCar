using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class QuestHandsBaselineChecks
{
    const string Key = "RacingCar.BaselinePlayCheck";
    static double entered;
    static QuestHandsBaselineChecks()
    {
        EditorApplication.playModeStateChanged += s => {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
            { entered = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(QuestHandsBaselineSetup.ScenePath);
        QuestHandsBaselineSetup.ValidateScene();
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - entered < 1) return;
        int code = 0;
        try
        {
            var check = UnityEngine.Object.FindAnyObjectByType<QuestHandsBaseline>();
            if (check == null || check.headsetCamera == null) throw new Exception("Test panel/camera is missing");
            for (int i = 0; i < 5; i++) { check.firstButton.onClick.Invoke(); check.secondButton.onClick.Invoke(); }
            if (!check.result.text.Contains("BUTTON A: 5") || !check.result.text.Contains("BUTTON B: 5")) throw new Exception("Button callbacks failed");
            if (Time.timeScale != 1) throw new Exception("Test room unexpectedly paused");
            check.panel.SetPositionAndRotation(check.headsetCamera.transform.position + check.headsetCamera.transform.forward * 1.25f, check.headsetCamera.transform.rotation);
            Canvas.ForceUpdateCanvases();
            foreach (var text in check.panel.GetComponentsInChildren<TMPro.TMP_Text>())
            { text.ForceMeshUpdate(); if (text.isTextOverflowing) throw new Exception("Panel label overflow: " + text.text); }
            RacingPreview.Export(check.headsetCamera, "Previews/quest-hands-baseline.png");
            File.WriteAllText("Logs/hands-baseline-play-checks.txt", "PASS: isolated scene starts unpaused, official XR rig/UI present, button callbacks count five each, instructions fit.\nActual hand visibility and pinch/poke acceptance remain pending on Quest.\n");
            Debug.Log("QUEST_HANDS_BASELINE_PLAY_OK");
        }
        catch (Exception e) { code = 1; Debug.LogException(e); File.WriteAllText("Logs/hands-baseline-play-checks.txt", e.ToString()); }
        SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false; EditorApplication.Exit(code);
    }
}
