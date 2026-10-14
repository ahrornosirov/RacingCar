using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

[DefaultExecutionOrder(200)]
public sealed class RaceCountdownUI : MonoBehaviour
{
    public RaceSession race;
    public RaceMenu menu;
    public RacerCameraRig view;
    public Canvas canvas;
    public TMP_Text number, caption, detail;
    bool visible, wasXR;

    void LateUpdate()
    {
        bool show = !menu.IsOpen && (race.CurrentPhase == RaceSession.Phase.Countdown || race.ShowGo);
        bool xr = XRSettings.isDeviceActive;
        canvas.gameObject.SetActive(show);
        if (show)
        {
            if (!visible || xr != wasXR) Place(xr);
            number.text = race.ShowGo ? "GO!" : Mathf.CeilToInt(race.CountdownRemaining).ToString();
            caption.text = race.ShowGo ? "THE CLOCK IS RUNNING" : "GET READY";
            detail.text = $"CIRCUIT 01  /  {race.LapTarget} LAPS";
        }
        visible = show; wasXR = xr;
    }

    void Place(bool xr)
    {
        var rect = (RectTransform)canvas.transform;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (xr)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            rect.sizeDelta = new Vector2(720, 400);
            rect.localScale = Vector3.one * 0.0015f;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            Quaternion facing = Quaternion.Euler(0, view.raceCamera.transform.eulerAngles.y, 0);
            rect.SetPositionAndRotation(view.raceCamera.transform.position + facing * Vector3.forward * 1.8f, facing);
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rect.localScale = Vector3.one;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
    }
}
