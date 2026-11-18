using TMPro;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

// Runs after the moving/recentered camera rig and menu have placed their world-space UI.
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(QuestHandSource), typeof(QuestHandUI))]
public sealed class QuestHandDriving : MonoBehaviour
{
    public const string ModeKey = "RacingCar.HandSteering";
    public RaceSession race;
    public RaceMenu menu;
    public RacerCameraRig view;
    public Material handMaterial;
    public Material HandRenderMaterial { get; private set; }
    public GameObject leftHandPrefab, rightHandPrefab;
    public Transform TrackingSpace => view.raceCamera.transform.parent;
    public bool HandsEnabled { get; private set; }
    public bool Reverse { get; private set; }
    public bool Supported => race.car != null && race.car.profile != null && race.car.profile.id == "porsche-911-turbo-s";
    public string Status { get; private set; } = "Choose Porsche in Garage for hand steering";
    public readonly HandWheelState wheel = new HandWheelState();
    public QuestHandSource Source { get; private set; }
    public QuestHandUI UI { get; private set; }
    public CarPresentation Presentation { get; private set; }
    public Quaternion WheelRotation => Presentation.steeringWheel.parent.rotation * Presentation.SteeringRest;
    public Vector3 WheelPosition => Presentation.steeringWheel.position;
    TMP_Text cockpitDisplay;
    ArcadeCar boundCar;
    int resetVersion, recenterVersion;
    float evaluatedAt = -100;
    bool focused = true;
    SkinnedMeshRenderer leftVisual, rightVisual;

    void Awake()
    {
        // The world-space menu draws above the cockpit. Draw hands and hand rays after its labels.
        HandRenderMaterial = new Material(handMaterial) { renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay + 1 };
        Source = GetComponent<QuestHandSource>(); UI = GetComponent<QuestHandUI>(); UI.driver = this;
        HandsEnabled = PlayerPrefs.GetInt(ModeKey, 0) == 1;
    }
    void OnEnable() { if (Source != null) Source.VisualsUpdated += OnHandVisualsUpdated; }
    void OnHandVisualsUpdated() => RefreshHandVisuals();

    void Start()
    {
        leftVisual = CreateHand(leftHandPrefab);
        rightVisual = CreateHand(rightHandPrefab);
    }

    SkinnedMeshRenderer CreateHand(GameObject prefab)
    {
        var model = Instantiate(prefab, TrackingSpace, false);
        var events = model.GetComponent<XRHandTrackingEvents>();
        events.updateType = XRHandTrackingEvents.UpdateTypes.Dynamic | XRHandTrackingEvents.UpdateTypes.BeforeRender;
        // The skeleton follows Unity's joint events. Visibility uses the last valid pose
        // so a render callback without new joints cannot hide an otherwise tracked hand.
        model.GetComponent<XRHandMeshController>().enabled = false;
        var mesh = model.GetComponentInChildren<SkinnedMeshRenderer>();
        mesh.sharedMaterial = HandRenderMaterial;
        mesh.sortingOrder = menu.canvas.sortingOrder + 1;
        mesh.updateWhenOffscreen = true;
        mesh.enabled = false;
        return mesh;
    }
    public void SetHandsEnabled(bool enabled)
    {
        HandsEnabled = enabled;
        PlayerPrefs.SetInt(ModeKey, enabled ? 1 : 0); PlayerPrefs.Save();
        Reverse = false; wheel.Reset();
        if (Presentation != null) Presentation.HandWheelAngle = null;
        if (enabled) view.SetView(true);
    }
    public void SelectGear(bool reverse)
    {
        if (race.car.SpeedKph >= 1.5f) return;
        Reverse = reverse; wheel.DisarmPedals();
    }
    public void RecenterSeat() { view.Recenter(); wheel.Reset(); }

