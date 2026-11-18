using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RacingVehicleChecks
{
    public static void Run()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        var definitions = UnityEngine.Object.FindAnyObjectByType<RaceGarage>().vehicles;
        var definitionPaths = definitions.Select(AssetDatabase.GetAssetPath).ToArray();
        var prefabPaths = definitions.Select(d => AssetDatabase.GetAssetPath(d.prefab)).ToArray();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(10000, 1, 10000);
        var results = new List<string>();
        var mode = Physics.simulationMode;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            for (int index = 0; index < definitions.Length; index++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(definitionPaths[index]);
                var car = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[index]).GetComponent<ArcadeCar>(), new Vector3(0, 0.8f, 0), Quaternion.identity);
                definition.Apply(car);
                Physics.SyncTransforms();
                car.SetInput(0, 0, 1);
                for (int i = 0; i < 100; i++) Step(car);
                float fiveSeconds = 0, zeroTo100 = 0;
                car.SetInput(1, 0, 0);
                for (int i = 0; i < 1750; i++)
                {
                    Step(car);
                    if (i == 249) fiveSeconds = car.SpeedKph;
                    if (zeroTo100 == 0 && car.SpeedKph >= 100) zeroTo100 = (i + 1) * 0.02f;
                }
                float top = car.SpeedKph;
                if (fiveSeconds < 10 || top > definition.topSpeedKph + 5) throw new Exception(definition.name + ": acceleration/top-speed check failed: " + top);
                var brakingStart = car.Body.position;
                car.SetInput(0, 0, 1);
                float stopTime = 0;
                for (int i = 0; i < 750; i++) { Step(car); if (car.SpeedKph < 1) { stopTime = (i + 1) * 0.02f; break; } }
                if (stopTime == 0) throw new Exception($"{definition.name}: brakes failed to stop car; start {brakingStart}, end {car.Body.position}, speed {car.SpeedKph:F1}");
                float stoppingDistance = Vector3.Distance(brakingStart, car.Body.position);
                car.ResetTo(new Vector3(0, 0.8f, 0), Quaternion.identity);
                Physics.SyncTransforms(); car.SetInput(-1, 0, 0);
                for (int i = 0; i < 750; i++) Step(car);
                if (car.SpeedKph < 5 || car.SpeedKph > definition.reverseSpeedKph + 5) throw new Exception(definition.name + ": reverse check failed: " + car.SpeedKph);
                results.Add($"PASS {definition.displayName}: speed at 5s {fiveSeconds:F1} km/h; 0-100 {zeroTo100:F2}s; speed at 35s {top:F1} km/h; stop {stopTime:F2}s / {stoppingDistance:F1}m; reverse {car.SpeedKph:F1} km/h");
                UnityEngine.Object.DestroyImmediate(car.gameObject);
            }
            File.WriteAllLines("Logs/vehicle-physics-checks.txt", results);
            Debug.Log("RACINGCAR_VEHICLE_PHYSICS_OK");
        }
        finally { Physics.simulationMode = mode; }
    }
    static void Step(ArcadeCar car) { car.StepPhysics(); Physics.Simulate(0.02f); }
}
