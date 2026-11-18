using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.OpenXR;

// Rendering tolerates partially tracked fingers; driving requires all gesture joints.
public sealed class QuestHandSource : MonoBehaviour
{
    public sealed class Sample
    {
        public readonly Pose[] joints = new Pose[XRHandJointID.EndMarker.ToIndex()];
        public readonly bool[] valid = new bool[XRHandJointID.EndMarker.ToIndex()];
        public float timestamp = -100;
        public bool tracked, gestureValid, closed, systemGesture;
        public float pinch;
        public bool Visible => Time.unscaledTime - timestamp < 0.15f;
        public bool Fresh => tracked && Visible && gestureValid && !systemGesture;
        public bool AimFresh => tracked && Visible && !systemGesture && valid[XRHandJointID.ThumbTip.ToIndex()] && valid[XRHandJointID.IndexTip.ToIndex()];
        public Vector3 Palm => joints[XRHandJointID.Palm.ToIndex()].position;
    }
    public readonly Sample left = new Sample(), right = new Sample();
    readonly List<XRHandSubsystem> availableSubsystems = new List<XRHandSubsystem>();
    public event System.Action VisualsUpdated;
    XRHandSubsystem subsystem;
    float nextSearch;
    string previousDiagnostic;
    public bool SubsystemAvailable => subsystem != null;
    public bool SubsystemRunning => subsystem != null && subsystem.running;
    public string TrackingStatus => !SubsystemAvailable ? "Hand tracking unavailable"
        : !SubsystemRunning ? "Hand tracking stopped"
        : $"Left: {Describe(left)} / Right: {Describe(right)}";
    static string Describe(Sample sample) => !sample.Visible ? "not detected" : sample.systemGesture ? "system gesture"
        : !sample.gestureValid ? "partial tracking" : "tracked";

    void Update()
    {
        var available = HandTracking.subsystem;
        if (available == null && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 1f;
            SubsystemManager.GetSubsystems(availableSubsystems);
            foreach (var candidate in availableSubsystems)
                if (candidate.running) { available = candidate; break; }
        }
        // Retain a running provider discovered through SubsystemManager between searches.
        if (available == null && subsystem != null && subsystem.running) available = subsystem;
        if (subsystem != available)
        {
            Disconnect(); subsystem = available;
            if (subsystem != null) subsystem.updatedHands += Capture;
        }
        string diagnostic = TrackingStatus;
        if (previousDiagnostic != diagnostic)
        {
            Debug.Log("RACINGCAR_HANDS: " + diagnostic);
            previousDiagnostic = diagnostic;
        }
    }
    void Disconnect()
    {
        if (subsystem != null) subsystem.updatedHands -= Capture;
        subsystem = null; left.tracked = right.tracked = false;
        left.timestamp = right.timestamp = -100f;
    }
    void OnDisable() => Disconnect();
    void Capture(XRHandSubsystem source, XRHandSubsystem.UpdateSuccessFlags flags, XRHandSubsystem.UpdateType update)
    {
        // Accept both provider update phases; some runtimes have fresh joints only before rendering.
        Read(source.leftHand, left, (flags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != 0, MetaAimHand.left);
        Read(source.rightHand, right, (flags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != 0, MetaAimHand.right);
        VisualsUpdated?.Invoke();
    }
    static void Read(XRHand hand, Sample sample, bool updated, MetaAimHand aim)
    {
        sample.tracked = updated && hand.isTracked;
        if (sample.tracked) sample.timestamp = Time.unscaledTime;
        sample.systemGesture = aim != null && ((MetaAimFlags)(ulong)aim.aimFlags.ReadValue() & MetaAimFlags.SystemGesture) != 0;
        for (int i = 0; i < sample.joints.Length; i++)
            sample.valid[i] = sample.tracked && hand.GetJoint(XRHandJointIDUtility.FromIndex(i)).TryGetPose(out sample.joints[i]);
        EvaluateGestures(sample);
    }
    public static void EvaluateGestures(Sample sample)
    {
        sample.gestureValid = sample.tracked;
        foreach (var joint in Required) sample.gestureValid &= sample.valid[joint.ToIndex()];
        // Pinch UI needs only the pinch joints, not a complete driving grip.
        bool pinchValid = sample.valid[XRHandJointID.ThumbTip.ToIndex()] && sample.valid[XRHandJointID.IndexTip.ToIndex()];
        sample.pinch = pinchValid ? 1f - Mathf.InverseLerp(0.018f, 0.05f,
            Vector3.Distance(sample.joints[XRHandJointID.ThumbTip.ToIndex()].position, sample.joints[XRHandJointID.IndexTip.ToIndex()].position)) : 0;
        if (!sample.gestureValid) { sample.closed = false; return; }
        Vector3 palm = sample.Palm;
        float scale = Mathf.Clamp(Vector3.Distance(palm, sample.joints[XRHandJointID.MiddleProximal.ToIndex()].position), 0.025f, 0.07f);
        float middle = Vector3.Distance(palm, sample.joints[XRHandJointID.MiddleTip.ToIndex()].position) / scale;
        float ring = Vector3.Distance(palm, sample.joints[XRHandJointID.RingTip.ToIndex()].position) / scale;
        sample.closed = Mathf.Max(middle, ring) < (sample.closed ? 1.9f : 1.55f);
    }
    static readonly XRHandJointID[] Required = { XRHandJointID.Palm, XRHandJointID.ThumbTip, XRHandJointID.IndexTip,
        XRHandJointID.MiddleTip, XRHandJointID.RingTip, XRHandJointID.MiddleProximal };
}
