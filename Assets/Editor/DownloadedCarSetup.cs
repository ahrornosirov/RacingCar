using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class DownloadedCarSetup
{
    const string Folder = "Assets/ThirdParty/CarConcept/";
    [Serializable] class MaterialFile { public MaterialSpec[] materials; }
    [Serializable] class MaterialSpec
    {
        public string name, baseMap, normalMap, metalRoughMap, emissionMap;
        public float[] color, emission, tiling, offset;
        public float metallic, roughness, normalStrength;
        public bool doubleSided;
    }
    static Dictionary<string, Material> materials;

    static void Prepare()
    {
        Directory.CreateDirectory(Folder + "Materials");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + "CarConcept.fbx");
        importer.importAnimation = false;
        importer.addCollider = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        var file = JsonUtility.FromJson<MaterialFile>(File.ReadAllText(Folder + "UnityMaterials.json"));
        materials = new Dictionary<string, Material>();
        foreach (var spec in file.materials)
        {
            string path = Folder + "Materials/" + spec.name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = spec.name, enableInstancing = true };
                AssetDatabase.CreateAsset(material, path);
            }
            var color = new Color(spec.color[0], spec.color[1], spec.color[2], spec.color[3]).gamma;
            material.color = color;
            material.SetFloat("_Metallic", spec.metallic);
            material.SetFloat("_Smoothness", 1f - spec.roughness);
            material.SetFloat("_Cull", spec.doubleSided ? (float)CullMode.Off : (float)CullMode.Back);
            if (!string.IsNullOrEmpty(spec.baseMap) && spec.name != "License") material.SetTexture("_BaseMap", Texture(spec.baseMap, false));
            material.SetTextureScale("_BaseMap", new Vector2(spec.tiling[0], spec.tiling[1]));
            material.SetTextureOffset("_BaseMap", new Vector2(spec.offset[0], -spec.offset[1]));
            if (!string.IsNullOrEmpty(spec.normalMap))
            {
                material.SetTexture("_BumpMap", Texture(spec.normalMap, true));
                material.SetFloat("_BumpScale", spec.normalStrength);
                material.EnableKeyword("_NORMALMAP");
            }
            if (!string.IsNullOrEmpty(spec.metalRoughMap))
            {
                var source = Texture(spec.metalRoughMap, false, true, false);
                var pixels = source.GetPixels();
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(pixels[i].b, 0f, 0f, 1f - pixels[i].g * spec.roughness);
                var packed = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
                packed.SetPixels(pixels); packed.Apply();
                string packedPath = Folder + "Textures/" + spec.name + "_MetalSmooth.png";
                File.WriteAllBytes(packedPath, packed.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(packed);
                AssetDatabase.ImportAsset(packedPath, ImportAssetOptions.ForceSynchronousImport);
                material.SetTexture("_MetallicGlossMap", Texture(spec.name + "_MetalSmooth.png", false, false, false));
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Smoothness", 1f);
            }
            if (spec.emission.Any(x => x > 0f) && spec.emissionMap != "Image3.png")
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(spec.emission[0], spec.emission[1], spec.emission[2]));
                if (!string.IsNullOrEmpty(spec.emissionMap)) material.SetTexture("_EmissionMap", Texture(spec.emissionMap, false));
            }
            if (spec.name == "Glass")
            {
                material.color = new Color(0.65f, 0.78f, 0.85f, 0.035f);
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f); material.SetFloat("_Cull", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = (int)RenderQueue.Transparent;
                material.SetShaderPassEnabled("ShadowCaster", false);
                material.SetFloat("_EnvironmentReflections", 0f);
                material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            }
            if (spec.name == "Dashboard" || spec.name == "Material")
            {
                // Runtime instruments replace the source asset's static zero-speed graphic.
                material.DisableKeyword("_EMISSION"); material.SetTexture("_EmissionMap", null);
                material.SetColor("_EmissionColor", Color.black);
            }
            materials[spec.name] = material;
            EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
        materials["Material_2"] = materials["Material"];
    }

    static Texture2D Texture(string name, bool normal, bool readable = false, bool srgb = true)
    {
        string path = Folder + "Textures/" + name;
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal && srgb; importer.isReadable = readable;
        importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 4; importer.maxTextureSize = 1024;
        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 1024, format = TextureImporterFormat.ASTC_6x6 });
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    public static void Inspect()
    {
        Prepare();
        var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "CarConcept.fbx"));
        model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        ApplyMaterials(model);
        var lines = new List<string>();
        foreach (var t in model.GetComponentsInChildren<Transform>())
        {
            var renderer = t.GetComponent<Renderer>();
            lines.Add($"{t.name} position={t.position} rotation={t.eulerAngles} scale={t.lossyScale}" + (renderer == null ? "" : $" bounds={renderer.bounds} mats={string.Join(",", renderer.sharedMaterials.Select(m => m.name))}"));
        }
        File.WriteAllLines("Logs/downloaded-car-inspection.txt", lines);
        UnityEngine.Object.DestroyImmediate(model);
    }

    static void ApplyMaterials(GameObject model)
    {
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            var assigned = renderer.sharedMaterials;
            for (int i = 0; i < assigned.Length; i++)
            {
                string name = assigned[i].name;
                if (!materials.TryGetValue(name, out var material)) throw new InvalidOperationException("Unmapped imported material: " + name);
                assigned[i] = material;
            }
            renderer.sharedMaterials = assigned;
            if (assigned.Any(m => m.name == "Glass")) renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    static Transform Find(Transform model, string name) => model.GetComponentsInChildren<Transform>().First(t => t.name == name);

    [MenuItem("RacingCar/Use Downloaded Concept Car")]
    public static void Apply()
    {
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the scene before replacing the car.");
        Prepare();
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        var car = UnityEngine.Object.FindAnyObjectByType<ArcadeCar>();
        if (car.transform.Find("Downloaded Concept Car") != null) throw new InvalidOperationException("Downloaded car already installed.");
        Directory.CreateDirectory("Backups/BeforeDownloadedCar");
        foreach (string path in new[] { RacingPrototypeBuilder.ScenePath, RacingPrototypeBuilder.CarPath })
        {
            string destination = "Backups/BeforeDownloadedCar/" + Path.GetFileName(path);
            if (!File.Exists(destination)) File.Copy(path, destination);
        }
        var presentation = car.GetComponent<CarPresentation>();
        UnityEngine.Object.DestroyImmediate(car.transform.Find("Roadster bodywork").gameObject);
        var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "CarConcept.fbx"), car.transform);
        model.name = "Downloaded Concept Car";
        model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity; model.transform.localScale = Vector3.one;
        ApplyMaterials(model);
        Vector3 roof = car.transform.InverseTransformPoint(Find(model.transform, "BodyRoofPanel").GetComponent<Renderer>().bounds.center);
        Vector3 axleCenter = car.transform.InverseTransformPoint(Find(model.transform, "Axles").GetComponent<Renderer>().bounds.center);
        if (roof.y < axleCenter.y) model.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
        // Orient the imported hood toward the controller's +Z forward direction.
        Vector3 hood = car.transform.InverseTransformPoint(Find(model.transform, "BodyHood").GetComponent<Renderer>().bounds.center);
        if (hood.z < 0f) model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.localRotation;
        var wheelNodes = new[] { "WheelFrontL", "WheelFrontR", "WheelRearL", "WheelRearR" }.Select(name => Find(model.transform, name)).ToArray();
        float frontZ = (wheelNodes[0].localToWorldMatrix.GetColumn(3).z + wheelNodes[1].position.z) * 0.5f;
        float rearZ = (wheelNodes[2].position.z + wheelNodes[3].position.z) * 0.5f;
        model.transform.localScale = Vector3.one * (2.4f / Mathf.Abs(frontZ - rearZ));
        Vector3 mean = Vector3.zero;
        foreach (var wheel in wheelNodes) mean += car.transform.InverseTransformPoint(wheel.position) * 0.25f;
        model.transform.localPosition = new Vector3(-mean.x, -0.22f - mean.y, -mean.z);
        for (int i = 0; i < 4; i++)
        {
            // Match left/right by actual position rather than exporter handedness.
            int axle = i < 2 ? 0 : 2;
            var wheel = wheelNodes.Skip(axle).Take(2).First(w => (car.transform.InverseTransformPoint(w.position).x < 0f) == (i % 2 == 0));
            Vector3 center = car.transform.InverseTransformPoint(wheel.position);
            car.wheels[i].transform.localPosition = center;
            var visual = car.wheelVisuals[i];
            for (int child = visual.childCount - 1; child >= 0; child--) UnityEngine.Object.DestroyImmediate(visual.GetChild(child).gameObject);
            visual.localPosition = center; visual.localRotation = Quaternion.identity;
            wheel.SetParent(visual, true); wheel.localPosition = Vector3.zero;
        }
        // Remove source-brand plates/badge from the gameplay variant; original files remain intact.
        foreach (string name in new[] { "License Plate", "InteriorSteeringEmblem" }) UnityEngine.Object.DestroyImmediate(Find(model.transform, name).gameObject);
        var steeringParts = model.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("InteriorSteeringWheel")).ToArray();
        Bounds steeringBounds = steeringParts[0].GetComponent<Renderer>().bounds;
        foreach (var part in steeringParts.Skip(1)) steeringBounds.Encapsulate(part.GetComponent<Renderer>().bounds);
        var steering = new GameObject("Animated imported steering wheel").transform;
        steering.SetParent(model.transform, false); steering.position = steeringBounds.center;
        steering.rotation = car.transform.rotation * Quaternion.Euler(20f, 0f, 0f);
        foreach (var part in steeringParts) part.SetParent(steering, true);
        presentation.steeringWheel = steering; presentation.rpmNeedle = null;
        presentation.brakeLights = new[] { Find(model.transform, "BodyTaillights").GetComponent<Renderer>() };
        var rig = UnityEngine.Object.FindAnyObjectByType<RacerCameraRig>();
        Vector3 wheelCenter = car.transform.InverseTransformPoint(steeringBounds.center);
        rig.cockpitOffset = new Vector3(wheelCenter.x, wheelCenter.y + 0.33f, wheelCenter.z - 0.38f);
        rig.desktopRestPitch = 8f; rig.cockpit = true; rig.raceCamera.nearClipPlane = 0.025f;
        rig.transform.SetPositionAndRotation(car.transform.TransformPoint(rig.cockpitOffset), car.transform.rotation);
        rig.raceCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(8f, 0f, 0f));
        var dashboard = UnityEngine.Object.FindAnyObjectByType<RaceDashboard>();
        var instrument = Find(model.transform, "InteriorSteeringDash").GetComponent<Renderer>().bounds;
        Vector3 dashCenter = car.transform.InverseTransformPoint(instrument.center);
        dashboard.cockpitText.transform.localPosition = new Vector3(dashCenter.x, dashCenter.y, dashCenter.z - 0.15f);
        dashboard.cockpitText.rectTransform.sizeDelta = new Vector2(0.31f, 0.095f);
        dashboard.cockpitText.fontSizeMin = 0.15f; dashboard.cockpitText.fontSizeMax = 0.55f;
        var body = car.GetComponent<BoxCollider>();
        body.size = new Vector3(1.95f, 0.5f, 3.8f);
        car.name = "Concept Racing Car";
        AddCredit(car.transform, dashCenter);
        PrefabUtility.SaveAsPrefabAsset(car.gameObject, RacingPrototypeBuilder.CarPath);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/downloaded-car-setup.txt", $"Model: Khronos CarConcept\nScale: {model.transform.localScale}\nEye: {rig.cockpitOffset}\nSteering: {wheelCenter}\nDisplay: {dashboard.cockpitText.transform.localPosition}\n");
        RacingPrototypeChecks.Run(); RacingVisualUpgrade.Validate(); RacingPreview.Render();
        Debug.Log("RACINGCAR_DOWNLOADED_MODEL_OK");
    }

    static void AddCredit(Transform car, Vector3 dash)
    {
        var text = new GameObject("Car Model Credit").AddComponent<TextMeshPro>(); text.transform.SetParent(car, false);
        text.transform.localPosition = dash + new Vector3(0.34f, -0.06f, -0.15f);
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        text.rectTransform.sizeDelta = new Vector2(0.28f, 0.10f); text.fontSize = 0.16f; text.color = new Color(0.65f, 0.68f, 0.7f);
        text.text = "Car Concept\nEric Chadwick / DGG\nCC BY 4.0"; text.alignment = TextAlignmentOptions.Center;
    }

    public static void FitView()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        var rig = UnityEngine.Object.FindAnyObjectByType<RacerCameraRig>();
        var steering = rig.car.GetComponent<CarPresentation>().steeringWheel;
        Vector3 center = rig.car.transform.InverseTransformPoint(steering.position);
        rig.cockpitOffset = new Vector3(center.x, center.y + 0.33f, center.z - 0.38f);
        var dashboard = UnityEngine.Object.FindAnyObjectByType<RaceDashboard>();
        dashboard.cockpitText.rectTransform.sizeDelta = new Vector2(0.31f, 0.095f);
        var glass = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Materials/Glass.mat");
        glass.SetFloat("_EnvironmentReflections", 0f); glass.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        var model = rig.car.transform.Find("Downloaded Concept Car");
        if (rig.car.transform.Find("Imported body batches") == null) CombineBody(rig.car.transform, model, steering);
        rig.transform.SetPositionAndRotation(rig.car.transform.TransformPoint(rig.cockpitOffset), rig.car.transform.rotation);
        rig.raceCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(rig.desktopRestPitch, 0f, 0f));
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        PrefabUtility.SaveAsPrefabAsset(rig.car.gameObject, RacingPrototypeBuilder.CarPath);
        AssetDatabase.SaveAssets();
        RacingPreview.Render();
    }

    static void CombineBody(Transform car, Transform model, Transform steering)
    {
        var groups = new Dictionary<Material, List<CombineInstance>>();
        var originals = new List<MeshRenderer>();
        foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.transform.IsChildOf(steering) || filter.name == "BodyTaillights") continue;
            var renderer = filter.GetComponent<MeshRenderer>();
            for (int sub = 0; sub < renderer.sharedMaterials.Length; sub++)
            {
                var material = renderer.sharedMaterials[sub];
                if (!groups.ContainsKey(material)) groups[material] = new List<CombineInstance>();
                groups[material].Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = sub, transform = car.worldToLocalMatrix * filter.transform.localToWorldMatrix });
            }
            originals.Add(renderer);
        }
        var batchRoot = new GameObject("Imported body batches").transform; batchRoot.SetParent(car, false);
        Directory.CreateDirectory(Folder + "Meshes");
        foreach (var group in groups)
        {
            var mesh = new Mesh { name = "Car body " + group.Key.name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(group.Value.ToArray(), true, true);
            AssetDatabase.CreateAsset(mesh, Folder + "Meshes/" + group.Key.name + ".asset");
            var go = new GameObject(group.Key.name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(batchRoot, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = group.Key;
            if (group.Key.name == "Glass") renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
        foreach (var renderer in originals) renderer.enabled = false;
        Debug.Log($"Downloaded car body: {originals.Count} renderers combined into {groups.Count} materials.");
    }
}