    void LateUpdate()
    {
        if (boundCar != race.car)
        {
            if (Presentation != null) Presentation.HandWheelAngle = null;
            if (cockpitDisplay != null) cockpitDisplay.gameObject.SetActive(true);
            boundCar = race.car; Presentation = boundCar.GetComponent<CarPresentation>();
            cockpitDisplay = boundCar.GetComponent<VehicleCockpit>().display;
            wheel.Reset(); Reverse = false;
        }
        if (resetVersion != boundCar.ResetVersion || recenterVersion != view.RecenterVersion)
        {
            resetVersion = boundCar.ResetVersion; recenterVersion = view.RecenterVersion; wheel.Reset();
        }
        bool xr = XRSettings.isDeviceActive;
        if (cockpitDisplay != null) cockpitDisplay.gameObject.SetActive(!(HandsEnabled && xr && Supported && !menu.IsOpen));
        RefreshHandVisuals();
        UI.UpdateInteraction(xr && focused);
        bool blocked = !HandsEnabled || !xr || !focused || !Supported || !view.cockpit || view.showcase
            || menu.BlocksDriving || UI.UsingUI;
        var left = MakeHand(Source.left);
        var right = MakeHand(Source.right);
        wheel.Step(left, right, Time.unscaledDeltaTime, boundCar.SpeedKph, Reverse, blocked);
        evaluatedAt = Time.unscaledTime;
        if (Presentation != null) Presentation.HandWheelAngle = HandsEnabled && xr && Supported && !menu.IsOpen ? wheel.Angle : (float?)null;
        Status = !Supported ? "Choose Porsche in Garage for hand steering"
            : !xr ? "Hand steering ready for Quest headset"
            : !view.cockpit ? "Select FIRST PERSON for hand steering"
            : !left.tracked || !right.tracked ? Source.TrackingStatus + " / braking"
            : !(wheel.LeftHeld || wheel.RightHeld) ? "Open hands, then curl middle + ring fingers around rim"
            : !wheel.PedalsArmed ? "Release both thumb-index pinches to enable pedals"
            : "Right pinch: GAS  |  Left pinch: BRAKE";
        UI.UpdateStatus();
        if (xr && menu.IsOpen && (menu.CurrentPage == RaceMenu.Page.Home || menu.CurrentPage == RaceMenu.Page.Controls || menu.CurrentPage == RaceMenu.Page.Pause))
            menu.footer.text = "HANDS TEST 3 / " + Source.TrackingStatus + "\n"
                + (!focused ? "Close the Quest system menu to resume" : "Point and pinch, or use controller stick + A");
    }

    public void RefreshHandVisuals()
    {
        // System gestures or missing pedal joints must not make tracked hands disappear.
        if (leftVisual != null) leftVisual.enabled = Source.left.Visible;
        if (rightVisual != null) rightVisual.enabled = Source.right.Visible;
    }

    HandWheelState.Hand MakeHand(QuestHandSource.Sample sample)
    {
        return new HandWheelState.Hand {
            tracked = sample.Fresh, closed = sample.closed, pinch = sample.pinch,
            position = Presentation == null ? Vector3.zero : Quaternion.Inverse(WheelRotation)
                * (TrackingSpace.TransformPoint(sample.Palm) - WheelPosition)
        };
    }
    public bool TryGetInput(out float throttle, out float steer, out float brake)
    {
        throttle = steer = 0; brake = 0.65f;
        if (!HandsEnabled || !XRSettings.isDeviceActive) return false;
        if (!focused || !Supported || !view.cockpit || menu.BlocksDriving || UI.UsingUI
            || Time.unscaledTime - evaluatedAt > 0.15f) return true;
        steer = wheel.Steering;
        if (!Source.left.Fresh || !Source.right.Fresh) return true;
        throttle = wheel.Throttle; brake = wheel.Brake;
        return true;
    }
    void OnDestroy() { if (HandRenderMaterial != null) Destroy(HandRenderMaterial); }
    void OnApplicationFocus(bool value) { focused = value; wheel.Reset(); }
    void OnDisable()
    {
        if (Source != null) Source.VisualsUpdated -= OnHandVisualsUpdated;
        wheel.Reset();
        if (Presentation != null) Presentation.HandWheelAngle = null;
        if (cockpitDisplay != null) cockpitDisplay.gameObject.SetActive(true);
        if (leftVisual != null) leftVisual.enabled = false;
        if (rightVisual != null) rightVisual.enabled = false;
    }
}
