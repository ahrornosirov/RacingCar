using System;
using System.IO;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class QuestHandPlayChecks
{
    const string Key = "RacingCar.HandPlayCheck";
    static int stage;
    static double started, stageStarted;
    static RaceMenu menu;
    static QuestHandDriving hands;
    static XRHandSubsystem simulatedHands;
    static QuestHandPlayChecks()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
            { started = stageStarted = EditorApplication.timeSinceStartup; EditorApplication.update -= Tick; EditorApplication.update += Tick; }
        };
    }
    public static void Run()
    {
        SessionState.SetBool(Key + ".exists", PlayerPrefs.HasKey(QuestHandDriving.ModeKey));
        SessionState.SetInt(Key + ".mode", PlayerPrefs.GetInt(QuestHandDriving.ModeKey));
        PlayerPrefs.SetInt(QuestHandDriving.ModeKey, 0);
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        try
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - started > 40) throw new Exception("Hand play check timed out at stage " + stage);
            if (now - stageStarted < .5) return;
            if (stage == 0)
            {
                menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>(); hands = menu.hands;
                Require(hands != null && hands.Source != null && hands.UI != null && hands.handMaterial != null, "Runtime references are wired");
                Require(!hands.HandsEnabled, "Controller mode remains the default");
                if (simulatedHands == null)
                {
                    StartSimulatedHands();
                    stageStarted = now + 1;
                    return;
                }
                CheckHandVisibility();
                menu.garage.Preview(Array.FindIndex(menu.garage.vehicles, v => v.id == "porsche-911-turbo-s"));
                menu.ShowControls(); menu.handModeButton.onClick.Invoke();
                Require(hands.HandsEnabled && PlayerPrefs.GetInt(QuestHandDriving.ModeKey) == 1, "Menu selects and persists hand mode");
                Next();
            }
            else if (stage == 1)
            {
                menu.help.ForceMeshUpdate();
                Require(!menu.help.isTextOverflowing, "Hand control instructions fit the menu");
                RacingPreview.Export(menu.view.raceCamera, "Previews/hand-steering-controls.png");
                Require(!hands.TryGetInput(out _, out _, out _), "Desktop keyboard input is preserved without a headset");
                menu.Back(); menu.garage.Preview(1); menu.StartRace();
                Require(menu.HasStarted && !menu.IsOpen, "A saved Quest hands preference allows keyboard racing with other cars on desktop");
                menu.ShowHome();
                menu.garage.Preview(Array.FindIndex(menu.garage.vehicles, v => v.id == "porsche-911-turbo-s"));
                menu.race.countdownSeconds = 0; menu.StartRace(); Next(); stageStarted += 1;
            }
            else if (stage == 2)
            {
                Require(menu.HasStarted && !menu.IsOpen && menu.view.cockpit, "Hand mode starts Porsche in cockpit view");
                Require(hands.Presentation == menu.race.car.GetComponent<CarPresentation>(), "Hand wheel follows garage replacement");
                menu.race.car.GetComponent<VehicleCockpit>().display.gameObject.SetActive(false);
                hands.UI.UpdateInteraction(true); hands.UI.UpdateStatus();
                Canvas.ForceUpdateCanvases();
                RacingPreview.Export(menu.view.raceCamera, "Previews/hand-steering-dashboard-layout.png");
                var wheel = hands.WheelPosition;
                var rim = hands.WheelRotation * Vector3.right * .15f;
                var nearSide = menu.view.raceCamera.transform.forward * -.045f;
                VisibilityCheckProvider.leftPosition = hands.TrackingSpace.InverseTransformPoint(wheel - rim + nearSide);
                VisibilityCheckProvider.rightPosition = hands.TrackingSpace.InverseTransformPoint(wheel + rim + nearSide);
                simulatedHands.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
                RacingPreview.Export(menu.view.raceCamera, "Previews/hands3-cockpit-simulation.png");
                hands.SelectGear(true); Require(hands.Reverse, "Stopped car can select reverse");
                menu.race.car.Body.linearVelocity = menu.race.car.transform.forward * 5;
                hands.SelectGear(false); Require(hands.Reverse, "Moving car cannot switch gear");
                menu.race.car.Body.linearVelocity = Vector3.zero;
                menu.ShowPause(); menu.ShowControls(); menu.handModeButton.onClick.Invoke();
                Require(!hands.HandsEnabled, "Menu can return to controller controls");
                Next();
            }
            else
            {
                Require(hands.Presentation.HandWheelAngle == null, "Controller mode releases hand wheel visual ownership");
                Finish(0, "PASS: hand-mode menu, instructions layout, garage binding, cockpit start, desktop fallback, gear interlock and controller restore.\nHeadset tracking and ergonomics require Quest hardware testing.");
            }
        }
        catch (Exception exception) { Finish(1, exception.ToString()); }
    }
    static void StartSimulatedHands()
    {
        const string id = "RacingCar Editor Hand Check";
        XRHandSubsystemDescriptor.Register(new XRHandSubsystemDescriptor.Cinfo { id = id, providerType = typeof(VisibilityCheckProvider) });
        var descriptors = new List<XRHandSubsystemDescriptor>();
        SubsystemManager.GetSubsystemDescriptors(descriptors);
        simulatedHands = descriptors.Find(descriptor => descriptor.id == id).Create();
        foreach (var skeleton in hands.TrackingSpace.GetComponentsInChildren<XRHandSkeletonDriver>())
            VisibilityCheckProvider.CaptureRestPose(skeleton);
        simulatedHands.Start();
    }

    public class VisibilityCheckProvider : XRHandSubsystemProvider
    {
        public static XRHandSubsystem.UpdateSuccessFlags flags;
        static readonly Pose[] leftOffsets = new Pose[XRHandJointID.EndMarker.ToIndex()];
        static readonly Pose[] rightOffsets = new Pose[XRHandJointID.EndMarker.ToIndex()];
        static Quaternion leftRotation, rightRotation;
        public static Vector3 leftPosition = new Vector3(-.10f, -.10f, .57f);
        public static Vector3 rightPosition = new Vector3(.10f, -.10f, .57f);
        public static void CaptureRestPose(XRHandSkeletonDriver skeleton)
        {
            var offsets = skeleton.handTrackingEvents.handedness == Handedness.Left ? leftOffsets : rightOffsets;
            var wrist = skeleton.rootTransform;
            foreach (var joint in skeleton.jointTransformReferences)
                offsets[joint.xrHandJointID.ToIndex()] = new Pose(wrist.InverseTransformPoint(joint.jointTransform.position),
                    Quaternion.Inverse(wrist.rotation) * joint.jointTransform.rotation);
            var rest = Quaternion.LookRotation(offsets[XRHandJointID.MiddleTip.ToIndex()].position,
                offsets[XRHandJointID.ThumbTip.ToIndex()].position);
            if (skeleton.handTrackingEvents.handedness == Handedness.Left)
                leftRotation = Quaternion.LookRotation(Vector3.up, Vector3.right) * Quaternion.Inverse(rest);
            else
                rightRotation = Quaternion.LookRotation(Vector3.up, Vector3.left) * Quaternion.Inverse(rest);
        }
        public override void Start() { }
        public override void Stop() { }
        public override void Destroy() { }

        public override void GetHandLayout(NativeArray<bool> layout)
        {
            for (int i = 0; i < layout.Length; i++) layout[i] = true;
        }

        public override XRHandSubsystem.UpdateSuccessFlags TryUpdateHands(XRHandSubsystem.UpdateType phase,
            ref Pose leftRoot, NativeArray<XRHandJoint> left, ref Pose rightRoot, NativeArray<XRHandJoint> right)
        {
            leftRoot = new Pose(leftPosition, leftRotation);
            rightRoot = new Pose(rightPosition, rightRotation);
            for (int i = 0; i < left.Length; i++)
            {
                var id = XRHandJointIDUtility.FromIndex(i);
                var state = id == XRHandJointID.RingTip ? XRHandJointTrackingState.None : XRHandJointTrackingState.Pose;
                left[i] = XRHandProviderUtility.CreateJoint(Handedness.Left, state, id, leftOffsets[i].GetTransformedBy(leftRoot));
                right[i] = XRHandProviderUtility.CreateJoint(Handedness.Right, XRHandJointTrackingState.Pose, id, rightOffsets[i].GetTransformedBy(rightRoot));
            }
            return flags;
        }
    }

    static void CheckHandVisibility()
    {
        var skeletons = hands.TrackingSpace.GetComponentsInChildren<XRHandSkeletonDriver>();
        Require(skeletons.Length == 2, "Both XR hand models share the headset tracking space");
        VisibilityCheckProvider.flags = XRHandSubsystem.UpdateSuccessFlags.All;
        simulatedHands.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);
        foreach (var skeleton in skeletons)
        {
            var mesh = skeleton.GetComponentInChildren<SkinnedMeshRenderer>();
            bool left = skeleton.handTrackingEvents.handedness == Handedness.Left;
            var wrist = left ? simulatedHands.leftHand.rootPose : simulatedHands.rightHand.rootPose;
            Require(mesh.enabled && mesh.sharedMesh.vertexCount > 0 && mesh.sharedMaterial == hands.HandRenderMaterial
                && Vector3.Distance(skeleton.rootTransform.position, hands.TrackingSpace.TransformPoint(wrist.position)) < .001f,
                "Tracked wrist positions the visible rigged mesh for " + skeleton.handTrackingEvents.handedness);
        }
        var camera = menu.view.raceCamera;
        var canvas = menu.canvas;
        menu.desktopBrand.SetActive(false);
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;
        var rect = (RectTransform)canvas.transform;
        rect.sizeDelta = new Vector2(1000, 850);
        rect.localScale = Vector3.one * .00165f;
        rect.SetPositionAndRotation(camera.transform.position + camera.transform.forward * 1.8f, camera.transform.rotation);
        menu.card.anchorMin = menu.card.anchorMax = new Vector2(.5f, .5f);
        menu.card.anchoredPosition = Vector2.zero;
        RacingPreview.Export(camera, "Previews/hands3-menu-simulation.png");
        foreach (var skeleton in skeletons) skeleton.GetComponentInChildren<SkinnedMeshRenderer>().enabled = false;
        RacingPreview.Export(camera, "Temp/hands3-menu-without-hands.png");
        CheckMenuPixels(camera, menu.card);
        hands.RefreshHandVisuals();
        menu.ShowHome();
        var sample = hands.Source.left;
        Require(sample.Visible && !sample.Fresh && sample.AimFresh && hands.Source.right.Fresh,
            "Missing ring tip preserves hand visibility and menu pinch while blocking the affected driving grip");
        VisibilityCheckProvider.flags = XRHandSubsystem.UpdateSuccessFlags.None;
        simulatedHands.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
        Require(sample.Visible && !sample.Fresh && Array.TrueForAll(skeletons, s => s.GetComponentInChildren<SkinnedMeshRenderer>().enabled),
            "Empty before-render update preserves the most recent visible hands and cuts pedal input");
        hands.Source.left.timestamp = hands.Source.right.timestamp = Time.unscaledTime - 1f;
        hands.RefreshHandVisuals();
        Require(Array.TrueForAll(skeletons, s => !s.GetComponentInChildren<SkinnedMeshRenderer>().enabled),
            "Expired poses hide both hand models");
        VisibilityCheckProvider.flags = XRHandSubsystem.UpdateSuccessFlags.All;
        simulatedHands.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
        Require(Array.TrueForAll(skeletons, s => s.GetComponentInChildren<SkinnedMeshRenderer>().enabled),
            "Both meshes reappear when before-render tracking returns");
    }
    static void CheckMenuPixels(Camera camera, RectTransform card)
    {
        var on = new Texture2D(2, 2);
        var off = new Texture2D(2, 2);
        try
        {
            on.LoadImage(File.ReadAllBytes("Previews/hands3-menu-simulation.png"));
            off.LoadImage(File.ReadAllBytes("Temp/hands3-menu-without-hands.png"));
            var corners = new Vector3[4];
            card.GetWorldCorners(corners);
            var lo = camera.WorldToViewportPoint(corners[0]);
            var hi = camera.WorldToViewportPoint(corners[2]);
            var lit = on.GetPixels32();
            var background = off.GetPixels32();
            int leftPixels = 0, rightPixels = 0;
            for (int y = (int)(lo.y * on.height) + 2; y < hi.y * on.height - 2; y++)
            {
                for (int x = (int)(lo.x * on.width) + 2; x < hi.x * on.width - 2; x++)
                {
                    int i = y * on.width + x;
                    if (lit[i].b < background[i].b + 50 || lit[i].g < background[i].g + 50) continue;
                    if (x < on.width / 2) leftPixels++; else rightPixels++;
                }
            }
            Require(leftPixels > 100 && rightPixels > 100,
                $"Both hand meshes contribute visible pixels over the menu ({leftPixels} left, {rightPixels} right)");
        }
        finally { UnityEngine.Object.DestroyImmediate(on); UnityEngine.Object.DestroyImmediate(off); }
    }

    static void Next() { stage++; stageStarted = EditorApplication.timeSinceStartup; }
    static void Require(bool value, string message) { if (!value) throw new Exception(message); Debug.Log("HAND_PLAY_PASS: " + message); }
    static void Finish(int code, string message)
    {
        if (simulatedHands != null)
        {
            simulatedHands.Stop();
            simulatedHands.Destroy();
            simulatedHands = null;
        }
        SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
        if (SessionState.GetBool(Key + ".exists", false)) PlayerPrefs.SetInt(QuestHandDriving.ModeKey, SessionState.GetInt(Key + ".mode", 0));
        else PlayerPrefs.DeleteKey(QuestHandDriving.ModeKey);
        PlayerPrefs.Save();
        File.WriteAllText("Logs/hand-play-checks.txt", message);
        if (code == 0) Debug.Log("RACINGCAR_HAND_PLAY_OK"); else Debug.LogError(message);
        EditorApplication.isPlaying = false; EditorApplication.Exit(code);
    }
}
