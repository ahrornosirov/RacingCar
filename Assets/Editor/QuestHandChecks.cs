using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class QuestHandChecks
{
    static readonly List<string> results = new List<string>();
    static HandWheelState.Hand Hand(float degrees, bool closed = true, float pinch = 0)
    {
        float angle = degrees * Mathf.Deg2Rad;
        return new HandWheelState.Hand { tracked = true, closed = closed, pinch = pinch,
            position = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * .18f };
    }
    static void Grab(HandWheelState state)
    {
        state.Reset(); state.Step(Hand(180, false), Hand(0, false), .02f, 0, false, false);
        state.Step(Hand(180), Hand(0), .02f, 0, false, false);
        state.Step(Hand(180), Hand(0), .02f, 0, false, false);
    }
    public static void Run()
    {
        results.Clear();
        var state = new HandWheelState();
        state.Step(Hand(180), Hand(0, true, 1), .02f, 0, false, false);
        Require(!state.RightHeld && state.Throttle == 0, "Closed hands at startup cannot accidentally grab or accelerate");
        Grab(state);
        Require(state.LeftHeld && state.RightHeld && state.PedalsArmed, "Open then curl grips either side of wheel and arms released pedals");
        for (int i = 1; i <= 9; i++) state.Step(Hand(180 - i * 5), Hand(-i * 5, true, 1), .02f, 20, false, false);
        Require(state.Steering > .2f && Mathf.Abs(state.Angle + 45) < .01f, "Two hands turning clockwise yield right steering without doubling angle");
        float old = state.Angle;
        state.Step(Hand(135), Hand(-45, false), .02f, 20, false, false);
        state.Step(Hand(130), Hand(-60), .02f, 20, false, false);
        Require(Mathf.Abs(state.Angle - old) <= 5.1f, "Regrabbing second hand introduces no angular snap");
        Grab(state);
        for (int i = 0; i < 100; i++) state.Step(Hand(180), Hand(0, true, 1), .02f, 20, false, false);
        Require(state.Throttle > .5f && state.Brake == 0, "Right pinch accelerates independently of curled grip");
        state.Step(Hand(180, true, 1), Hand(0, true, 1), .02f, 20, false, false);
        Require(state.Throttle == 0 && state.Brake == 1, "Left pinch immediately overrides accelerator with full brake");
        var lost = Hand(0, true, 1); lost.tracked = false;
        state.Step(Hand(180), lost, .02f, 20, false, false);
        Require(state.Throttle == 0 && state.Brake >= .65f && !state.PedalsArmed, "Single-hand tracking loss cuts motor and brakes");
        for (int i = 0; i < 20; i++) state.Step(Hand(180), Hand(0, true, 1), .02f, 20, false, false);
        Require(state.Throttle == 0, "Tracking recovery with held pinch cannot resume acceleration");
        Grab(state);
        state.Step(Hand(180), Hand(0, true, 1), .02f, 0, false, true);
        Require(!state.LeftHeld && state.Throttle == 0 && state.Brake > .5f, "Menu, focus or reset interruption releases wheel and pedals");
        Grab(state);
        for (int i = 0; i < 80; i++) state.Step(Hand(180), Hand(0, true, 1), .02f, 0, true, false);
        Require(state.Throttle < -.5f, "Reverse gear produces negative torque demand");
        state.Step(Hand(180), Hand(0, true, 1), .02f, 15, true, false);
        Require(state.Throttle == 0 && state.Brake >= .3f, "Reverse prototype speed cap removes power and brakes overspeed");
        Grab(state);
        for (int i = 0; i < 90; i++) state.Step(Hand(180), Hand(0, true, 1), .02f, 0, false, false);
        state.Step(Hand(180, false), Hand(0, false), .02f, 25, false, false);
        Require(state.Throttle == 0 && state.Brake >= .65f, "Releasing both grips stops acceleration and brakes");
        Grab(state);
        state.Step(Hand(180), Hand(110), .02f, 0, false, false);
        Require(!state.RightHeld && Mathf.Abs(state.Angle) < .01f, "Tracking teleport is rejected instead of injecting steering");
        // Angular seam at +/-180 must not create a 360-degree steering jump.
        Grab(state); state.Step(Hand(175), Hand(-5), .02f, 0, false, false);
        state.Step(Hand(185), Hand(5), .02f, 0, false, false);
        Require(Mathf.Abs(state.Angle - 5) < .01f, "Wheel angle safely crosses atan2 seam");
        CheckPhysics(); CheckRay();
        Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/hand-steering-checks.txt", results);
        Debug.Log("RACINGCAR_HAND_CHECKS_OK\n" + string.Join("\n", results));
    }
    static void CheckRay()
    {
        var go = new GameObject("Hand ray test", typeof(RectTransform));
        try
        {
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(200, 100);
            rect.localScale = Vector3.one * .002f;
            rect.SetPositionAndRotation(new Vector3(2, 3, 4), Quaternion.Euler(15, 65, 0));
            Require(QuestHandUI.HitRect(new Ray(rect.position - rect.forward, rect.forward), rect, out float distance)
                && Mathf.Abs(distance - 1) < .001f, "Hand ray hits scaled, rotated world-space menu button");
            Require(!QuestHandUI.HitRect(new Ray(rect.position - rect.forward + rect.right, rect.forward), rect, out _),
                "Ray outside button cannot click through menu bounds");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    static void CheckPhysics()
    {
        var previous = Physics.simulationMode;
        var originalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); Physics.simulationMode = SimulationMode.Script;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.position = new Vector3(0, -.5f, 0); ground.transform.localScale = new Vector3(1000, 1, 1000);
            var definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>("Assets/Vehicles/Porsche.asset");
            var car = UnityEngine.Object.Instantiate(definition.prefab); definition.Apply(car); car.ResetTo(new Vector3(0, .8f, 0), Quaternion.identity);
            Physics.SyncTransforms();
            for (int i = 0; i < 100; i++) { car.SetInput(0, 0, 1); car.StepPhysics(); Physics.Simulate(.02f); }
            var state = new HandWheelState(); Grab(state);
            for (int i = 0; i < 180; i++)
            {
                float angle = Mathf.Min(20, i * .2f);
                state.Step(Hand(180 - angle), Hand(-angle, true, 1), .02f, car.SpeedKph, false, false);
                car.SetInput(state.Throttle, state.Steering, state.Brake); car.StepPhysics(); Physics.Simulate(.02f);
            }
            float speed = car.SpeedKph;
            Require(speed > 10 && car.wheels[0].steerAngle > 0 && car.transform.eulerAngles.y > 1,
                "Gesture output drives real Porsche WheelColliders forward and right");
            for (int i = 0; i < 200; i++)
            {
                state.Step(Hand(160, true, 1), Hand(-20, true, 1), .02f, car.SpeedKph, false, false);
                car.SetInput(state.Throttle, state.Steering, state.Brake); car.StepPhysics(); Physics.Simulate(.02f);
            }
            Require(car.SpeedKph < speed * .2f && car.wheels[0].motorTorque == 0,
                $"Brake gesture stops actual car: {speed:F1} to {car.SpeedKph:F1} km/h");
        }
        finally
        {
            Physics.simulationMode = previous;
            if (!string.IsNullOrEmpty(originalScene)) EditorSceneManager.OpenScene(originalScene);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
    static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Hand check failed: " + message);
        results.Add("PASS: " + message);
    }
}
