using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RacingTrackSetup
{
    [MenuItem("RacingCar/Install Map Selection")]
    public static void Apply()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        Directory.CreateDirectory("Backups/BeforeTrackSelection");
        string backup = "Backups/BeforeTrackSelection/RacingPrototype.unity";
        if (!File.Exists(backup)) File.Copy(RacingPrototypeBuilder.ScenePath, backup);
        var menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>();
        var selection = menu.GetComponent<RaceTrackSelection>();
        if (selection == null) selection = menu.gameObject.AddComponent<RaceTrackSelection>();
        selection.race = menu.race; selection.garage = menu.garage; selection.view = menu.view;
        if (selection.circuitEnvironment == null) selection.circuitEnvironment = GameObject.Find("Visual Upgrade v1");
        if (selection.circuitEnvironment == null) throw new InvalidOperationException("Original circuit environment was not found.");
        if (selection.circuitCheckpoints == null)
        {
            selection.circuitCheckpoints = new GameObject("Circuit 01 Checkpoints");
            foreach (var checkpoint in UnityEngine.Object.FindObjectsByType<RaceCheckpoint>())
                checkpoint.transform.SetParent(selection.circuitCheckpoints.transform, true);
        }
        Directory.CreateDirectory("Assets/Tracks");
        var circuit = AssetDatabase.LoadAssetAtPath<TrackDefinition>("Assets/Tracks/Circuit01.asset");
        if (circuit == null)
        {
            circuit = ScriptableObject.CreateInstance<TrackDefinition>();
            circuit.id = "circuit-01"; circuit.displayName = "Circuit 01";
            circuit.description = "Original circuit\nAsphalt, curbs and sweeping corners";
            circuit.credit = "RacingCar original circuit\nAsphalt texture: ambientCG | CC0";
            circuit.recordKey = "RacingCar.Circuit01.v1";
            circuit.startPosition = menu.race.startPosition;
            circuit.startEuler = menu.race.startEuler;
            AssetDatabase.CreateAsset(circuit, "Assets/Tracks/Circuit01.asset");
        }
        if (selection.tracks == null || selection.tracks.Length == 0) selection.tracks = new[] { circuit };
        menu.tracks = selection;
        if (menu.tracksButton == null) menu.tracksButton = CloneButton(menu, "Select map");
        if (menu.previousTrack == null) menu.previousTrack = CloneButton(menu, "Previous map");
        if (menu.nextTrack == null) menu.nextTrack = CloneButton(menu, "Next map");
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        EditorSceneManager.SaveScene(menu.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("RACINGCAR_MAP_SELECTION_INSTALLED");
    }

    static Button CloneButton(RaceMenu menu, string name)
    {
        var button = UnityEngine.Object.Instantiate(menu.controls, menu.card);
        button.name = name; button.onClick = new Button.ButtonClickedEvent();
        button.GetComponentInChildren<TMP_Text>().text = name.ToUpperInvariant();
        button.gameObject.SetActive(false);
        return button;
    }

    public static void ApplyAndCheck() { Apply(); RacingTrackPlayChecks.Run(); }
}
