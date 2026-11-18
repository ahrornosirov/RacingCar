using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RacingTrackPlayChecks
{
    const string Key = "RacingCar.Check.TracksRunning";
    static RaceMenu menu;
    static RaceTrackSelection selection;
    static TrackDefinition testMap;
    static TrackDefinition[] shippedMaps;
    static GameObject source;
    static int stage;
    static double started, next;
    static bool captured, hadSaved;
    static string saved;
    static readonly List<string> results = new List<string>();

    static RacingTrackPlayChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
            { started = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
        };
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup - started > 35) throw new Exception("Map selector check timed out at " + stage);
            if (stage == 0)
            {
                menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>();
                if (menu == null || !menu.IsOpen) return;
                selection = menu.tracks;
                hadSaved = PlayerPrefs.HasKey(RaceTrackSelection.SelectionKey);
                saved = PlayerPrefs.GetString(RaceTrackSelection.SelectionKey); captured = true;
                selection.Preview(0); selection.Confirm();
                shippedMaps = selection.tracks;
                testMap = ScriptableObject.CreateInstance<TrackDefinition>();
                testMap.id = "map-test-only"; testMap.displayName = "Test map";
                testMap.recordKey = "RacingCar.Check.TrackSelection";
                testMap.startPosition = new Vector3(500, 0.8f, 500);
                source = new GameObject("Temporary map check source"); source.SetActive(false);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.transform.SetParent(source.transform);
                floor.transform.position = new Vector3(500, -0.5f, 500);
                floor.transform.localScale = new Vector3(100, 1, 100);
                for (int i = 0; i < 3; i++)
                {
                    var gate = new GameObject("Test gate " + i, typeof(BoxCollider), typeof(RaceCheckpoint));
                    gate.transform.SetParent(source.transform);
                    gate.transform.position = new Vector3(500, 1, 498 + i * 10);
                    gate.GetComponent<BoxCollider>().isTrigger = true;
                    gate.GetComponent<BoxCollider>().size = new Vector3(30, 4, 1);
                    gate.GetComponent<RaceCheckpoint>().index = i;
                }
                testMap.environmentPrefab = source;
                selection.tracks = new[] { shippedMaps[0], testMap };
                menu.tracksButton.onClick.Invoke();
                Require(menu.CurrentPage == RaceMenu.Page.Tracks && menu.BlocksDriving, "Select Map opens and blocks driving");
                menu.nextTrack.onClick.Invoke(); Advance();
            }
            else if (stage == 1)
            {
                Require(selection.PreviewIndex == 1 && !selection.circuitEnvironment.activeSelf && !selection.circuitCheckpoints.activeSelf,
                    "Map preview disables original road and checkpoint triggers");
                Require(Vector3.Distance(menu.race.car.transform.position, testMap.startPosition) < 2, "Selected car moves to new map spawn");
                Require(menu.race.checkpointCount == 3 && UnityEngine.Object.FindObjectsByType<RaceCheckpoint>().Length == 3, "Only the new map's gates are active");
                menu.garage.Browse(1);
                Require(menu.race.recordKey.StartsWith(testMap.recordKey), "Changing car preserves selected map record scope");
                menu.back.onClick.Invoke(); Advance();
            }
            else if (stage == 2)
            {
                Require(selection.PreviewIndex == 0 && selection.circuitEnvironment.activeSelf && menu.race.checkpointCount == 8,
                    "Back restores confirmed circuit, spawn and gate count");
                Require(menu.race.recordKey.StartsWith(shippedMaps[0].recordKey), "Back restores original map record scope");
                menu.tracksButton.onClick.Invoke(); menu.nextTrack.onClick.Invoke(); menu.play.onClick.Invoke();
                Require(menu.CurrentPage == RaceMenu.Page.Home && PlayerPrefs.GetString(RaceTrackSelection.SelectionKey) == testMap.id,
                    "Use This Map saves selection and returns Home");
                menu.race.countdownSeconds = 0;
                menu.StartRace();
                Require(!selection.Preview(0), "Map changes are rejected during racing");
                menu.race.GetComponent<RaceDriverInput>().enabled = false;
                menu.race.Throttle = 1; Advance(2);
            }
            else if (stage == 3)
            {
                Require(menu.race.car.SpeedKph > 5 && menu.race.car.wheels[0].isGrounded,
                    $"Car drives on selected map collision surface ({menu.race.car.SpeedKph:F1} km/h, grounded {menu.race.car.wheels[0].isGrounded})");
                menu.race.GetComponent<RaceDriverInput>().enabled = true;
                menu.ShowPause(); Require(!selection.Preview(0), "Pausing cannot change the race map");
                menu.race.Restart();
                Require(Vector3.Distance(menu.race.car.transform.position, testMap.startPosition) < 0.01f, "Reset uses selected map spawn");
                menu.ShowHome(); selection.Preview(0); selection.Confirm();
                selection.tracks = shippedMaps;
                menu.garage.Cancel(); menu.ShowHome();
                Advance();
            }
            else if (stage == 4)
            {
                RacingPreview.Export(menu.view.raceCamera, "Previews/map-selection-home.png");
                menu.tracksButton.onClick.Invoke(); Advance();
            }
            else if (stage == 5)
            {
                RacingPreview.Export(menu.view.raceCamera, "Previews/map-selection.png");
                Finish(true);
            }
        }
        catch (Exception error) { results.Add(error.ToString()); Finish(false); }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        results.Add("PASS: " + message);
    }
    static void Advance(double delay = 0.4) { stage++; next = EditorApplication.timeSinceStartup + delay; }
    static void Finish(bool ok)
    {
        if (captured)
        {
            if (hadSaved) PlayerPrefs.SetString(RaceTrackSelection.SelectionKey, saved); else PlayerPrefs.DeleteKey(RaceTrackSelection.SelectionKey);
            PlayerPrefs.Save();
        }
        File.WriteAllLines("Logs/track-play-checks.txt", results);
        SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1); else EditorApplication.isPlaying = false;
    }
}
