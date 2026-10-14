using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Runs the actual runtime components, particle systems and physics across Play mode frames.
[InitializeOnLoad]
public static class RacingVisualPlayChecks
{
    const string Key = "RacingCar.VisualPlayChecks";
    static RaceSession race;
    static CarDrivingEffects effects;
    static CarPresentation presentation;
    static int stage;
    static double stageStart;
    static float pausedLap;
    static int pausedMarks;
    static bool sawSparks;
    static Quaternion restingSteering;
    static readonly List<string> results = new List<string>();

    static RacingVisualPlayChecks()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
        if (SessionState.GetBool(Key, false)) EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    static void OnPlayState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            stageStart = EditorApplication.timeSinceStartup;
        }
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(Key, false)) return;
        try
        {
            double elapsed = EditorApplication.timeSinceStartup - stageStart;
            if (stage == 0)
            {
                race = UnityEngine.Object.FindAnyObjectByType<RaceSession>();
                if (race == null || race.car.Body == null) return;
                UnityEngine.Object.FindAnyObjectByType<RaceDriverInput>().enabled = false;
                Require(UnityEngine.Object.FindAnyObjectByType<RacerCameraRig>().cockpit, "First-person view is the default");
                effects = race.car.GetComponent<CarDrivingEffects>();
                presentation = race.car.GetComponent<CarPresentation>();
                restingSteering = presentation.steeringWheel.localRotation;
                var menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>();
                if (menu != null) menu.StartRace();
                else if (race.Paused) race.TogglePause();
                race.Restart(); race.Throttle = 1f;
                Advance();
            }
            else if (stage == 1 && elapsed > 4f)
            {
                results.Add($"Runtime state: speed {race.car.SpeedKph:0.0}, lap {race.LapTime:0.0}, paused {race.Paused}, position {race.car.transform.position}, throttle {race.Throttle}, grounded {race.car.wheels[0].isGrounded}.");
                Require(race.car.SpeedKph > 15f, "Runtime acceleration on the new track");
                Require(race.car.EngineRpm > 900f && race.car.Gear >= 1, "Drivetrain instruments update");
                race.Steering = 0.5f;
                Advance();
            }
            else if (stage == 2 && elapsed > 0.5f)
            {
                Require(Quaternion.Angle(restingSteering, presentation.steeringWheel.localRotation) > 15f, "Steering wheel follows input");
                race.TogglePause(); pausedLap = race.LapTime; pausedMarks = effects.ActiveMarks;
                Advance();
            }
            else if (stage == 3 && elapsed > 0.6f)
            {
                Require(Mathf.Abs(race.LapTime - pausedLap) < 0.001f && effects.ActiveMarks == pausedMarks, "Pause freezes lap timer and new tire marks");
                race.TogglePause(); race.Throttle = 1f; race.Steering = 0f;
                // Infield grass has a distinct collider, away from guardrails and road.
                race.car.ResetTo(new Vector3(0f, 0.4f, 0f), Quaternion.identity);
                Advance();
            }
            else if (stage == 4 && elapsed > 2.5f)
            {
                int dust = 0; foreach (var particles in effects.dust) dust += particles.particleCount;
                Require(dust > 0, "Grass contact emits dust while driving");
                race.Restart();
                Advance();
            }
            else if (stage == 5 && elapsed > 0.2f)
            {
                int particles = effects.sparks.particleCount;
                foreach (var effect in effects.smoke) particles += effect.particleCount;
                foreach (var effect in effects.dust) particles += effect.particleCount;
                Require(effects.ActiveMarks == 0 && particles == 0, "Reset clears marks and particles");
                // Inject sideways motion onto asphalt to exercise real WheelCollider slip.
                race.car.Body.linearVelocity = new Vector3(7f, 0f, 9f);
                race.Handbrake = true; race.Steering = 0.5f;
                Advance();
            }
            else if (stage == 6 && elapsed > 0.4f)
            {
                Require(effects.ActiveMarks > 0 && effects.ActiveMarks <= CarDrivingEffects.MarkBudget, "Grounded tire slip creates bounded skidmarks");
                int smoke = 0; foreach (var particles in effects.smoke) smoke += particles.particleCount;
                Require(smoke > 0, "Tire slip emits smoke");
                race.Restart();
                race.car.ResetTo(new Vector3(39.5f, 0.73f, 2f), Quaternion.Euler(0f, 90f, 0f));
                race.car.Body.linearVelocity = Vector3.right * 8f;
                Advance();
            }
            else if (stage == 7)
            {
                sawSparks |= effects.sparks.particleCount > 0;
                if (elapsed > 1f)
                {
                    Require(sawSparks, "Metal guardrail impact emits sparks");
                    Finish(true, null);
                }
            }
        }
        catch (Exception error) { Finish(false, error); }
    }

    static void Advance() { stage++; stageStart = EditorApplication.timeSinceStartup; }
    static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        results.Add("PASS: " + description);
    }
    static void Finish(bool success, Exception error)
    {
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        Directory.CreateDirectory("Logs");
        if (error != null) results.Add("FAIL: " + error);
        File.WriteAllLines("Logs/visual-play-checks.txt", results);
        if (success) Debug.Log("RACINGCAR_VISUAL_PLAY_CHECKS_OK"); else Debug.LogException(error);
        if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
}
