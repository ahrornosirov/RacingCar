using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Hands;

// A small hand-ray adapter for the existing uGUI menus; does not replace their controller navigation.
public sealed class QuestHandUI : MonoBehaviour
{
    public QuestHandDriving driver;
    public bool UsingUI { get; private set; }
    public Canvas panel { get; private set; }
    public TMP_Text status { get; private set; }
    Button drive, reverse, seat, pause;
    readonly List<Button> candidates = new List<Button>();
    readonly Pointer left = new Pointer(), right = new Pointer();
    sealed class Pointer
    {
        public bool released;
        public Button hover;
        public float hoverSince;
        public LineRenderer line;
    }
    public void UpdateInteraction(bool xr)
    {
        if (panel == null) CreatePanel();
        bool drivingPanel = xr && driver.HandsEnabled && !driver.menu.IsOpen;
        panel.gameObject.SetActive(drivingPanel);
        if (drivingPanel)
        {
            if (driver.Supported && driver.view.cockpit)
                panel.transform.SetPositionAndRotation(driver.WheelPosition + driver.race.car.transform.TransformDirection(new Vector3(.34f, .24f, .16f)),
                    Quaternion.Euler(0, driver.race.car.transform.eulerAngles.y, 0));
            else
                panel.transform.SetPositionAndRotation(driver.view.raceCamera.transform.TransformPoint(new Vector3(0, -.22f, .9f)),
                    driver.view.raceCamera.transform.rotation);
            drive.interactable = reverse.interactable = seat.interactable = driver.race.car.SpeedKph < 1.5f;
        }
        candidates.Clear(); UsingUI = false;
        if (xr)
        {
            if (driver.menu.IsOpen) driver.menu.canvas.GetComponentsInChildren(false, candidates);
            else if (drivingPanel) panel.GetComponentsInChildren(false, candidates);
        }
        bool clicked = Point(left, driver.Source.left, MetaAimHand.left, xr, true);
        Point(right, driver.Source.right, MetaAimHand.right, xr, !clicked);
    }
    bool Point(Pointer pointer, QuestHandSource.Sample sample, MetaAimHand aim, bool xr, bool allowClick)
    {
        if (pointer.line == null)
        {
            var obj = new GameObject("Hand menu ray"); obj.transform.SetParent(transform, false);
            pointer.line = obj.AddComponent<LineRenderer>(); pointer.line.sharedMaterial = driver.HandRenderMaterial;
            pointer.line.sortingOrder = driver.menu.canvas.sortingOrder + 1;
            pointer.line.startWidth = .003f; pointer.line.endWidth = .001f; pointer.line.positionCount = 2;
            pointer.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        bool valid = xr && sample.AimFresh && aim != null
            && ((MetaAimFlags)(ulong)aim.aimFlags.ReadValue() & MetaAimFlags.Valid) != 0;
        // A curled wheel hand's pinch is a pedal, never a dashboard click.
        valid &= driver.menu.IsOpen || !sample.closed;
        pointer.line.enabled = false;
        if (!valid) { pointer.hover = null; pointer.released = false; return false; }
        var origin = driver.TrackingSpace;
        var ray = new Ray(origin.TransformPoint(aim.devicePosition.ReadValue()), origin.rotation * aim.deviceRotation.ReadValue() * Vector3.forward);
        Button target = null; float nearest = 5f;
        foreach (var button in candidates)
        {
            if (!button.isActiveAndEnabled || !button.IsInteractable()) continue;
            var rect = (RectTransform)button.transform;
            if (HitRect(ray, rect, out float distance) && distance < nearest) { target = button; nearest = distance; }
        }
        if (pointer.hover != target) { pointer.hover = target; pointer.hoverSince = Time.unscaledTime; }
        if (sample.pinch < .2f) pointer.released = true;
        bool pressed = sample.pinch > .8f && pointer.released;
        if (sample.pinch > .8f) pointer.released = false; // Consume even a pinch in empty space.
        pointer.line.enabled = driver.menu.IsOpen || target != null;
        pointer.line.SetPosition(0, ray.origin); pointer.line.SetPosition(1, ray.GetPoint(target == null ? 1.5f : nearest));
        if (target == null) return false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target.gameObject);
        if (!pressed || !allowClick || Time.unscaledTime - pointer.hoverSince < .12f) return false;
        UsingUI = true;
        target.onClick.Invoke(); driver.wheel.DisarmPedals(); return true;
    }
    public static bool HitRect(Ray ray, RectTransform rect, out float distance)
    {
        if (new Plane(rect.forward, rect.position).Raycast(ray, out distance) && distance >= .02f)
        {
            Vector3 local = rect.InverseTransformPoint(ray.GetPoint(distance));
            return rect.rect.Contains(new Vector2(local.x, local.y));
        }
        return false;
    }
    public void UpdateStatus()
    {
        if (status == null || !panel.gameObject.activeSelf) return;
        status.text = $"{driver.race.car.SpeedKph:0} KM/H   LAP {driver.race.LapNumber}/{driver.race.LapTarget}   |   " + (driver.Reverse ? "R / REVERSE 12 KM/H" : "D / DRIVE 60 KM/H") + "   |   "
            + (driver.wheel.LeftHeld ? "L GRIP " : "") + (driver.wheel.RightHeld ? "R GRIP" : "")
            + "\n" + driver.Status;
    }
    void CreatePanel()
    {
        var go = new GameObject("Hand driving dashboard", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(transform, false); panel = go.GetComponent<Canvas>(); panel.renderMode = RenderMode.WorldSpace;
        panel.worldCamera = driver.view.raceCamera; panel.sortingOrder = 20;
        var root = (RectTransform)go.transform; root.sizeDelta = new Vector2(720, 190); root.localScale = Vector3.one * .0007f;
        go.AddComponent<Image>().color = new Color(.025f, .04f, .06f, .96f);
        status = Label(root, "Hand status", new Vector2(680, 85), new Vector2(0, 40)); status.fontSize = 24;
        drive = MakeButton("D / DRIVE", -265, () => driver.SelectGear(false));
        reverse = MakeButton("R / REVERSE", -88, () => driver.SelectGear(true));
        seat = MakeButton("RECENTER", 88, driver.RecenterSeat);
        pause = MakeButton("PAUSE", 265, driver.menu.ShowPause);
        go.SetActive(false);
    }
    Button MakeButton(string name, float x, UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(panel.transform, false);
        var rect = (RectTransform)obj.transform; rect.sizeDelta = new Vector2(166, 58); rect.anchoredPosition = new Vector2(x, -55);
        var img = obj.GetComponent<Image>(); img.color = new Color(.12f, .23f, .29f);
        var button = obj.GetComponent<Button>(); button.targetGraphic = img; button.onClick.AddListener(action);
        var colors = button.colors; colors.selectedColor = new Color(1f, .65f, .25f); button.colors = colors;
        var label = Label(rect, name + " label", new Vector2(160, 54), Vector2.zero); label.text = name; label.fontSize = 23;
        return button;
    }
    TMP_Text Label(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(parent, false);
        var rect = (RectTransform)obj.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
        var text = obj.GetComponent<TextMeshProUGUI>(); text.font = driver.menu.help.font;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; text.color = Color.white;
        text.enableAutoSizing = true; text.fontSizeMin = 18; text.fontSizeMax = 24;
        return text;
    }
    void OnDisable()
    {
        UsingUI = false; left.released = right.released = false;
        if (left.line != null) left.line.enabled = false;
        if (right.line != null) right.line.enabled = false;
        if (panel != null) panel.gameObject.SetActive(false);
    }
}
