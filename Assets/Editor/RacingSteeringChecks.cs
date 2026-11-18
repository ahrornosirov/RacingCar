using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RacingSteeringChecks
{
    public static void RunAll()
    {
        Run();
        RacingVehicleChecks.Run();
    }

    public static void Run()
    {
        string originalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        var previousMode = Physics.simulationMode;
        float previousStep = Time.fixedDeltaTime;
        VehicleDefinition profile = null;
        ArcadeCar car = null;
        var results = new List<string>();
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Physics.simulationMode = SimulationMode.Script;
            Time.fixedDeltaTime = 0.02f;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(2000, 1, 2000);
            var source = AssetDatabase.LoadAssetAtPath<VehicleDefinition>("Assets/Vehicles/Porsche.asset");
            Require(source.assistedSteering, "Porsche defaults to assisted steering");
            profile = UnityEngine.Object.Instantiate(source);
            car = UnityEngine.Object.Instantiate(source.prefab);
            profile.Apply(car);

            Require(RaceDriverInput.FilterSteeringStick(0.1f) == 0 && RaceDriverInput.FilterSteeringStick(-0.1f) == 0,
                "Small thumbstick drift is ignored");
            Require(Mathf.Approximately(RaceDriverInput.FilterSteeringStick(1), 1)
                && Mathf.Approximately(RaceDriverInput.FilterSteeringStick(-1), -1), "Full stick remains available both ways");
            float small = RaceDriverInput.FilterSteeringStick(0.3f);
            Require(small > 0 && small < 0.15f && Mathf.Approximately(small, -RaceDriverInput.FilterSteeringStick(-0.3f)),
                "Small thumbstick corrections are gentle and symmetric");

            foreach (float speed in new[] { 0f, 20f, 60f, 100f, 160f, 300f })
            {
                car.ResetTo(new Vector3(0, 0.8f, 0), Quaternion.identity);
                car.SetInput(0, 1, 0);
                for (int i = 0; i < 100; i++)
                {
                    car.Body.linearVelocity = car.transform.forward * (speed / 3.6f);
                    Step(car);
                }
                float angle = car.wheels[0].steerAngle;
                if (speed <= 20) Require(angle > 27, "Low-speed turning lock is preserved");
                if (speed == 100) Require(angle > 3 && angle < 6, "High-speed lock is reduced to a usable range");
                if (speed >= 160) Require(angle > 0 && angle < 3, "Very high speeds retain gentle steering authority");
                Require(car.wheels[2].steerAngle == 0 && car.wheels[3].steerAngle == 0, "Rear wheels do not steer");
                results.Add($"Full steering at {speed:F0} km/h: {angle:F2} degrees");
            }

            // A real WheelCollider maneuver from an accelerated, settled car, not just curve assertions.
            var oldRight = Tap(car, profile, false, 1);
            var newRight = Tap(car, profile, true, 1);
            var newLeft = Tap(car, profile, true, -1);
            Require(newRight.yaw > 0.1f && newRight.yaw < oldRight.yaw * 0.7f,
                "A short key tap at 100 km/h produces less abrupt yaw without disabling steering");
            Require(newLeft.yaw < -0.1f && Mathf.Abs(newLeft.yaw + newRight.yaw) < 1,
                "Left and right lane corrections are symmetric");
            Require(newRight.speed > 70 && newRight.roll < 15, "Gentle correction preserves forward motion and upright body");
            results.Add($"100 km/h, 120 ms right key tap + 1.2s release: old yaw {oldRight.yaw:F2} deg, assisted {newRight.yaw:F2} deg; left {newLeft.yaw:F2} deg; assisted end speed {newRight.speed:F1} km/h, max roll {newRight.roll:F2} deg");

            Settle(car);
            car.SetInput(0.3f, 1, 0);
            for (int i = 0; i < 150; i++) Step(car);
            Require(Mathf.Abs(Mathf.DeltaAngle(0, car.transform.eulerAngles.y)) > 15, "Low-speed full input still turns the car");
            car.SetInput(0, -1, 0);
            Step(car);
            Require(car.wheels[0].steerAngle >= 0, "Direction change unwinds instead of snapping through center");
            for (int i = 0; i < 100; i++) Step(car);
            Require(car.wheels[0].steerAngle < 0, "Opposite steering becomes available after unwinding");
            car.ResetTo(new Vector3(0, 0.8f, 0), Quaternion.identity);
            Require(car.Steering == 0 && car.wheels[0].steerAngle == 0, "Reset clears visible and physical steering");
            results.Add("PASS: input dead zone/curve, speed limits, lane corrections, low-speed turning, direction changes, release and reset");
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/steering-checks.txt", results);
            Debug.Log("RACINGCAR_STEERING_CHECKS_OK\n" + string.Join("\n", results));
        }
        finally
        {
            if (car != null) UnityEngine.Object.DestroyImmediate(car.gameObject);
            if (profile != null) UnityEngine.Object.DestroyImmediate(profile);
            Physics.simulationMode = previousMode;
            Time.fixedDeltaTime = previousStep;
            if (!string.IsNullOrEmpty(originalScene)) EditorSceneManager.OpenScene(originalScene);
        }
    }

    static (float yaw, float speed, float roll) Tap(ArcadeCar car, VehicleDefinition profile, bool assisted, float direction)
    {
        profile.assistedSteering = assisted;
        Settle(car);
        car.SetInput(1, 0, 0);
        for (int i = 0; i < 500 && car.SpeedKph < 100; i++) Step(car);
        Require(car.SpeedKph >= 100, "Car reaches test speed under its own power");
        float initialYaw = car.transform.eulerAngles.y, maxRoll = 0;
        car.SetInput(0, direction, 0);
        for (int i = 0; i < 66; i++)
        {
            if (i == 6) car.SetInput(0, 0, 0);
            Step(car);
            maxRoll = Mathf.Max(maxRoll, Mathf.Abs(Mathf.DeltaAngle(0, car.transform.eulerAngles.z)));
        }
        Require(Mathf.Abs(car.wheels[0].steerAngle) < 0.01f, "Steering centers after release");
        return (Mathf.DeltaAngle(initialYaw, car.transform.eulerAngles.y), car.SpeedKph, maxRoll);
    }

    static void Settle(ArcadeCar car)
    {
        car.ResetTo(new Vector3(0, 0.8f, 0), Quaternion.identity);
        Physics.SyncTransforms();
        car.SetInput(0, 0, 1);
        for (int i = 0; i < 100; i++) Step(car);
    }

    static void Step(ArcadeCar car) { car.StepPhysics(); Physics.Simulate(Time.fixedDeltaTime); }
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Steering check failed: " + message);
    }
}
