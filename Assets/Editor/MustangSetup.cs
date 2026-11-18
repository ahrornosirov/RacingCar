using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

public static class MustangSetup
{
    const string Folder = "Assets/ThirdParty/Mustang/";
    [Serializable] class MaterialFile { public MaterialSpec[] materials; }
    [Serializable] class MaterialSpec { public string name, baseMap, normalMap; public float[] color; public float metallic, roughness; }
    static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>().First(t => t.name == name);
    static void Prepare()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Directory.CreateDirectory(Folder + "Materials");
        var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + "Mustang.fbx");
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
            if (spec.name == "GlassMtl" || spec.name == "GlassDarkMtl")
            {
                material.color = new Color(.65f,.78f,.85f, spec.name == "GlassDarkMtl" ? .12f : .055f);
                material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000; material.SetShaderPassEnabled("ShadowCaster", false);
            }
            if (spec.name == "GlassRedMtl")
            {
                material.color = new Color(.55f, .015f, .01f);
                material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(.15f, .005f, .002f));
            }
            EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
    }
    static Texture2D Texture(string name, bool normal)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string path = Folder + "Textures/" + name;
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal; importer.maxTextureSize = 1024;
        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 1024, format = TextureImporterFormat.ASTC_6x6 });
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static GameObject Model(Transform parent)
    {
        var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Mustang.fbx"), parent);
        model.name = "Mustang model";
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select(m => AssetDatabase.LoadAssetAtPath<Material>(Folder + "Materials/" + m.name + ".mat")).ToArray();
        }
        return model;
    }
    public static void ApplyAndCheck() { Apply(); RacingGaragePlayChecks.Run(); }

    public static void Inspect()
    {
        Prepare(); var model = Model(null);
        File.WriteAllLines("Logs/mustang-unity-inspection.txt", model.GetComponentsInChildren<Renderer>().Select(r => $"{r.name} position={r.transform.position} bounds={r.bounds}"));
        UnityEngine.Object.DestroyImmediate(model);
    }
    [MenuItem("RacingCar/Add Ford Mustang ROUSH")]
    public static void Apply()
    {
        Prepare();
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        var menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>();
        var original = menu.garage.vehicles[0];
        var car = UnityEngine.Object.Instantiate(original.prefab, Vector3.zero, Quaternion.identity);
        car.name = "Ford Mustang ROUSH 2019";
        var display = car.GetComponent<VehicleCockpit>().display;
        display.transform.SetParent(car.transform, true);
        foreach (string name in new[] { "Downloaded Concept Car", "Imported body batches", "Car Model Credit" })
        {
            var child = car.transform.Find(name); if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
        var model = Model(car.transform);
        // Unity imports the source with its nose toward +Z and driver on the left.
        model.transform.localPosition = new Vector3(0, -0.567f, -0.065f);
        model.transform.localScale = Vector3.one * 1.09f;
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
        var pivot = new GameObject("Animated Mustang steering").transform;
        pivot.SetParent(car.transform, false); pivot.position = steeringCenter; pivot.localRotation = Quaternion.Euler(15, 0, 0);
        steeringMesh.SetParent(pivot, true);
        var presentation = car.GetComponent<CarPresentation>();
        presentation.steeringWheel = pivot; presentation.rpmNeedle = null; presentation.brakeLights = new[] { Find(model.transform, "Body_GlassRedMtl").GetComponent<Renderer>() };
        var box = car.GetComponent<BoxCollider>(); box.center = new Vector3(0, 0.1f, 0); box.size = new Vector3(1.85f, 0.55f, 4.4f);
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
        panel.name = "Mustang live telemetry screen";
        UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
        panel.transform.SetParent(display.transform, false);
        panel.transform.localPosition = new Vector3(0, 0, .005f);
        panel.transform.localScale = new Vector3(.32f, .13f, 1);
        panel.GetComponent<Renderer>().sharedMaterial = panelMaterial;

        var definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>("Assets/Vehicles/Mustang.asset");
        if (definition == null) { definition = ScriptableObject.CreateInstance<VehicleDefinition>(); AssetDatabase.CreateAsset(definition, "Assets/Vehicles/Mustang.asset"); }
        definition.id = "mustang-roush-2019"; definition.displayName = "Ford Mustang ROUSH 2019";
        definition.description = "Rear-wheel drive / responsive steering";
        definition.accelerationLabel = "Strong acceleration"; definition.brakingLabel = "Sport brakes";
        definition.credit = "Mustang ROUSH: _the.render.box_ | CC BY 4.0\nHandling values are game tuning";
        definition.mass = 1720; definition.topSpeedKph = 250; definition.reverseSpeedKph = 28;
        definition.motorTorque = 1700; definition.brakeTorque = 4000;
        definition.torqueBySpeed = AnimationCurve.Linear(0, 1, 1, .65f);
        definition.steerAngle = 27; definition.steeringResponse = 3.7f;
        definition.drivetrain = VehicleDefinition.Drive.Rear;
        definition.grip = 1.1f; definition.frontBrakeBias = .62f; definition.downforce = 1.1f;
        definition.centerOfMass = new Vector3(0, -.35f, .12f);
        definition.suspensionSpring = 55000; definition.suspensionDamper = 6000;
        definition.cockpitOffset = wheelCenter + new Vector3(0, .30f, -.42f);
        definition.cockpitPitch = 5; definition.chaseOffset = new Vector3(0, 2.3f, -6.8f);
        definition.Apply(car);
        var prefab = PrefabUtility.SaveAsPrefabAsset(car.gameObject, "Assets/Prefabs/Mustang.prefab");
        definition.prefab = prefab.GetComponent<ArcadeCar>();
        EditorUtility.SetDirty(definition);
        var list = menu.garage.vehicles.Where(v => v.id != definition.id).ToList(); list.Add(definition); menu.garage.vehicles = list.ToArray();
        UnityEngine.Object.DestroyImmediate(car.gameObject);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene); EditorSceneManager.SaveScene(menu.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("RACINGCAR_MUSTANG_ADDED");
    }

}
