using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

public static class PorscheSetup
{
    const string Folder = "Assets/ThirdParty/Porsche/";
    [Serializable] class MaterialFile { public MaterialSpec[] materials; }
    [Serializable] class MaterialSpec { public string name, baseMap, normalMap, metalRoughMap, emissionMap, alphaMode; public float[] color, emission; public float metallic, roughness; }
    static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>().First(t => t.name == name);
    static void Prepare()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Directory.CreateDirectory(Folder + "Materials");
        var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + "Porsche.fbx");
        importer.importAnimation = false; importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        foreach (var spec in JsonUtility.FromJson<MaterialFile>(File.ReadAllText(Folder + "UnityMaterials.json")).materials)
        {
            string path = Folder + "Materials/" + spec.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.color = new Color(spec.color[0], spec.color[1], spec.color[2], spec.color[3]).gamma;
            material.SetFloat("_Metallic", spec.metallic); material.SetFloat("_Smoothness", 1f - spec.roughness);
            material.SetFloat("_Cull", 0);
            material.SetTexture("_BaseMap", Texture(spec.baseMap, false));
            if (!string.IsNullOrEmpty(spec.normalMap)) { material.SetTexture("_BumpMap", Texture(spec.normalMap, true)); material.EnableKeyword("_NORMALMAP"); material.SetFloat("_BumpScale", 0.5f); }
            if (spec.name == "invisible_dark") material.color = new Color(.025f, .025f, .025f);
            if (spec.name == "windows") material.color = new Color(.65f,.78f,.85f, .045f);
            if (spec.alphaMode == "BLEND" && spec.name != "windows_dots")
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000; material.SetShaderPassEnabled("ShadowCaster", false);
            }
            if (spec.name == "red_light_main")
            {
                material.color = new Color(.55f, .015f, .01f);
                material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(.15f, .005f, .002f));
            }
            if (!string.IsNullOrEmpty(spec.metalRoughMap))
            {
                var map = Texture(spec.metalRoughMap, false, true, true);
                var pixels = map.GetPixels();
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(pixels[i].b * spec.metallic, 0, 0, 1f - pixels[i].g * spec.roughness);
                var packed = new Texture2D(map.width, map.height, TextureFormat.RGBA32, false, true);
                packed.SetPixels(pixels); packed.Apply();
                string packedName = spec.name + "_MetalSmooth.png";
                File.WriteAllBytes(Folder + "Textures/" + packedName, packed.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(packed);
                AssetDatabase.ImportAsset(Folder + "Textures/" + packedName, ImportAssetOptions.ForceSynchronousImport);
                material.SetTexture("_MetallicGlossMap", Texture(packedName, false, true));
                material.SetFloat("_Metallic", 1); material.SetFloat("_Smoothness", 1); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            if (spec.emission.Any(v => v > 0))
            {
                material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(spec.emission[0], spec.emission[1], spec.emission[2]));
                material.SetTexture("_EmissionMap", Texture(spec.emissionMap, false));
            }
            if (spec.name == "windows_dots")
            {
                material.SetFloat("_AlphaClip", 1); material.SetFloat("_Cutoff", .5f); material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue = 2450;
            }
            EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
    }
    static Texture2D Texture(string name, bool normal, bool linear = false, bool readable = false)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string path = Folder + "Textures/" + name;
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal && !linear; importer.isReadable = readable; importer.maxTextureSize = 1024;
        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 1024, format = TextureImporterFormat.ASTC_6x6 });
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static GameObject Model(Transform parent)
    {
        var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Porsche.fbx"), parent);
        model.name = "Porsche model";
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select(m => AssetDatabase.LoadAssetAtPath<Material>(Folder + "Materials/" + m.name + ".mat")).ToArray();
        }
        return model;
    }
    public static void BuildOptimizedAndroid()
    {
        // Runtime shaders do not need CPU copies of the texture pixels.
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder.TrimEnd('/') }))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (!importer.isReadable) continue;
            importer.isReadable = false; importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        RacingBuild.BuildAndroid();
    }

    public static void ApplyAndCheck() { Apply(); RacingGaragePlayChecks.Run(); }

    public static void Inspect()
    {
        Prepare(); var model = Model(null);
        File.WriteAllLines("Logs/porsche-unity-inspection.txt", model.GetComponentsInChildren<Renderer>().Select(r => $"{r.name} position={r.transform.position} bounds={r.bounds}"));
        UnityEngine.Object.DestroyImmediate(model);
    }
    [MenuItem("RacingCar/Add Ford Porsche ROUSH")]
    public static void Apply()
    {
        Prepare();
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        var menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>();
        var original = menu.garage.vehicles[0];
        var car = UnityEngine.Object.Instantiate(original.prefab, Vector3.zero, Quaternion.identity);
        car.name = "Porsche 911 Turbo S";
        var display = car.GetComponent<VehicleCockpit>().display;
        display.transform.SetParent(car.transform, true);
        foreach (string name in new[] { "Downloaded Concept Car", "Imported body batches", "Car Model Credit" })
        {
            var child = car.transform.Find(name); if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
        var model = Model(car.transform);
        // Unity imports the source with its nose toward +Z and driver on the left.
        model.transform.localPosition = new Vector3(0, -0.573f, 0);
        model.transform.localScale = Vector3.one;
        var names = new[] { "WheelFL", "WheelFR", "WheelRL", "WheelRR" };
        for (int i = 0; i < 4; i++)
        {
            var wheel = Find(model.transform, names[i]);
            Vector3 center = car.transform.InverseTransformPoint(wheel.position);
            var visual = car.wheelVisuals[i];
            for (int c = visual.childCount - 1; c >= 0; c--) UnityEngine.Object.DestroyImmediate(visual.GetChild(c).gameObject);
            visual.localPosition = center; visual.localRotation = Quaternion.identity;
            wheel.SetParent(visual, true); wheel.localPosition = Vector3.zero;
            car.wheels[i].transform.localPosition = center; car.wheels[i].radius = 0.354f;
        }
        var steeringMesh = Find(model.transform, "SteeringWheel");
        Vector3 steeringCenter = steeringMesh.GetComponent<Renderer>().bounds.center;
        var pivot = new GameObject("Animated Porsche steering").transform;
        pivot.SetParent(car.transform, false); pivot.position = steeringCenter; pivot.localRotation = Quaternion.Euler(15, 0, 0);
        steeringMesh.SetParent(pivot, true);
        var presentation = car.GetComponent<CarPresentation>();
        presentation.steeringWheel = pivot; presentation.rpmNeedle = null; presentation.brakeLights = new[] { Find(model.transform, "Body_red_light_main").GetComponent<Renderer>() };
        var box = car.GetComponent<BoxCollider>(); box.center = new Vector3(0, 0.1f, 0); box.size = new Vector3(1.8f, 0.55f, 4.2f);
        Vector3 wheelCenter = car.transform.InverseTransformPoint(steeringCenter);
        display.transform.localPosition = wheelCenter + new Vector3(0.22f, 0.26f, 0.20f);
        display.transform.localRotation = Quaternion.identity;
        display.rectTransform.sizeDelta = new Vector2(.30f, .10f);
        display.fontSizeMin = .15f; display.fontSizeMax = .5f;
        var panelMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Materials/TelemetryPanel.mat");
        if (panelMaterial == null)
        {
            panelMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            panelMaterial.color = new Color(.015f, .02f, .025f);
            AssetDatabase.CreateAsset(panelMaterial, Folder + "Materials/TelemetryPanel.mat");
        }
        var panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        panel.name = "Porsche live telemetry screen";
        UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
        panel.transform.SetParent(display.transform, false);
        panel.transform.localPosition = new Vector3(0, 0, .005f);
        panel.transform.localScale = new Vector3(.32f, .13f, 1);
        panel.GetComponent<Renderer>().sharedMaterial = panelMaterial;

        var definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>("Assets/Vehicles/Porsche.asset");
        if (definition == null) { definition = ScriptableObject.CreateInstance<VehicleDefinition>(); AssetDatabase.CreateAsset(definition, "Assets/Vehicles/Porsche.asset"); }
        definition.id = "porsche-911-turbo-s"; definition.displayName = "Porsche 911 Turbo S";
        definition.description = "All-wheel drive / high grip";
        definition.accelerationLabel = "Fast acceleration"; definition.brakingLabel = "High-grip brakes";
        definition.credit = "Porsche 911: n.brizitskaya | CC BY 4.0\nHandling values are game tuning";
        definition.mass = 1600; definition.topSpeedKph = 300; definition.reverseSpeedKph = 30;
        definition.motorTorque = 2000; definition.brakeTorque = 5200;
        definition.torqueBySpeed = AnimationCurve.Linear(0, 1, 1, .75f);
        definition.steerAngle = 28; definition.steeringResponse = 4.8f;
        definition.assistedSteering = true;
        definition.drivetrain = VehicleDefinition.Drive.All;
        definition.grip = 1.35f; definition.frontBrakeBias = .62f; definition.downforce = 1.4f;
        definition.centerOfMass = new Vector3(0, -.32f, -.08f);
        definition.suspensionSpring = 60000; definition.suspensionDamper = 6500;
        definition.cockpitOffset = wheelCenter + new Vector3(0, .32f, -.42f);
        definition.cockpitPitch = 5; definition.chaseOffset = new Vector3(0, 2.2f, -6.4f);
        definition.Apply(car);
        var prefab = PrefabUtility.SaveAsPrefabAsset(car.gameObject, "Assets/Prefabs/Porsche.prefab");
        definition.prefab = prefab.GetComponent<ArcadeCar>();
        EditorUtility.SetDirty(definition);
        var list = menu.garage.vehicles.Where(v => v.id != definition.id).ToList(); list.Add(definition); menu.garage.vehicles = list.ToArray();
        UnityEngine.Object.DestroyImmediate(car.gameObject);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene); EditorSceneManager.SaveScene(menu.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("RACINGCAR_PORSCHE_ADDED");
    }

}
