using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using XRUsages = UnityEngine.XR.CommonUsages;

[DefaultExecutionOrder(-200)]
public sealed class RaceMenu : MonoBehaviour
{
    public enum Page { Home, Pause, Controls }
    public RaceSession race;
    public RacerCameraRig view;
    public Canvas canvas;
    public RectTransform card;
    public GameObject desktopBrand;
    public TMP_Text title, subtitle, cameraLabel, help, footer;
    public Button play, firstPerson, thirdPerson, restart, controls, mainMenu, quit, back;
    public Canvas chaseHud;
    public TMP_Text chaseHudText;
    public Page CurrentPage { get; private set; }
    public bool HasStarted { get; private set; }
    public bool IsOpen => canvas.gameObject.activeSelf;
    public bool BlocksDriving => IsOpen || inputFrame == Time.frameCount;
    Page controlsReturn;
    int inputFrame = -1;
    bool previousConfirm, previousBack, previousPause, previousCamera, wasXR;
    float nextNavigation;

    void Awake() => race.SetPaused(true);

    void Start()
    {
        play.onClick.AddListener(() => { if (CurrentPage == Page.Pause) Resume(); else StartRace(); });
        firstPerson.onClick.AddListener(() => SelectCamera(true));
        thirdPerson.onClick.AddListener(() => SelectCamera(false));
        restart.onClick.AddListener(RestartLap);
        controls.onClick.AddListener(ShowControls);
        mainMenu.onClick.AddListener(ShowHome);
        quit.onClick.AddListener(Quit);
        back.onClick.AddListener(Back);
        ShowHome();
    }

    public void ShowHome()
    {
        HasStarted = false;
        Open(Page.Home);
        view.SetShowcase(true);
    }

    public void StartRace()
    {
        HasStarted = true;
        race.BeginNewRace();
        Close();
    }

    public void ShowPause()
    {
        if (!HasStarted) return;
        view.SetShowcase(false);
        Open(Page.Pause);
    }

    public void Resume()
    {
        if (!HasStarted) return;
        race.SetPaused(false);
        Close();
    }

    public void RestartLap()
    {
        race.Restart();
        Resume();
    }

    public void SelectCamera(bool cockpit)
    {
        inputFrame = Time.frameCount;
        view.SetView(cockpit);
        Refresh();
    }

    public void ShowControls()
    {
        controlsReturn = CurrentPage;
        Open(Page.Controls);
    }

    public void Back()
    {
        if (CurrentPage == Page.Controls) Open(controlsReturn);
        else if (CurrentPage == Page.Pause) Resume();
    }

    void Open(Page page)
    {
        CurrentPage = page;
        inputFrame = Time.frameCount;
        race.SetPaused(true);
        race.Throttle = race.Steering = race.Brake = 0f;
        race.Handbrake = false;
        canvas.gameObject.SetActive(true);
        Refresh();
        ConfigureCanvas();
        SelectDefault();
        Cursor.visible = true;
    }

    void Close()
    {
        inputFrame = Time.frameCount;
        canvas.gameObject.SetActive(false);
        view.SetShowcase(false);
        view.SetView(view.cockpit);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void Refresh()
    {
        bool info = CurrentPage == Page.Controls, paused = CurrentPage == Page.Pause;
        title.text = info ? "HOW TO DRIVE" : paused ? "PAUSED" : "READY TO RACE?";
        subtitle.text = info ? "Desktop and Quest controls" : paused ? "Take a breath. Your lap is waiting." : "Choose your view and take the starting line.";
        cameraLabel.text = "CAMERA  /  " + (view.cockpit ? "FIRST PERSON" : "THIRD PERSON");
        cameraLabel.gameObject.SetActive(!info);
        Set(play, !info, paused ? "RESUME RACE" : "START RACE", 118f);
        Set(firstPerson, !info, "FIRST PERSON" + (view.cockpit ? "   /   SELECTED" : ""), 200f);
        Set(thirdPerson, !info, "THIRD PERSON" + (!view.cockpit ? "   /   SELECTED" : ""), 274f);
        Set(restart, paused, "RESTART LAP", 348f);
        Set(controls, !info, "CONTROLS", paused ? 422f : 348f);
        Set(mainMenu, paused, "MAIN MENU", 496f);
        Set(quit, !info && !paused, "QUIT GAME", 422f);
        Set(back, info, "BACK", 496f);
        help.gameObject.SetActive(info);
        help.text = "DESKTOP\nW / Up: accelerate   S / Down: brake\nA / D: steer   Q: reverse   Space: handbrake\nC: camera   P / Esc: pause   R: reset lap\nRight mouse: look / orbit   Home: recenter\nPage Up / Down: cockpit seat height\n\nQUEST\nRight trigger: accelerate   Left trigger: brake\nLeft stick: steer   Pull back: reverse\nY: camera   X: pause   B: reset lap\nA: recenter while racing\n\nMENU\nMouse or arrows + Enter on desktop\nLeft stick up/down + A on Quest; B goes back";
        footer.text = info ? "Car Concept: Eric Chadwick / Darmstadt Graphics Group GmbH\nCC BY 4.0 - creativecommons.org/licenses/by/4.0" : "Mouse / arrows + Enter   |   Quest: left stick + A\nC / Y changes the camera. P / X pauses the race.";
    }

    static void Set(Button button, bool active, string label, float y)
    {
        button.gameObject.SetActive(active);
        button.GetComponentInChildren<TMP_Text>().text = label;
        ((RectTransform)button.transform).anchoredPosition = new Vector2(0f, -y);
    }

    void SelectDefault()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject((CurrentPage == Page.Controls ? back : play).gameObject);
    }

