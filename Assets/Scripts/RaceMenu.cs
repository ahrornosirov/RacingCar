using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using XRUsages = UnityEngine.XR.CommonUsages;

[DefaultExecutionOrder(100)]
public sealed class RaceMenu : MonoBehaviour
{
    public enum Page { Home, Pause, Controls, Results, Garage, Tracks }
    public QuestHandDriving hands;
    public Button handModeButton;
    public RaceTrackSelection tracks;
    public Button tracksButton, previousTrack, nextTrack;
    public RaceGarage garage;
    public Button garageButton, previousCar, nextCar;
    public RaceSession race;
    public RacerCameraRig view;
    public Canvas canvas;
    public RectTransform card;
    public GameObject desktopBrand;
    public TMP_Text title, subtitle, cameraLabel, help, footer, summary;
    public Button play, firstPerson, thirdPerson, restart, controls, mainMenu, quit, back;
    public Canvas chaseHud;
    public TMP_Text chaseHudText;
    public TMP_Text resultDetails;
    public Page CurrentPage { get; private set; }
    public bool HasStarted { get; private set; }
    public bool IsOpen => canvas.gameObject.activeSelf;
    public bool BlocksDriving => IsOpen || !race.CanDrive || inputFrame == Time.frameCount;
    Page controlsReturn;
    int inputFrame = -1;
    bool previousConfirm, previousBack, previousPause, previousCamera, wasXR;
    float nextNavigation;
    bool placePanel;

    void Awake() => race.SetPaused(true);

    void Start()
    {
        race.Finished += ShowResults;
        play.onClick.AddListener(() => { if (CurrentPage == Page.Tracks) { tracks.Confirm(); ShowHome(); } else if (CurrentPage == Page.Garage) { garage.Confirm(); ShowHome(); } else if (CurrentPage == Page.Pause) Resume(); else StartRace(); });
        firstPerson.onClick.AddListener(() => SelectCamera(true));
        thirdPerson.onClick.AddListener(() => SelectCamera(false));
        restart.onClick.AddListener(StartRace);
        controls.onClick.AddListener(ShowControls);
        mainMenu.onClick.AddListener(ShowHome);
        quit.onClick.AddListener(Quit);
        back.onClick.AddListener(Back);
        if (garageButton != null)
        {
            garageButton.onClick.AddListener(() => { view.SetShowcase(true); Open(Page.Garage); });
            previousCar.onClick.AddListener(() => BrowseCar(-1));
            nextCar.onClick.AddListener(() => BrowseCar(1));
        }
        if (tracksButton != null)
        {
            tracksButton.onClick.AddListener(() => { view.SetShowcase(true); Open(Page.Tracks); });
            previousTrack.onClick.AddListener(() => BrowseTrack(-1));
            nextTrack.onClick.AddListener(() => BrowseTrack(1));
        }
        if (handModeButton != null) handModeButton.onClick.AddListener(() => {
            hands.SetHandsEnabled(!hands.HandsEnabled); Refresh();
        });
        ShowHome();
    }

    public void ShowHome()
    {
        HasStarted = false;
        race.ReturnHome();
        view.SetShowcase(true);
        Open(Page.Home);
    }

    public void StartRace()
    {
        if (HandDrivingActive)
        {
            if (!hands.Supported) { ShowControls(); subtitle.text = "Select Porsche in Garage, or switch to Controllers."; return; }
            view.SetView(true);
        }
        HasStarted = true;
        race.BeginNewRace();
        Close();
    }

    public void ShowPause()
    {
        if (!HasStarted || race.CurrentPhase == RaceSession.Phase.Finished) return;
        view.SetShowcase(false);
        Open(Page.Pause);
    }

    public void Resume()
    {
        if (!HasStarted || race.CurrentPhase == RaceSession.Phase.Finished) return;
        race.SetPaused(false);
        Close();
    }

    public void ShowResults()
    {
        view.SetShowcase(false);
        Open(Page.Results);
    }

