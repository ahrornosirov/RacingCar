using System;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class QuestHandsBaselineSetup
{
    public const string ScenePath = "Assets/Scenes/QuestHandsBaseline.unity";
    public static void ImportSamples()
    {
        Import("com.unity.xr.interaction.toolkit", "3.6.1", "Starter Assets");
        Import("com.unity.xr.hands", "1.7.3", "HandVisualizer");
        Import("com.unity.xr.interaction.toolkit", "3.6.1", "Hands Interaction Demo");
        Debug.Log("QUEST_HANDS_BASELINE_SAMPLES_IMPORTED");
    }
    public static void CreateScene()
    {
        EditorSceneManager.OpenScene("Assets/Samples/XR Interaction Toolkit/3.6.1/Hands Interaction Demo/HandsDemoScene.unity");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            bool keep = root.GetComponentInChildren<XROrigin>(true) != null
                || root.GetComponentInChildren<EventSystem>(true) != null
                || root.name == "XR Interaction Manager" || root.name == "Input Action Manager"
                || root.GetComponent<Light>() != null;
            if (!keep) UnityEngine.Object.DestroyImmediate(root);
        }
        var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
        if (origin == null) throw new Exception("Official XR Origin is missing");
        origin.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Test room floor";
        floor.transform.position = new Vector3(0, -.1f, 0); floor.transform.localScale = new Vector3(20, .2f, 20);
        Directory.CreateDirectory("Assets/Art/HandsBaseline"); AssetDatabase.Refresh();
        const string matPath = "Assets/Art/HandsBaseline/Floor.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.SetColor("_BaseColor", new Color(.13f, .18f, .22f)); AssetDatabase.CreateAsset(material, matPath); }
        floor.GetComponent<Renderer>().sharedMaterial = material;
        var go = new GameObject("Hands Check Panel", typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
        var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = origin.Camera;
        var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(600, 460); rect.localScale = Vector3.one * .0018f;
        rect.SetPositionAndRotation(new Vector3(0, 1.4f, 1.25f), Quaternion.identity);
        go.AddComponent<Image>().color = new Color(.02f, .035f, .05f, 1);
        var check = go.AddComponent<QuestHandsBaseline>(); check.panel = rect; check.headsetCamera = origin.Camera;
        Label(rect, "RACINGCAR HANDS CHECK / 01", 28, new Vector2(560, 45), new Vector2(0, 190));
        Label(rect, "Put controllers down and raise both hands.\nPoint at a button, then pinch thumb + index.", 21, new Vector2(560, 65), new Vector2(0, 125));
        check.status = Label(rect, "Waiting for hand provider...", 24, new Vector2(560, 65), new Vector2(0, 48));
        check.firstButton = Button(rect, "BUTTON A", -32);
        check.secondButton = Button(rect, "BUTTON B", -112);
        check.result = Label(rect, "", 21, new Vector2(560, 60), new Vector2(0, -190));
        EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
        ValidateScene();
        Debug.Log("QUEST_HANDS_BASELINE_SCENE_READY");
    }
    static TMP_Text Label(Transform parent, string value, float size, Vector2 bounds, Vector2 position)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = bounds; rect.anchoredPosition = position;
        var label = go.GetComponent<TextMeshProUGUI>(); label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.text = value; label.fontSize = size; label.alignment = TextAlignmentOptions.Center; label.color = Color.white; label.raycastTarget = false;
        return label;
    }
    static Button Button(Transform parent, string name, float y)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(500, 65); rect.anchoredPosition = new Vector2(0, y);
        var image = go.GetComponent<Image>(); image.color = new Color(.08f, .32f, .4f);
        var button = go.GetComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = Color.cyan; colors.selectedColor = Color.cyan; colors.pressedColor = new Color(1, .6f, .15f); button.colors = colors;
        Label(go.transform, name, 27, new Vector2(480, 60), Vector2.zero);
        return button;
    }
    public static void ValidateScene()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) > 0) throw new Exception("Missing sample script: " + node.name);
        if (UnityEngine.Object.FindAnyObjectByType<XRUIInputModule>() == null) throw new Exception("Official XR UI module missing");
        if (UnityEngine.Object.FindAnyObjectByType<QuestHandDriving>() != null || UnityEngine.Object.FindAnyObjectByType<RaceSession>() != null) throw new Exception("Racing input leaked into isolated test");
        int visualizers = roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).Count(b => b != null && b.GetType().Name == "HandVisualizer");
        if (visualizers < 1) throw new Exception("Official hand visualizer missing");
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/hands-baseline-validation.txt", "PASS: official hand visualizer, XR UI input, no missing scene scripts, no racing input.\nHardware acceptance pending: both hands visible and each button selectable five times.\n");
    }
    public static void Build()
    {
        EditorSceneManager.OpenScene(ScenePath); ValidateScene(); QuestProjectSetup.Validate();
        string company = PlayerSettings.companyName, product = PlayerSettings.productName;
        string identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        try
        {
            PlayerSettings.productName = "RacingCar Hands Check";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ahrornosirov.racingcar.handscheck");
            Directory.CreateDirectory("Builds/Quest");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "Builds/Quest/RacingCar-Hands-Check.apk", target = BuildTarget.Android, options = BuildOptions.Development });
            File.WriteAllText("Logs/hands-baseline-build-summary.txt", $"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nDuration: {report.summary.totalTime}\n");
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Hands baseline APK build failed");
            Debug.Log("QUEST_HANDS_BASELINE_APK_OK");
        }
        finally { PlayerSettings.companyName = company; PlayerSettings.productName = product; PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, identifier); AssetDatabase.SaveAssets(); }
    }
    static void Import(string package, string version, string name)
    {
        var sample = Sample.FindByPackage(package, version).FirstOrDefault(s => s.displayName == name);
        if (string.IsNullOrEmpty(sample.displayName)) throw new Exception("Missing official sample: " + name);
        if (!sample.isImported && !sample.Import(Sample.ImportOptions.OverridePreviousImports))
            throw new Exception("Sample import failed: " + name);
    }
}
