using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class QuestHandSetup
{
    [MenuItem("RacingCar/Install Hand Steering Prototype")]
    public static void Apply()
    {
        Directory.CreateDirectory("Backups/BeforeHandSteering");
        string backup = "Backups/BeforeHandSteering/RacingPrototype.unity";
        if (!File.Exists(backup)) File.Copy(RacingPrototypeBuilder.ScenePath, backup);
        QuestProjectSetup.Configure();
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        var menu = Object.FindAnyObjectByType<RaceMenu>();
        var hands = menu.GetComponent<QuestHandDriving>();
        if (hands == null) hands = menu.gameObject.AddComponent<QuestHandDriving>();
        hands.race = menu.race; hands.view = menu.view; hands.menu = menu;
        Directory.CreateDirectory("Assets/Art/Hands"); AssetDatabase.Refresh();
        const string path = "Assets/Art/Hands/TrackedHands.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color(.2f, .85f, 1f));
            AssetDatabase.CreateAsset(material, path);
        }
        hands.handMaterial = material;
        const string modelPath = "Assets/Samples/XR Hands/1.7.3/HandVisualizer/Prefabs/";
        hands.leftHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath + "Left Hand Tracking.prefab");
        hands.rightHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath + "Right Hand Tracking.prefab");
        menu.hands = hands; menu.race.GetComponent<RaceDriverInput>().hands = hands;
        if (menu.handModeButton == null)
        {
            var button = Object.Instantiate(menu.controls, menu.card);
            button.name = "Hand steering input mode";
            button.onClick = new Button.ButtonClickedEvent();
            button.GetComponentInChildren<TMP_Text>().text = "INPUT: CONTROLLERS / SWITCH TO HANDS";
            button.gameObject.SetActive(false); menu.handModeButton = button;
        }
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        EditorSceneManager.SaveScene(menu.gameObject.scene); AssetDatabase.SaveAssets();
        Debug.Log("RACINGCAR_HAND_STEERING_INSTALLED");
    }
}
