using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

// Diagnostic display only. Input and hand rendering belong to Unity's official sample rig.
public sealed class QuestHandsBaseline : MonoBehaviour
{
    public TMP_Text status, result;
    public Button firstButton, secondButton;
    public Transform panel;
    public Camera headsetCamera;
    readonly List<XRHandSubsystem> subsystems = new List<XRHandSubsystem>();
    int firstCount, secondCount;
    bool placed;
    string previousStatus;
    float nextStatus;
    void Start()
    {
        Time.timeScale = 1; AudioListener.pause = false;
        firstButton.onClick.AddListener(() => { firstCount++; RefreshResult(); });
        secondButton.onClick.AddListener(() => { secondCount++; RefreshResult(); });
        RefreshResult();
    }
    void RefreshResult() => result.text = $"BUTTON A: {firstCount}     BUTTON B: {secondCount}\nA click increases its counter.";
    void Update()
    {
        if (!placed)
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked)
            {
                Quaternion yaw = Quaternion.Euler(0, headsetCamera.transform.eulerAngles.y, 0);
                panel.SetPositionAndRotation(headsetCamera.transform.position + yaw * new Vector3(0, -.05f, 1.25f), yaw);
                placed = true;
            }
        }
        if (Time.unscaledTime < nextStatus) return;
        nextStatus = Time.unscaledTime + .25f;
        SubsystemManager.GetSubsystems(subsystems);
        var running = subsystems.Find(s => s.running);
        string message = running == null ? "HAND PROVIDER: " + (subsystems.Count == 0 ? "UNAVAILABLE" : "STOPPED")
            : $"LEFT: {(running.leftHand.isTracked ? "TRACKED" : "NOT DETECTED")}\nRIGHT: {(running.rightHand.isTracked ? "TRACKED" : "NOT DETECTED")}";
        status.text = message;
        if (message != previousStatus) { Debug.Log("HANDS_BASELINE: " + message.Replace('\n', ' ')); previousStatus = message; }
    }
}
