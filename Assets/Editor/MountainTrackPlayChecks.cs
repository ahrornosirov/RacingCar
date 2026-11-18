using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MountainTrackPlayChecks
{
    const string Key = "RacingCar.Check.Mountain";
    static RaceMenu menu;
    static int stage;
    static double next, started;
    static string saved;
    static bool hadSaved;
    static readonly System.Collections.Generic.List<string> results = new System.Collections.Generic.List<string>();
    static MountainTrackPlayChecks()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
            { started = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup - started > 40) throw new Exception("Mountain test timeout " + stage);
            if (stage == 0)
            {
                menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>();
                if (menu == null || !menu.IsOpen) return;
                hadSaved = PlayerPrefs.HasKey(RaceTrackSelection.SelectionKey); saved = PlayerPrefs.GetString(RaceTrackSelection.SelectionKey);
                menu.tracksButton.onClick.Invoke();
                menu.tracks.Preview(Array.FindIndex(menu.tracks.tracks, t => t.id == "mountain"));
                // Refresh through the real page controls after selecting the shipped map.
                menu.nextTrack.onClick.Invoke(); menu.previousTrack.onClick.Invoke();
                Require(menu.tracks.Current.id == "mountain" && menu.race.checkpointCount == 16, "Downloaded mountain map loads with 16 gates");
                Require(UnityEngine.Object.FindObjectsByType<RaceCheckpoint>().Length == 16, "Original circuit gates are disabled");
                var terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
                Require(terrain != null && terrain.materialTemplate.shader.name == "Universal Render Pipeline/Terrain/Lit", "Mountain terrain uses URP material");
                Require(terrain.terrainData.terrainLayers.Length > 0 && terrain.terrainData.terrainLayers.All(t => t != null && t.diffuseTexture != null), "Terrain layers retain their textures");
                Require(UnityEngine.Object.FindObjectsByType<MeshRenderer>().All(r => r.sharedMaterials.All(m => m != null && m.shader.isSupported)), "Active map and car materials have supported shaders");
                Advance();
            }
            else if (stage == 1)
            {
                RacingPreview.Export(menu.view.raceCamera, "Previews/mountain-select.png");
                menu.play.onClick.Invoke();
                Require(menu.CurrentPage == RaceMenu.Page.Home && PlayerPrefs.GetString(RaceTrackSelection.SelectionKey) == "mountain", "Use This Map saves the mountain selection");
                menu.race.countdownSeconds = 0; menu.StartRace();
                menu.race.GetComponent<RaceDriverInput>().enabled = false;
                menu.race.Throttle = 0.35f; Advance(3);
            }
            else if (stage == 2)
            {
                Require(menu.race.car.SpeedKph > 10 && menu.race.car.wheels.Count(w => w.isGrounded) >= 2,
                    $"Car drives on mountain road ({menu.race.car.SpeedKph:F1} km/h)");
                Require(menu.race.car.wheels[0].GetGroundHit(out var hit) && hit.collider.GetComponent<RoadSurface>() != null,
                    "Mountain road is identified as asphalt for tire effects");
                Require(menu.race.recordKey.StartsWith("RacingCar.Mountain.v1"), "Mountain records use their own scope");
                menu.race.Throttle = 0; menu.race.SetPaused(true); menu.SelectCamera(true); Advance();
            }
            else if (stage == 3)
            {
                RacingPreview.Export(menu.view.raceCamera, "Previews/mountain-cockpit.png");
                menu.SelectCamera(false); Advance();
            }
            else if (stage == 4)
            {
                RacingPreview.Export(menu.view.raceCamera, "Previews/mountain-chase.png");
                menu.race.Restart();
                Require(Vector3.Distance(menu.race.car.transform.position, menu.race.startPosition) < 0.01f, "Reset uses mountain spawn");
                menu.ShowHome();
                menu.garage.Browse(1); menu.garage.Cancel();
                Require(menu.race.recordKey.StartsWith("RacingCar.Mountain.v1"), "Garage changes retain mountain record scope");
                menu.tracksButton.onClick.Invoke(); menu.nextTrack.onClick.Invoke();
                Require(menu.tracks.Current.id == "circuit-01" && menu.race.checkpointCount == 8, "Original circuit remains selectable");
                menu.Back();
                Require(menu.tracks.Current.id == "mountain", "Back cancels an unconfirmed map change");
                Finish(true);
            }
        }
        catch (Exception error) { results.Add(error.ToString()); Finish(false); }
    }
    static void Advance(double delay = 0.4) { stage++; next = EditorApplication.timeSinceStartup + delay; }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); results.Add("PASS: " + message); }
    static void Finish(bool ok)
    {
        if (hadSaved) PlayerPrefs.SetString(RaceTrackSelection.SelectionKey, saved); else PlayerPrefs.DeleteKey(RaceTrackSelection.SelectionKey);
        PlayerPrefs.Save(); File.WriteAllLines("Logs/mountain-play-checks.txt", results);
        SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
        EditorApplication.Exit(ok ? 0 : 1);
    }
}
