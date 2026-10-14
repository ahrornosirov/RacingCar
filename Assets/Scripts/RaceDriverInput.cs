using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

public sealed class RaceDriverInput : MonoBehaviour
{
    public RaceSession race;
    public RacerCameraRig view;
    public RaceMenu menu;
    bool previousReset, previousPause, previousRecenter;

    void Update()
    {
        if (menu != null && menu.BlocksDriving)
        {
            race.Throttle = race.Steering = race.Brake = 0f;
            race.Handbrake = false;
            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            device.TryGetFeatureValue(XRCommonUsages.secondaryButton, out previousReset);
            device.TryGetFeatureValue(XRCommonUsages.primaryButton, out previousRecenter);
            return;
        }
        float throttle = 0f, steer = 0f, brake = 0f;
        bool reset = false, pause = false, handbrake = false;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            throttle = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.qKey.isPressed ? 1f : 0f);
            steer = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            brake = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f;
            handbrake = keyboard.spaceKey.isPressed;
            reset = keyboard.rKey.wasPressedThisFrame;
            pause = menu == null && (keyboard.pKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame);
            if (keyboard.cKey.wasPressedThisFrame) view.ToggleDesktopView();
        }
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (left.isValid || right.isValid)
        {
            left.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 stick);
            right.TryGetFeatureValue(XRCommonUsages.trigger, out float acceleration);
            left.TryGetFeatureValue(XRCommonUsages.trigger, out float braking);
            right.TryGetFeatureValue(XRCommonUsages.secondaryButton, out bool resetButton);
            left.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool pauseButton);
            right.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool recenterButton);
            steer = stick.x;
            throttle = stick.y < -0.6f ? -0.5f : acceleration;
            brake = braking;
            reset |= resetButton && !previousReset;
            pause |= menu == null && pauseButton && !previousPause;
            previousReset = resetButton;
            previousPause = pauseButton;
            if (recenterButton && !previousRecenter) view.Recenter();
            previousRecenter = recenterButton;
        }
        race.Throttle = throttle;
        race.Steering = steer;
        race.Brake = brake;
        race.Handbrake = handbrake;
        if (reset) race.Restart();
        if (pause) race.TogglePause();
    }

    void OnApplicationFocus(bool focus)
    {
        if (!focus && !race.Paused)
        {
            if (menu != null) menu.ShowPause(); else race.SetPaused(true);
        }
    }
}
