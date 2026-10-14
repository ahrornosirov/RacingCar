using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class RacingMenuPlayChecks
{
    const string SessionKey = "RacingCar.MenuPlayChecks";
    static RaceMenu menu;
    static Keyboard keyboard;
    static Mouse mouse;
    static int stage;
    static double stageStart, started;
    static float pausedLap, bestLap;
    static InputSettings.EditorInputBehaviorInPlayMode originalEditorInput;
    static InputSettings.BackgroundBehavior originalBackground;
    static bool changedInputSettings;
    static GameObject cameraWall;
    static bool checkedObstacle;
    static readonly List<string> results = new List<string>();

    static RacingMenuPlayChecks()
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
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        SessionState.SetBool(SessionKey, true); EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(SessionKey, false)) return;
        try
        {
            double now = EditorApplication.timeSinceStartup, elapsed = now - stageStart;
            if (started > 0 && now - started > 45) throw new InvalidOperationException("Menu check timed out at stage " + stage);
            if (stage == 0)
            {
                menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>();
                if (menu == null || !menu.IsOpen || EventSystem.current == null || elapsed < 0.4) return;
                Require(!menu.HasStarted && menu.CurrentPage == RaceMenu.Page.Home && menu.race.Paused && Time.timeScale == 0, "Home menu freezes race before Start");
                Require(menu.view.cockpit && menu.view.showcase, "First-person default and car showcase");
                RacingPreview.Export(menu.view.raceCamera, "Previews/main-menu.png");
                // Synthetic device events need a game-input route in a windowless editor.
                originalEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
                originalBackground = InputSystem.settings.backgroundBehavior;
                changedInputSettings = true;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                keyboard = InputSystem.AddDevice<Keyboard>("MenuCheckKeyboard"); keyboard.MakeCurrent();
                mouse = InputSystem.AddDevice<Mouse>("MenuCheckMouse"); mouse.MakeCurrent();
                Vector2 position = ClickPoint();
                results.Add($"Mouse state: {mouse.position.ReadValue()}, pressed {mouse.leftButton.isPressed}, enabled {mouse.enabled}, point {position}");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
                Advance();
            }
            else if (stage == 1 && elapsed > 0.2)
            {
                Vector2 position = ClickPoint();
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
                Advance();
            }
            else if (stage == 2 && elapsed > 0.3)
            {
                Require(!menu.view.cockpit && menu.IsOpen, "Mouse click selects third person without starting race");
                EventSystem.current.SetSelectedGameObject(menu.play.gameObject);
                Keys(Key.Enter); Advance();
            }
            else if (stage == 3 && elapsed > 0.25)
            {
                Require(menu.HasStarted && !menu.IsOpen && !menu.race.Paused && Time.timeScale == 1, "Keyboard submit starts selected race view");
                Vector3 offset = menu.view.raceCamera.transform.position - menu.race.car.transform.position;
                Require(Vector3.Dot(offset, menu.race.car.transform.forward) < -3f && offset.y > 1f, "Third-person camera follows behind and above car");
                cameraWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cameraWall.name = "Camera test wall";
                cameraWall.transform.position = menu.race.car.transform.position + Vector3.up * 1.4f - menu.race.car.transform.forward * 3f;
                cameraWall.transform.localScale = new Vector3(4f, 3f, 0.2f);
                Physics.SyncTransforms();
                Keys(); Advance();
            }
            else if (stage == 4)
            {
                if (!checkedObstacle && elapsed > 0.2)
                {
                    Require(Vector3.Distance(menu.view.raceCamera.transform.position, menu.race.car.transform.position) < 3.5f, "Chase camera moves in front of an obstruction");
                    UnityEngine.Object.Destroy(cameraWall); checkedObstacle = true; Keys(Key.W);
                    stageStart = EditorApplication.timeSinceStartup;
                }
                else if (checkedObstacle && elapsed > 2.5)
                {
                    Require(menu.race.car.SpeedKph > 10, "Driving input works after leaving menu");
                    RacingPreview.Export(menu.view.raceCamera, "Previews/third-person.png");
                    Keys(Key.W, Key.P); Advance();
                }
            }
            else if (stage == 5 && elapsed > 0.2)
            {
                Require(menu.IsOpen && menu.CurrentPage == RaceMenu.Page.Pause && menu.race.Paused, "P opens pause menu while driving");
                pausedLap = menu.race.LapTime; Keys(Key.W); Advance();
            }
            else if (stage == 6 && elapsed > 0.4)
            {
                Require(Mathf.Abs(menu.race.LapTime - pausedLap) < 0.001f && menu.race.Throttle == 0, "Paused menu blocks throttle and freezes lap time");
                menu.controls.onClick.Invoke(); Keys(); Advance();
            }
            else if (stage == 7 && elapsed > 0.2)
            {
                Require(menu.CurrentPage == RaceMenu.Page.Controls && menu.help.gameObject.activeSelf && !menu.play.gameObject.activeSelf, "Controls page shows instructions");
                RacingPreview.Export(menu.view.raceCamera, "Previews/controls-menu.png");
                Keys(Key.Escape); Advance();
            }
            else if (stage == 8 && elapsed > 0.25)
            {
                Require(menu.CurrentPage == RaceMenu.Page.Pause && menu.race.Paused, "Escape returns Controls to Pause");
                menu.firstPerson.onClick.Invoke(); Keys(); Advance();
            }
            else if (stage == 9 && elapsed > 0.2)
            {
                Require(menu.view.cockpit && menu.race.Paused, "Camera changes while paused");
                RacingPreview.Export(menu.view.raceCamera, "Previews/pause-menu.png");
                menu.play.onClick.Invoke(); Advance();
            }
            else if (stage == 10 && elapsed > 0.2)
            {
                Require(!menu.IsOpen && !menu.race.Paused && menu.race.LapTime >= pausedLap, "Resume preserves current lap");
                Vector3 expected = menu.race.car.transform.position + Quaternion.Euler(0, menu.race.car.transform.eulerAngles.y, 0) * menu.view.cockpitOffset;
                Require(Vector3.Distance(expected, menu.view.transform.position) < 0.03f, "First-person camera returns to cockpit");
                Keys(Key.C); Advance();
            }
            else if (stage == 11 && elapsed > 0.2)
            {
                Require(!menu.view.cockpit && !menu.IsOpen, "C switches camera during race");
                Keys();
                var race = menu.race; race.car.Body.linearVelocity = race.car.transform.forward * 5f;
                for (int i = 1; i < race.checkpointCount; i++) race.PassCheckpoint(i, race.car.transform.forward);
                race.PassCheckpoint(0, race.car.transform.forward); bestLap = race.BestLap;
                Require(race.CompletedLaps == 1 && bestLap > 0, "Completed lap establishes session best");
                menu.ShowPause(); menu.restart.onClick.Invoke(); Advance();
            }
            else if (stage == 12 && elapsed > 0.2)
            {
                Require(!menu.IsOpen && !menu.race.Paused && menu.race.CompletedLaps == 1 && menu.race.LapTime < 0.4f && menu.race.BestLap == bestLap, "Restart Lap resets current lap and keeps completed laps/best");
                menu.ShowPause(); menu.mainMenu.onClick.Invoke(); Advance();
            }
            else if (stage == 13 && elapsed > 0.2)
            {
                Require(menu.IsOpen && menu.CurrentPage == RaceMenu.Page.Home && !menu.HasStarted && menu.race.Paused && menu.view.showcase, "Main Menu returns to parked showcase");
                menu.firstPerson.onClick.Invoke(); menu.play.onClick.Invoke(); Advance();
            }
            else if (stage == 14 && elapsed > 0.2)
            {
                Require(menu.race.CompletedLaps == 0 && menu.race.LastLap == 0 && menu.race.BestLap == bestLap && !menu.race.Paused, "Start Race creates a fresh race and retains best lap");
                Finish(true, null);
            }
        }
        catch (Exception error) { Finish(false, error); }
    }

    static void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    static Vector2 ClickPoint()
    {
        var rectangle = (RectTransform)menu.thirdPerson.transform;
        Camera camera = menu.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : menu.view.raceCamera;
        return RectTransformUtility.WorldToScreenPoint(camera, rectangle.TransformPoint(rectangle.rect.center));
    }
    static void Advance() { stage++; stageStart = EditorApplication.timeSinceStartup; }
    static void Require(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        results.Add("PASS: " + description);
    }
    static void Finish(bool success, Exception error)
    {
        SessionState.SetBool(SessionKey, false); EditorApplication.update -= Tick;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        if (changedInputSettings)
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorInput;
            InputSystem.settings.backgroundBehavior = originalBackground;
        }
        if (error != null) results.Add("FAIL: " + error);
        Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/menu-play-checks.txt", results);
        if (success) Debug.Log("RACINGCAR_MENU_PLAY_CHECKS_OK"); else Debug.LogException(error);
        if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1); else EditorApplication.isPlaying = false;
    }
}