    void ConfigureCanvas()
    {
        bool xr = XRSettings.isDeviceActive;
        wasXR = xr;
        desktopBrand.SetActive(!xr);
        var rectangle = (RectTransform)canvas.transform;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (xr)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            rectangle.sizeDelta = new Vector2(1000f, 850f);
            rectangle.localScale = Vector3.one * 0.00165f;
            // Place once when opened so the panel stays still while the player looks around.
            Quaternion facing = Quaternion.Euler(0f, view.raceCamera.transform.eulerAngles.y, 0f);
            rectangle.SetPositionAndRotation(view.raceCamera.transform.position + facing * Vector3.forward * 1.8f, facing);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = view.raceCamera;
            canvas.planeDistance = 1f;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            card.anchorMin = card.anchorMax = new Vector2(0.76f, 0.5f);
        }
        card.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.pKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
            {
                if (IsOpen) Back(); else ShowPause();
            }
            if (IsOpen && keyboard.cKey.wasPressedThisFrame) SelectCamera(!view.cockpit);
        }
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        left.TryGetFeatureValue(XRUsages.primary2DAxis, out Vector2 stick);
        left.TryGetFeatureValue(XRUsages.primaryButton, out bool pause);
        left.TryGetFeatureValue(XRUsages.secondaryButton, out bool camera);
        right.TryGetFeatureValue(XRUsages.primaryButton, out bool confirm);
        right.TryGetFeatureValue(XRUsages.secondaryButton, out bool cancel);
        if (camera && !previousCamera) SelectCamera(!view.cockpit);
        if (pause && !previousPause) { if (IsOpen) Back(); else ShowPause(); }
        if (IsOpen)
        {
            if (Mathf.Abs(stick.y) < 0.35f) nextNavigation = 0f;
            else if (Mathf.Abs(stick.y) > 0.65f && Time.unscaledTime >= nextNavigation)
            {
                Navigate(stick.y > 0f ? -1 : 1);
                nextNavigation = Time.unscaledTime + 0.28f;
            }
            if (cancel && !previousBack) Back();
            else if (confirm && !previousConfirm && EventSystem.current != null)
            {
                var selected = EventSystem.current.currentSelectedGameObject;
                if (selected != null && selected.TryGetComponent<Button>(out var button) && button.IsInteractable()) button.onClick.Invoke();
            }
        }
        previousConfirm = confirm; previousBack = cancel; previousPause = pause; previousCamera = camera;
        if (IsOpen && XRSettings.isDeviceActive != wasXR) ConfigureCanvas();
        bool showHud = XRSettings.isDeviceActive && HasStarted && !IsOpen && !view.cockpit;
        chaseHud.gameObject.SetActive(showHud);
        if (showHud) chaseHudText.text = $"{race.car.SpeedKph:0} KM/H    LAP {race.CompletedLaps + 1}\n{RaceDashboard.FormatTime(race.LapTime)}    GATE {race.NextCheckpoint + 1}/{race.checkpointCount}";
    }

    void Navigate(int step)
    {
        var buttons = new[] { play, firstPerson, thirdPerson, restart, controls, mainMenu, quit, back };
        var active = new System.Collections.Generic.List<Button>();
        foreach (var button in buttons) if (button.gameObject.activeSelf && button.IsInteractable()) active.Add(button);
        if (active.Count == 0 || EventSystem.current == null) return;
        int index = active.FindIndex(b => b.gameObject == EventSystem.current.currentSelectedGameObject);
        index = (index + step + active.Count) % active.Count;
        EventSystem.current.SetSelectedGameObject(active[index].gameObject);
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
