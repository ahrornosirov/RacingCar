using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RacingGarageSetup
{
    [MenuItem("RacingCar/Install Garage")]
    public static void Apply()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        var menu = Object.FindAnyObjectByType<RaceMenu>();
        var dashboard = menu.race.GetComponent<RaceDashboard>();
        var mount = menu.race.car.GetComponent<VehicleCockpit>();
        if (mount == null) mount = menu.race.car.gameObject.AddComponent<VehicleCockpit>();
        mount.display = dashboard.cockpitText;
        PrefabUtility.SaveAsPrefabAsset(menu.race.car.gameObject, RacingPrototypeBuilder.CarPath);
        Directory.CreateDirectory("Assets/Vehicles");
        var definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>("Assets/Vehicles/Concept.asset");
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<VehicleDefinition>();
            definition.id = "concept"; definition.displayName = "Concept Roadster";
            definition.description = "Lightweight rear-wheel drive";
            definition.credit = "Car Concept: Eric Chadwick / DGG | CC BY 4.0\nHandling values are game tuning";
            definition.mass = 900f; definition.motorTorque = 900f; definition.topSpeedKph = 136.8f;
            definition.brakeTorque = 2800f; definition.frontBrakeBias = 0.5f;
            definition.torqueBySpeed = AnimationCurve.Linear(0, 1, 1, 1);
            definition.cockpitOffset = menu.view.cockpitOffset;
            definition.chaseOffset = menu.view.chaseOffset;
            definition.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RacingPrototypeBuilder.CarPath).GetComponent<ArcadeCar>();
            AssetDatabase.CreateAsset(definition, "Assets/Vehicles/Concept.asset");
        }
        var garage = menu.GetComponent<RaceGarage>();
        if (garage == null) garage = menu.gameObject.AddComponent<RaceGarage>();
        garage.race = menu.race; garage.view = menu.view; garage.dashboard = dashboard;
        if (garage.vehicles == null || garage.vehicles.Length == 0) garage.vehicles = new[] { definition };
        menu.garage = garage;
        if (menu.garageButton == null) menu.garageButton = CloneButton(menu, "Garage");
        if (menu.previousCar == null) menu.previousCar = CloneButton(menu, "Previous car");
        if (menu.nextCar == null) menu.nextCar = CloneButton(menu, "Next car");
        menu.summary.fontSize = 20;
        menu.summary.rectTransform.sizeDelta = new Vector2(540, 110);
        menu.subtitle.enableAutoSizing = true; menu.subtitle.fontSizeMin = 15;
        EditorUtility.SetDirty(definition);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        EditorSceneManager.SaveScene(menu.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("RACINGCAR_GARAGE_INSTALLED");
    }

    static Button CloneButton(RaceMenu menu, string name)
    {
        var button = Object.Instantiate(menu.controls, menu.card);
        button.name = name; button.onClick = new Button.ButtonClickedEvent();
        button.GetComponentInChildren<TMP_Text>().text = name.ToUpper();
        button.gameObject.SetActive(false);
        return button;
    }
}
