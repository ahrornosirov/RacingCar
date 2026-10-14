using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class RacingRaceModePlayChecks
{
    const string SessionKey = "RacingCar.RaceModeChecks";
    const string RecordKey = "RacingCar.Check.RaceMode";
    static RaceMenu menu;
    static RaceSession race;
    static Keyboard keyboard;
    static int stage;
    static double stageStart, started;
    static float pausedCountdown, finishedTime, bestLap, bestRace;
    static InputSettings.EditorInputBehaviorInPlayMode originalEditorInput;
    static InputSettings.BackgroundBehavior originalBackground;
    static bool changedInputSettings;
    static bool sawOne;
    static readonly List<string> results = new List<string>();
    [Serializable] public class Records { public float lap, race; }

    static RacingRaceModePlayChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
            started = stageStart = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        };
        if (SessionState.GetBool(SessionKey, false)) EditorApplication.update += Tick;
    }

    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before starting race checks.");
        stage = 0; started = 0; sawOne = false; changedInputSettings = false; results.Clear();
        DeleteTestRecords();
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        SessionState.SetBool(SessionKey, true); EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(SessionKey, false)) return;
        try
        {
            double now = EditorApplication.timeSinceStartup, elapsed = now - stageStart;
            if (started > 0 && now - started > 50) throw new InvalidOperationException("Race mode check timed out at stage " + stage);
            if (stage == 0 && elapsed > 0.4)
            {
                menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>(); race = menu.race;
                race.recordKey = RecordKey; race.LoadRecords();
                originalEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
                originalBackground = InputSystem.settings.backgroundBehavior; changedInputSettings = true;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                keyboard = InputSystem.AddDevice<Keyboard>("RaceModeKeyboard"); keyboard.MakeCurrent();
                Require(race.CurrentPhase == RaceSession.Phase.Ready && race.Paused && race.LapTarget == 3, "Home waits for a three-lap race");
                Require(RaceDashboard.FormatTime(59.999f) == "01:00.00", "Time display rolls over correctly at one minute");
                menu.StartRace(); Keys(Key.W); Advance();
            }
            else if (stage == 1 && elapsed > 0.3)
            {
                Require(race.CurrentPhase == RaceSession.Phase.Countdown && !race.CanDrive && race.LapTime == 0 && race.RaceTime == 0 && race.Throttle == 0 && race.car.Throttle == 0, "Countdown blocks held acceleration and both clocks");
                race.car.Body.linearVelocity = race.car.transform.forward * 5;
                race.PassCheckpoint(1, race.car.transform.forward);
                Require(race.NextCheckpoint == 1, "Countdown rejects checkpoints");
                race.car.Body.linearVelocity = Vector3.zero;
                var ui = UnityEngine.Object.FindAnyObjectByType<RaceCountdownUI>();
                Require(ui.canvas.gameObject.activeSelf && ui.number.text == "3", "Visible 3 countdown over the selected camera");
                RacingPreview.Export(menu.view.raceCamera, "Previews/race-countdown.png");
                Advance();
            }
            else if (stage == 2 && elapsed > 0.85)
            {
                Require(UnityEngine.Object.FindAnyObjectByType<RaceCountdownUI>().number.text == "2", "Countdown progresses to 2");
                Keys(Key.P); Advance();
            }
            else if (stage == 3 && elapsed > 0.2)
            {
                Require(menu.CurrentPage == RaceMenu.Page.Pause && race.Paused, "Countdown can be paused");
                pausedCountdown = race.CountdownRemaining; Keys(); Advance();
            }
            else if (stage == 4 && elapsed > 0.4)
            {
                Require(Mathf.Abs(race.CountdownRemaining - pausedCountdown) < 0.001f, "Pause freezes countdown");
                menu.play.onClick.Invoke(); Advance();
            }
            else if (stage == 5)
            {
                sawOne |= UnityEngine.Object.FindAnyObjectByType<RaceCountdownUI>().number.text == "1";
                if (race.CurrentPhase != RaceSession.Phase.Racing) return;
                Require(sawOne, "Countdown progresses to 1 before GO");
                Require(race.ShowGo && race.CanDrive && race.RaceTime < 0.15f, "GO releases driving and starts clocks after countdown");
                RacingPreview.Export(menu.view.raceCamera, "Previews/race-go.png");
                Keys(Key.W); Advance();
            }
            else if (stage == 6 && elapsed > 1.8)
            {
                Require(race.car.SpeedKph > 10, "Car accelerates after GO"); Keys();
                float elapsedRace = race.RaceTime;
                race.Restart();
                Require(race.RaceTime == elapsedRace && race.LapTime == 0 && race.Resets == 1, "Lap reset keeps total race time");
                race.car.Body.linearVelocity = race.car.transform.forward * 5;
                race.PassCheckpoint(0, race.car.transform.forward); race.PassCheckpoint(3, race.car.transform.forward);
                Require(race.CompletedLaps == 0 && race.NextCheckpoint == 1, "Skipped gates cannot finish a lap");
                race.car.Body.linearVelocity = -race.car.transform.forward * 5;
                race.PassCheckpoint(1, race.car.transform.forward);
                Require(race.NextCheckpoint == 1, "Reverse crossing is rejected");
                race.car.Body.linearVelocity = Vector3.zero; Advance();
            }
            else if (stage == 7 && elapsed > 0.4)
            {
                CompleteLap(); Require(race.CompletedLaps == 1 && race.LapNumber == 2 && race.CurrentPhase == RaceSession.Phase.Racing, "First ordered lap advances to lap 2"); Advance();
            }
            else if (stage == 8 && elapsed > 0.65)
            {
                CompleteLap(); Require(race.CompletedLaps == 2 && race.LapNumber == 3 && race.CurrentPhase == RaceSession.Phase.Racing, "Second lap advances to final lap"); Advance();
            }
            else if (stage == 9 && elapsed > 0.85)
            {
                CompleteLap(); finishedTime = race.RaceTime; bestLap = race.BestLap; bestRace = race.BestRace;
                Require(race.CurrentPhase == RaceSession.Phase.Finished && race.CompletedLaps == 3 && race.LapTimes.Count == 3 && !race.CanDrive && Time.timeScale == 0, "Third lap finishes and freezes the race");
                Require(menu.CurrentPage == RaceMenu.Page.Results && menu.IsOpen && menu.resultDetails.gameObject.activeSelf && menu.resultDetails.text.Contains("LAP 3") && menu.play.GetComponentInChildren<TMPro.TMP_Text>().text == "RACE AGAIN", "Results show all splits, totals and Race Again");
                Require(race.NewRaceRecord && race.NewLapRecord && bestLap > 0 && bestRace == finishedTime, "First completed race establishes saved records");
                Advance();
            }
            else if (stage == 10 && elapsed > 0.3)
            {
                race.SetPaused(false); race.PassCheckpoint(0, race.car.transform.forward); menu.Resume();
                Require(race.RaceTime == finishedTime && race.CompletedLaps == 3 && race.Paused && menu.CurrentPage == RaceMenu.Page.Results, "Finished race rejects resume and extra laps");
                RacingPreview.Export(menu.view.raceCamera, "Previews/race-results.png");
                Keys(Key.Enter); Advance();
            }
            else if (stage == 11 && elapsed > 0.25)
            {
                Require(race.CurrentPhase == RaceSession.Phase.Countdown && race.CompletedLaps == 0 && race.LapTimes.Count == 0 && race.RaceTime == 0 && race.BestRace == bestRace && !menu.IsOpen, "Keyboard Race Again starts a fresh countdown and preserves records");
                Keys(); menu.ShowPause(); menu.restart.onClick.Invoke();
                Require(race.CountdownRemaining == 3 && !menu.IsOpen, "Pause restart begins a full race countdown");
                menu.ShowHome();
                Require(race.CurrentPhase == RaceSession.Phase.Ready && race.Paused && menu.view.showcase, "Home cancels the countdown and parks the car");
                race.countdownSeconds = 0; menu.StartRace(); Advance();
            }
            else if (stage >= 12 && stage <= 14 && elapsed > 2.0)
            {
                CompleteLap();
                if (stage == 14)
                {
                    Require(race.BestLap == bestLap && race.BestRace == bestRace && !race.NewLapRecord && !race.NewRaceRecord, "Slower race keeps earlier records");
                    race.LoadRecords();
                    Require(race.BestLap == bestLap && race.BestRace == bestRace, "Saved records reload in the running game");
                    File.WriteAllText("Logs/race-record-check.json", JsonUtility.ToJson(new Records { lap = bestLap, race = bestRace }));
                    Finish(true, null);
                }
                else Advance();
            }
        }
        catch (Exception error) { Finish(false, error); }
    }

    static void CompleteLap()
    {
        race.car.Body.linearVelocity = race.car.transform.forward * 5;
        for (int i = 1; i < race.checkpointCount; i++) race.PassCheckpoint(i, race.car.transform.forward);
        race.PassCheckpoint(0, race.car.transform.forward);
        race.car.Body.linearVelocity = Vector3.zero;
    }
    static void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    static void Advance() { stage++; stageStart = EditorApplication.timeSinceStartup; }
    static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
        results.Add("PASS: " + message);
    }
    static void Finish(bool success, Exception error)
    {
        SessionState.SetBool(SessionKey, false); EditorApplication.update -= Tick;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        if (changedInputSettings)
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorInput;
            InputSystem.settings.backgroundBehavior = originalBackground;
        }
        if (error != null) results.Add("FAIL: " + error);
        Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/race-mode-play-checks.txt", results);
        if (success) Debug.Log("RACINGCAR_RACE_MODE_CHECKS_OK"); else { DeleteTestRecords(); Debug.LogException(error); }
        if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1); else EditorApplication.isPlaying = false;
    }

    public static void VerifySavedRecords()
    {
        try
        {
            EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
            var session = UnityEngine.Object.FindAnyObjectByType<RaceSession>(); session.recordKey = RecordKey; session.LoadRecords();
            var expected = JsonUtility.FromJson<Records>(File.ReadAllText("Logs/race-record-check.json"));
            if (Mathf.Abs(session.BestLap - expected.lap) > 0.001f || Mathf.Abs(session.BestRace - expected.race) > 0.001f)
                throw new InvalidOperationException("Records did not survive an editor restart.");
            session.lapsToFinish = 4; session.LoadRecords();
            if (session.BestRace != 0 || Mathf.Abs(session.BestLap - expected.lap) > 0.001f)
                throw new InvalidOperationException("Race record leaked across race distances.");
            File.AppendAllText("Logs/race-mode-play-checks.txt", "PASS: records survive editor restart; race records scoped by lap count\n");
            Debug.Log("RACINGCAR_RECORD_RESTART_CHECK_OK");
        }
        finally { DeleteTestRecords(); }
    }
    static void DeleteTestRecords()
    {
        PlayerPrefs.DeleteKey(RecordKey + ".BestLap"); PlayerPrefs.DeleteKey(RecordKey + ".BestRace.3"); PlayerPrefs.Save();
    }
}