    public void SelectCamera(bool cockpit)
    {
        if (!cockpit && HandDrivingActive && HasStarted) return;
        inputFrame = Time.frameCount;
        view.SetShowcase(CurrentPage == Page.Garage && !cockpit);
        view.SetView(cockpit);
        placePanel = IsOpen;
        Refresh();
    }

    public void ShowControls()
    {
        controlsReturn = CurrentPage;
        Open(Page.Controls);
    }

    void BrowseCar(int direction) { garage.Browse(direction); Refresh(); placePanel = true; }
    void BrowseTrack(int direction) { tracks.Browse(direction); Refresh(); placePanel = true; }

    public void Back()
    {
        if (CurrentPage == Page.Tracks) { tracks.Cancel(); ShowHome(); }
        else if (CurrentPage == Page.Garage) { garage.Cancel(); ShowHome(); }
        else if (CurrentPage == Page.Controls) Open(controlsReturn);
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
        placePanel = true;
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
        bool results = CurrentPage == Page.Results;
        bool inGarage = CurrentPage == Page.Garage;
        bool choosingTrack = CurrentPage == Page.Tracks;
        bool homeWithTracks = CurrentPage == Page.Home && tracks != null;
        string trackName = tracks != null ? tracks.Current.displayName.ToUpperInvariant() : "CIRCUIT 01";
        if (tracksButton != null)
        {
            Set(tracksButton, CurrentPage == Page.Home, "SELECT MAP", 386f);
            Set(previousTrack, choosingTrack, "<  PREVIOUS MAP", 118f);
            Set(nextTrack, choosingTrack, "NEXT MAP  >", 192f);
            previousTrack.interactable = nextTrack.interactable = tracks.tracks.Length > 1;
        }
        if (garageButton != null)
        {
            Set(garageButton, CurrentPage == Page.Home, "GARAGE", homeWithTracks ? 324f : 348f);
            Set(previousCar, inGarage, "<  PREVIOUS CAR", 118f);
            Set(nextCar, inGarage, "NEXT CAR  >", 192f);
        }
        resultDetails.gameObject.SetActive(results);
        if (results)
        {
            var details = new System.Text.StringBuilder();
            details.Append($"<color=#FFBF60><size=135%>{RaceDashboard.FormatTime(race.RaceTime)}</size></color>\nTOTAL RACE TIME\n\n");
            for (int i = 0; i < race.LapTimes.Count; i++) details.Append($"LAP {i + 1}       {RaceDashboard.FormatTime(race.LapTimes[i])}\n");
            details.Append($"\nBEST LAP       {RaceDashboard.FormatTime(race.BestLap)}\nBEST RACE     {RaceDashboard.FormatTime(race.BestRace)}");
            if (race.Resets > 0) details.Append($"\n{race.Resets} LAP {(race.Resets == 1 ? "RESET" : "RESETS")} - INCLUDED IN TOTAL");
            resultDetails.text = details.ToString();
        }
        title.text = info ? "HOW TO DRIVE" : paused ? "PAUSED" : "READY TO RACE?";
        if (results) title.text = "RACE COMPLETE";
        subtitle.text = info ? "Desktop and Quest controls" : paused ? "Take a breath. Your lap is waiting." : "Choose your view and take the starting line.";
        if (results) subtitle.text = $"{trackName}  /  {race.LapTarget} LAPS FINISHED";
        cameraLabel.text = "CAMERA  /  " + (view.cockpit ? "FIRST PERSON" : "THIRD PERSON");
        cameraLabel.gameObject.SetActive(!info && !results);
        Set(play, !info, results ? "RACE AGAIN" : paused ? "RESUME RACE" : "START RACE", results ? 500f : 118f);
        Set(firstPerson, !info && !results, "FIRST PERSON" + (view.cockpit ? "   /   SELECTED" : ""), 200f);
        Set(thirdPerson, !info && !results, "THIRD PERSON" + (!view.cockpit ? "   /   SELECTED" : ""), homeWithTracks ? 262f : 274f);
        Set(restart, paused, "RESTART RACE", 348f);
        Set(controls, !info && !results, "CONTROLS", homeWithTracks ? 448f : 422f);
        Set(mainMenu, paused || results, "MAIN MENU", results ? 580f : 496f);
        Set(quit, !info && !paused && !results, "QUIT GAME", homeWithTracks ? 510f : 496f);
        Set(back, info, "BACK", 580f);
        summary.gameObject.SetActive(!info && !results);
        summary.rectTransform.anchoredPosition = new Vector2(0f, paused ? -602f : -580f);
        summary.text = paused ? $"LAP {race.LapNumber}/{race.LapTarget}   /   {RaceDashboard.FormatTime(race.LapTime)}\nRACE   {RaceDashboard.FormatTime(race.RaceTime)}" : $"{trackName}  /  {race.LapTarget} LAPS\nBEST LAP   {RaceDashboard.FormatTime(race.BestLap)}\nBEST RACE   {RaceDashboard.FormatTime(race.BestRace)}";
        help.gameObject.SetActive(info);
        help.text = "DESKTOP\nW / Up: accelerate   S / Down: brake\nA / D: steer   Q: reverse   Space: handbrake\nC: camera   P / Esc: pause   R: reset lap\nRight mouse: look / orbit   Home: recenter\nPage Up / Down: cockpit seat height\n\nQUEST\nRight trigger: accelerate   Left trigger: brake\nLeft stick: steer   Pull back: reverse\nY: camera   X: pause   B: reset lap\nA: recenter while racing\n\nMENU\nMouse or arrows + Enter on desktop\nLeft stick up/down + A on Quest; B goes back";
        footer.text = info ? "Car Concept: Eric Chadwick / Darmstadt Graphics Group GmbH\nCC BY 4.0 - creativecommons.org/licenses/by/4.0" : "Mouse / arrows + Enter   |   Quest: left stick + A\nC / Y changes the camera. P / X pauses the race.";
        if (inGarage)
        {
            var vehicle = garage.Current;
            title.text = "GARAGE";
            subtitle.text = $"{garage.PreviewIndex + 1}/{garage.vehicles.Length}  /  {vehicle.displayName}";
            cameraLabel.gameObject.SetActive(false);
            Set(play, true, "USE THIS CAR", 266f);
            Set(firstPerson, true, "COCKPIT PREVIEW", 462f);
            Set(thirdPerson, true, "EXTERIOR PREVIEW", 536f);
            Set(back, true, "BACK", 610f);
            controls.gameObject.SetActive(false); quit.gameObject.SetActive(false);
            summary.rectTransform.anchoredPosition = new Vector2(0, -344f);
            summary.text = $"LIMIT {vehicle.topSpeedKph:0} KM/H   /   {vehicle.drivetrain.ToString().ToUpper()} DRIVE\n{vehicle.mass:0} KG   /   {vehicle.brakingLabel}\n{vehicle.accelerationLabel}\n{vehicle.description}";
            footer.text = vehicle.credit;
        }
        else if (CurrentPage == Page.Home && garage != null)
            subtitle.text = garage.Current.displayName;
        if (choosingTrack)
        {
            title.text = "SELECT MAP";
            subtitle.text = $"{tracks.PreviewIndex + 1}/{tracks.tracks.Length}  /  {tracks.Current.displayName}";
            cameraLabel.gameObject.SetActive(false);
            Set(play, true, "USE THIS MAP", 266f);
            Set(back, true, "BACK", 610f);
            firstPerson.gameObject.SetActive(false); thirdPerson.gameObject.SetActive(false);
            controls.gameObject.SetActive(false); quit.gameObject.SetActive(false);
            summary.rectTransform.anchoredPosition = new Vector2(0, -344f);
            summary.text = tracks.Current.description + $"\n{race.checkpointCount} CHECKPOINTS  /  {race.LapTarget} LAPS";
            footer.text = tracks.Current.credit;
        }
        foreach (var button in new[] { play, firstPerson, thirdPerson, garageButton, tracksButton, controls, quit })
            if (button != null)
            {
                var rect = (RectTransform)button.transform;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, homeWithTracks ? 54f : 64f);
            }
        var series = desktopBrand.transform.Find("Series");
        if (series != null) series.GetComponent<TMP_Text>().text = "QUEST RACING  /  " + trackName;
        if (handModeButton != null)
        {
            Set(handModeButton, info, hands.HandsEnabled ? "INPUT: HANDS / SWITCH TO CONTROLLERS" : "INPUT: CONTROLLERS / SWITCH TO HANDS", 530f);
            if (info)
            {
                help.rectTransform.sizeDelta = new Vector2(540, 390);
                Set(back, true, "BACK", 610f);
                if (hands.HandsEnabled)
                    help.text = "HAND STEERING / PORSCHE / FIRST PERSON\n\nOpen hands near the wheel, then curl your middle\nand ring fingers around the rim. Turn either hand\nor both hands to steer. Keep thumb + index free.\n\nRIGHT thumb-index pinch: accelerate\nLEFT thumb-index pinch: brake (overrides gas)\nRelease both grips or lose tracking: automatic brake\nRelease both pinches after a pause or tracking loss.\n\nDashboard: D / R, Recenter, Pause\nStop before changing gear or recentering.\nTest limits: 60 km/h forward, 12 km/h reverse.\n\nMENUS: point an open hand, then pinch to select.\nStart with hands open; use a well-lit play area.";
            }
            if (CurrentPage == Page.Home || paused)
                controls.GetComponentInChildren<TMP_Text>().text = "CONTROLS / " + (hands.HandsEnabled ? "HANDS" : "CONTROLLERS");
            if (HandDrivingActive && hands.Supported && (CurrentPage == Page.Home || paused)) thirdPerson.interactable = false;
            else thirdPerson.interactable = true;
            if (!info && !inGarage && !choosingTrack && !results)
                footer.text = "Hands: point, then thumb-index pinch to select\n" + (hands.HandsEnabled ? "Porsche / first person / gesture pedals" : "Controllers: left stick + A / B to go back");
        }
        if (results) footer.text = race.NewRaceRecord ? "NEW RACE RECORD!  /  SAVED ON THIS DEVICE" : race.NewLapRecord ? "NEW LAP RECORD!  /  SAVED ON THIS DEVICE" : "PERSONAL BESTS SAVED ON THIS DEVICE";
    }

    bool HandDrivingActive => XRSettings.isDeviceActive && hands != null && hands.HandsEnabled;

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
        desktopBrand.SetActive(!xr && CurrentPage == Page.Home);
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
            rectangle.localScale = Vector3.one;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = view.raceCamera;
            canvas.planeDistance = 1f;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            card.anchorMin = card.anchorMax = new Vector2(CurrentPage == Page.Results ? 0.5f : 0.76f, 0.5f);
        }
        card.anchoredPosition = Vector2.zero;
    }

    void LateUpdate()
    {
        // Camera LateUpdate runs first: opening Home or changing view moves the rig.
        if (placePanel && IsOpen) { ConfigureCanvas(); placePanel = false; }
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
            if ((IsOpen || race.CurrentPhase == RaceSession.Phase.Countdown) && keyboard.cKey.wasPressedThisFrame) SelectCamera(!view.cockpit);
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
        if (showHud) chaseHudText.text = $"{race.car.SpeedKph:0} KM/H    LAP {race.LapNumber}/{race.LapTarget}\n{RaceDashboard.FormatTime(race.LapTime)}    GATE {race.NextCheckpoint + 1}/{race.checkpointCount}";
    }

    void Navigate(int step)
    {
        var buttons = new[] { previousTrack, nextTrack, previousCar, nextCar, play, firstPerson, thirdPerson, garageButton, tracksButton, restart, controls, handModeButton, mainMenu, quit, back };
        var active = new System.Collections.Generic.List<Button>();
        foreach (var button in buttons) if (button != null && button.gameObject.activeSelf && button.IsInteractable()) active.Add(button);
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

    void OnDestroy() { if (race != null) race.Finished -= ShowResults; }
}
