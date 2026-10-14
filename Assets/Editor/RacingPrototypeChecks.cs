using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RacingPrototypeChecks
{
    [MenuItem("RacingCar/Run Prototype Physics Checks")]
    public static void Run()
    {
        var originalScene = SceneManager.GetActiveScene();
        if (originalScene.isDirty) throw new InvalidOperationException("Save your scene before running physics checks.");
        string originalPath = originalScene.path;
        var previousSimulation = Physics.simulationMode;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var physics = scene.GetPhysicsScene();
        Physics.simulationMode = SimulationMode.Script;
        var results = new List<string>();
        try
        {
            SceneManager.SetActiveScene(scene);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.2f, 0f);
            ground.transform.localScale = new Vector3(200f, 0.4f, 400f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RacingPrototypeBuilder.CarPath);
            Require(prefab != null, "Car prefab exists");
            var go = UnityEngine.Object.Instantiate(prefab);
            var car = go.GetComponent<ArcadeCar>();
            car.ResetTo(new Vector3(0f, 0.95f, 0f), Quaternion.identity);
            Physics.SyncTransforms();
            Step(car, physics, 100);
            Require(car.wheels[0].isGrounded && car.wheels[2].isGrounded, "Suspension settles on the road");
            car.SetInput(1f, 0f, 0f);
            Step(car, physics, 200);
            float movingSpeed = car.SpeedKph;
            Require(movingSpeed > 15f && go.transform.position.z > 5f, "Acceleration moves the car forward");
            results.Add($"Acceleration: {movingSpeed:0.0} km/h");
            car.SetInput(0f, 0f, 1f);
            Step(car, physics, 200);
            Require(car.SpeedKph < 2f, "Braking stops the car");
            car.ResetTo(new Vector3(0f, 0.95f, 0f), Quaternion.identity);
            Physics.SyncTransforms();
            Step(car, physics, 100);
            car.SetInput(1f, 0.4f, 0f);
            Step(car, physics, 150);
            Require(Mathf.Abs(Mathf.DeltaAngle(0f, go.transform.eulerAngles.y)) > 5f, "Steering changes heading");
            car.ResetTo(new Vector3(0f, 0.95f, 0f), Quaternion.identity);
            Require(car.Body.linearVelocity.sqrMagnitude < 0.01f && car.Body.angularVelocity.sqrMagnitude < 0.01f, "Reset clears motion");

            var race = new GameObject("Test Race").AddComponent<RaceSession>();
            race.car = car;
            race.startPosition = new Vector3(0f, 0.95f, 0f);
            car.Body.linearVelocity = Vector3.forward * 5f;
            race.PassCheckpoint(0, Vector3.forward);
            race.PassCheckpoint(3, Vector3.forward);
            Require(race.CompletedLaps == 0 && race.NextCheckpoint == 1, "Skipped gates cannot complete a lap");
            car.Body.linearVelocity = Vector3.back * 5f;
            race.PassCheckpoint(1, Vector3.forward);
            Require(race.NextCheckpoint == 1, "Reverse crossing is rejected");
            car.Body.linearVelocity = Vector3.forward * 5f;
            for (int i = 1; i < race.checkpointCount; i++) race.PassCheckpoint(i, Vector3.forward);
            race.PassCheckpoint(0, Vector3.forward);
            Require(race.CompletedLaps == 1 && race.NextCheckpoint == 1, "All ordered gates complete one lap");
            race.Restart();
            Require(race.NextCheckpoint == 1 && race.LapTime == 0f && car.Body.linearVelocity.sqrMagnitude < 0.01f, "Race reset restarts the current lap");
            race.TogglePause();
            Require(race.Paused && Time.timeScale == 0f, "Pause stops simulation");
            race.TogglePause();
            Require(!race.Paused && Time.timeScale == 1f, "Resume restores simulation");
            results.Add("PASS: suspension, acceleration, braking, steering, reset, gate order/direction, lap completion, pause/resume");
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/prototype-checks.txt", results);
            Debug.Log("RACINGCAR_PROTOTYPE_CHECKS_OK\n" + string.Join("\n", results));
        }
        finally
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Physics.simulationMode = previousSimulation;
            if (!string.IsNullOrEmpty(originalPath)) EditorSceneManager.OpenScene(originalPath);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    static void Step(ArcadeCar car, PhysicsScene physics, int count)
    {
        for (int i = 0; i < count; i++)
        {
            car.StepPhysics();
            physics.Simulate(0.02f);
        }
    }

    static void Require(bool passed, string message)
    {
        if (!passed) throw new InvalidOperationException("Prototype check failed: " + message);
    }
}
