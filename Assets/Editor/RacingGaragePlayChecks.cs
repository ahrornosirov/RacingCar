using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class RacingGaragePlayChecks
{
    const string Key = "RacingCar.Check.GarageRunning";
    static RaceMenu menu;
    static RaceGarage garage;
    static VehicleDefinition testVehicle;
    static VehicleDefinition[] shippedVehicles;
    static readonly List<string> results = new List<string>();
    static int stage, vehicleIndex;
    static double start, next;
    static string saved;
    static bool hadSaved, captured;
    static RacingGaragePlayChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
            { start = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        results.Add("PASS: " + message);
    }
    static void Advance() { stage++; next = EditorApplication.timeSinceStartup + 0.4; }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup - start > 35) throw new Exception("Garage timeout stage " + stage);
            if (stage == 0)
            {
                menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>();
                if (menu == null || !menu.IsOpen) return;
                garage = menu.garage;
                hadSaved = PlayerPrefs.HasKey(RaceGarage.SelectionKey); saved = PlayerPrefs.GetString(RaceGarage.SelectionKey); captured = true;
                Require(garage != null && menu.race.car.profile != null, "Home binds selected vehicle and handling profile");
                // A temporary second definition exercises swaps even before the next art download.
                shippedVehicles = garage.vehicles;
                testVehicle = UnityEngine.Object.Instantiate(garage.vehicles[0]);
                testVehicle.id = "garage-test-only"; testVehicle.displayName = "Test profile";
                testVehicle.mass = 1450; testVehicle.motorTorque = 500; testVehicle.topSpeedKph = 180;
                testVehicle.drivetrain = VehicleDefinition.Drive.Front;
                testVehicle.cockpitOffset += Vector3.up * 0.05f;
                garage.vehicles = new[] { garage.vehicles[0], testVehicle };
                garage.Preview(0); garage.Confirm();
                menu.garageButton.onClick.Invoke();
                Require(menu.CurrentPage == RaceMenu.Page.Garage && menu.race.Paused, "Garage opens and blocks driving");
                Canvas.ForceUpdateCanvases();
                RacingPreview.Export(menu.view.raceCamera, "Previews/garage.png");
                menu.nextCar.onClick.Invoke(); Advance();
            }
            else if (stage == 1)
            {
                Require(garage.PreviewIndex == 1 && menu.race.car.Body.mass == 1450, "Next car swaps the actual Rigidbody and applies distinct mass");
                Require(menu.view.car == menu.race.car && garage.dashboard.cockpitText.transform.IsChildOf(menu.race.car.transform), "Camera and cockpit display rebind to new car");
                Require(menu.race.recordKey.EndsWith(".garage-test-only"), "Records are isolated per vehicle");
                menu.SelectCamera(true); Require(!menu.view.showcase && menu.view.cockpit, "Garage cockpit preview");
                menu.SelectCamera(false); Require(menu.view.showcase && !menu.view.cockpit, "Garage exterior preview");
                menu.Back(); Advance();
            }
            else if (stage == 2)
            {
                Require(garage.PreviewIndex == 0 && !menu.race.recordKey.EndsWith(".garage-test-only"), "Back cancels unconfirmed car and restores record scope");
                menu.garageButton.onClick.Invoke(); menu.nextCar.onClick.Invoke();
                menu.play.onClick.Invoke(); Advance();
            }
            else if (stage == 3)
            {
                Require(menu.CurrentPage == RaceMenu.Page.Home && garage.SelectedIndex == 1 && PlayerPrefs.GetString(RaceGarage.SelectionKey) == testVehicle.id, "Use this car persists selection and returns Home");
                menu.race.countdownSeconds = 0; menu.StartRace();
                Require(!garage.Preview(0), "Vehicle switching is rejected during a race");
                menu.race.car.SetInput(1, 0, 0); menu.race.car.StepPhysics();
                Require(menu.race.car.wheels[0].motorTorque > 0 && menu.race.car.wheels[2].motorTorque == 0, "Front-wheel-drive profile delivers torque only to front axle");
                menu.race.car.SetInput(-1, 0, 0); menu.race.car.Body.linearVelocity = -menu.race.car.transform.forward * 12;
                menu.race.car.StepPhysics();
                Require(menu.race.car.wheels[0].motorTorque == 0 && menu.race.car.wheels[0].brakeTorque > 0, "Reverse speed limiter removes drive torque and applies braking");
                menu.ShowHome(); garage.Preview(0); garage.Confirm();
                menu.SelectCamera(true);
                garage.vehicles = shippedVehicles;
                vehicleIndex = 1;
                if (vehicleIndex < shippedVehicles.Length)
                {
                    menu.garageButton.onClick.Invoke(); garage.Preview(vehicleIndex); menu.SelectCamera(false); Advance();
                }
                else Finish(true);
            }
            else if (stage == 4)
            {
                var vehicle = garage.Current;
                var car = menu.race.car;
                Require(car.profile == vehicle && car.Body.mass == vehicle.mass, vehicle.displayName + ": actual prefab and physics profile loaded");
                Require(menu.view.car == car && garage.dashboard.cockpitText.transform.IsChildOf(car.transform), vehicle.displayName + ": camera and cockpit bindings updated");
                car.SetInput(1, 0, 0); car.StepPhysics();
                for (int wheel = 0; wheel < 4; wheel++)
                {
                    bool driven = vehicle.drivetrain == VehicleDefinition.Drive.All || (vehicle.drivetrain == VehicleDefinition.Drive.Front ? wheel < 2 : wheel >= 2);
                    Require(driven ? car.wheels[wheel].motorTorque > 0 : car.wheels[wheel].motorTorque == 0, vehicle.displayName + ": correct drive torque on wheel " + wheel);
                }
                RacingPreview.Export(menu.view.raceCamera, "Previews/" + PreviewName() + "-garage.png");
                menu.SelectCamera(true); Advance();
            }
            else if (stage == 5)
            {
                menu.canvas.gameObject.SetActive(false);
                RacingPreview.Export(menu.view.raceCamera, "Previews/" + PreviewName() + "-cockpit.png");
                menu.canvas.gameObject.SetActive(true);
                menu.play.onClick.Invoke();
                Require(PlayerPrefs.GetString(RaceGarage.SelectionKey) == garage.Current.id, garage.Current.displayName + ": selection persisted");
                menu.race.countdownSeconds = 0; menu.SelectCamera(false); menu.StartRace(); Advance();
            }
            else if (stage == 6)
            {
                Require(menu.HasStarted && !menu.IsOpen && !menu.view.showcase && !menu.view.cockpit && menu.race.CanDrive, garage.Current.displayName + ": third-person race started");
                var carTransform = menu.race.car.transform;
                var cameraOffset = menu.view.raceCamera.transform.position - carTransform.position;
                Require(Vector3.Dot(cameraOffset, carTransform.forward) < -3f, garage.Current.displayName + ": chase camera behind car");
                menu.SelectCamera(true); Advance();
            }
            else if (stage == 7)
            {
                Require(menu.view.cockpit && menu.race.recordKey.EndsWith("." + garage.Current.id), garage.Current.displayName + ": cockpit switch and per-car records");
                menu.ShowHome();
                if (++vehicleIndex < shippedVehicles.Length)
                {
                    menu.garageButton.onClick.Invoke(); garage.Preview(vehicleIndex); menu.SelectCamera(false);
                    stage = 3; Advance();
                }
                else Finish(true);
            }
        }
        catch (Exception e) { results.Add(e.ToString()); Finish(false); }
    }
    static string PreviewName() => garage.Current.id.Split('-')[0];

    static void Finish(bool ok)
    {
        if (captured)
        {
            if (hadSaved) PlayerPrefs.SetString(RaceGarage.SelectionKey, saved); else PlayerPrefs.DeleteKey(RaceGarage.SelectionKey);
            PlayerPrefs.Save();
        }
        File.WriteAllLines("Logs/garage-play-checks.txt", results);
        SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1); else EditorApplication.isPlaying = false;
    }
}
