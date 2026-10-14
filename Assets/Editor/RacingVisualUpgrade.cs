using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class RacingVisualUpgrade
{
    const int Sections = 320;
    static Transform scenery;
    static Material asphalt, grass, gravel, paint, red, metal, rubber, carbon, blue, leather, orange;
    static int meshSerial;

    [MenuItem("RacingCar/Apply First Person Visual Upgrade")]
    public static void Apply()
    {
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the scene before upgrading.");
        Directory.CreateDirectory("Backups/BeforeVisualUpgrade");
        Backup(RacingPrototypeBuilder.ScenePath, "RacingPrototype.unity");
        Backup(RacingPrototypeBuilder.CarPath, "PrototypeCar.prefab");
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        if (GameObject.Find("Visual Upgrade v1") != null) throw new InvalidOperationException("Visual upgrade is already applied.");
        Directory.CreateDirectory("Assets/Art/Upgrade/Materials");
        Directory.CreateDirectory("Assets/Art/Upgrade/Meshes");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        InitializeMaterials();
        foreach (string name in new[] { "Oval Circuit", "Circuit Furniture", "Ground" })
        {
            var previous = GameObject.Find(name);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
        }
        scenery = new GameObject("Visual Upgrade v1").transform;
        BuildCircuit();
        BuildScenery();
        var race = UnityEngine.Object.FindFirstObjectByType<RaceSession>();
        BuildVehicle(race.car);
        BuildCockpit(race);
        AddEffects(race.car);
        var rig = UnityEngine.Object.FindFirstObjectByType<RacerCameraRig>();
        rig.cockpit = true;
        rig.cockpitOffset = new Vector3(-0.36f, 0.78f, 0.08f);
        rig.transform.SetPositionAndRotation(race.car.transform.position + rig.cockpitOffset, Quaternion.identity);
        rig.raceCamera.fieldOfView = 75f;
        rig.raceCamera.farClipPlane = 350f;
        rig.raceCamera.backgroundColor = new Color(0.52f, 0.69f, 0.82f);
        foreach (var checkpoint in UnityEngine.Object.FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None))
        {
            float t = checkpoint.index * 2f * Mathf.PI / race.checkpointCount;
            checkpoint.transform.SetPositionAndRotation(Point(t) + Vector3.up * 1.5f, Heading(t));
            for (int i = checkpoint.transform.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(checkpoint.transform.GetChild(i).gameObject);
        }
        race.startPosition = Point(0f) + new Vector3(0f, 0.92f, 2f);
        race.car.transform.position = race.startPosition;
        var sun = GameObject.Find("Sun").GetComponent<Light>();
        sun.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        sun.intensity = 1.4f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.58f, 0.68f, 0.82f);
        RenderSettings.ambientEquatorColor = new Color(0.38f, 0.42f, 0.40f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.20f, 0.15f);
        RenderSettings.fogStartDistance = 145f;
        RenderSettings.fogEndDistance = 320f;
        RenderSettings.fogColor = rig.raceCamera.backgroundColor;
        QualitySettings.shadowDistance = 45f;
        PrefabUtility.SaveAsPrefabAsset(race.car.gameObject, RacingPrototypeBuilder.CarPath);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Polish();
        RacingPrototypeChecks.Run();
        Validate();
        RacingPreview.Render();
        Debug.Log("RACINGCAR_VISUAL_UPGRADE_OK");
    }

    static void Backup(string path, string name)
    {
        string destination = "Backups/BeforeVisualUpgrade/" + name;
        if (!File.Exists(destination)) File.Copy(path, destination);
    }

    static void InitializeMaterials()
    {
        asphalt = Mat("Asphalt PBR", Color.white, 0f, 0.18f);
        string folder = "Assets/Art/Textures/Asphalt012/Asphalt012_1K-JPG_";
        ImportTexture(folder + "Color.jpg", false, false);
        ImportTexture(folder + "NormalGL.jpg", true, false);
        ImportTexture(folder + "Roughness.jpg", false, true);
        asphalt.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "Color.jpg"));
        asphalt.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "NormalGL.jpg"));
        asphalt.SetFloat("_BumpScale", 0.55f);
        asphalt.EnableKeyword("_NORMALMAP");
        var roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "Roughness.jpg");
        var packed = new Texture2D(roughness.width, roughness.height, TextureFormat.RGBA32, false, true);
        var pixels = roughness.GetPixels();
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0f, 0f, 0f, 1f - pixels[i].r);
        packed.SetPixels(pixels); packed.Apply();
        string packedPath = "Assets/Art/Upgrade/AsphaltMetallicSmoothness.png";
        File.WriteAllBytes(packedPath, packed.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(packed);
        AssetDatabase.ImportAsset(packedPath, ImportAssetOptions.ForceSynchronousImport);
        ImportTexture(packedPath, false, false, false);
        ImportTexture(folder + "Roughness.jpg", false, false, false);
        asphalt.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));
        asphalt.SetFloat("_Smoothness", 0.35f);
        asphalt.EnableKeyword("_METALLICSPECGLOSSMAP");
        grass = Mat("Grass", new Color(0.23f, 0.30f, 0.13f));
        gravel = Mat("Gravel", new Color(0.48f, 0.42f, 0.30f));
        paint = Mat("Track Paint", new Color(0.88f, 0.88f, 0.79f));
        red = Mat("Curb Red", new Color(0.62f, 0.065f, 0.025f));
        metal = Mat("Brushed Aluminium", new Color(0.49f, 0.55f, 0.59f), 0.7f, 0.4f);
        rubber = Mat("Tire Rubber", new Color(0.023f, 0.026f, 0.029f), 0f, 0.16f);
        carbon = Mat("Cockpit Graphite", new Color(0.055f, 0.062f, 0.068f), 0.1f, 0.3f);
        blue = Mat("Roadster Paint", new Color(0.028f, 0.22f, 0.34f), 0.55f, 0.74f);
        leather = Mat("Seat Leather", new Color(0.09f, 0.068f, 0.052f), 0f, 0.22f);
        orange = Mat("Signal Amber", new Color(1f, 0.43f, 0.06f), 0.1f, 0.4f);
    }

    static void ImportTexture(string path, bool normal, bool readable, bool srgb = true)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = normal ? false : srgb;
        importer.isReadable = readable;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.anisoLevel = 4;
        importer.maxTextureSize = 1024;
        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
        { name = "Android", overridden = true, maxTextureSize = 1024, format = TextureImporterFormat.ASTC_6x6 });
        importer.SaveAndReimport();
    }

    static Material Mat(string name, Color color, float metallic = 0f, float smoothness = 0.1f)
    {
        string path = "Assets/Art/Upgrade/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color; material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    public static Vector3 Point(float t)
    {
        float s = Mathf.Sin(t);
        return new Vector3(36f * Mathf.Cos(t) + 8f * s * s * Mathf.Sin(2f * t),
            0.03f + 0.45f * s * s * Mathf.Sin(2f * t), 60f * s + 7f * s * s);
    }
    static Vector3 Tangent(float t) => (Point(t + 0.001f) - Point(t - 0.001f)).normalized;
    static Vector3 Normal(float t) => Vector3.Cross(Vector3.up, Tangent(t)).normalized;
    static Quaternion Heading(float t) => Quaternion.LookRotation(Tangent(t), Vector3.up);

    static void BuildCircuit()
    {
        Box("Grass ground", scenery, new Vector3(0f, -0.85f, 0f), new Vector3(500f, 0.5f, 500f), grass, true);
        Ribbon("Grand Prix Circuit", -5f, 5f, 0f, asphalt, true, true);
        Ribbon("Inner gravel shoulder", -6.1f, -5f, -0.012f, gravel);
        Ribbon("Outer gravel shoulder", 5f, 6.1f, -0.012f, gravel);
        Ribbon("Inner edge paint", -4.68f, -4.52f, 0.008f, paint);
        Ribbon("Outer edge paint", 4.52f, 4.68f, 0.008f, paint);
        for (int i = 0; i < Sections; i++)
        {
            float t = i * 2f * Mathf.PI / Sections, next = (i + 1) * 2f * Mathf.PI / Sections;
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 a = Point(t) + Normal(t) * side * 4.95f, b = Point(next) + Normal(next) * side * 4.95f;
                var curb = Box("Curb", scenery, (a + b) * 0.5f + Vector3.up * 0.025f,
                    new Vector3(0.55f, 0.05f, Vector3.Distance(a, b) + 0.025f), i % 8 < 4 ? red : paint, true);
                curb.transform.rotation = Quaternion.LookRotation(b - a);
                if (i % 4 == 0)
                {
                    float end = (i + 4) * 2f * Mathf.PI / Sections;
                    a = Point(t) + Normal(t) * side * 6.4f; b = Point(end) + Normal(end) * side * 6.4f;
                    var rail = Box("Guardrail", scenery, (a + b) * 0.5f + Vector3.up * 0.45f,
                        new Vector3(0.12f, 0.34f, Vector3.Distance(a, b) + 0.06f), metal, true);
                    rail.transform.rotation = Quaternion.LookRotation(b - a);
                    Box("Rail support", scenery, a + Vector3.up * 0.28f, new Vector3(0.1f, 0.65f, 0.1f), metal);
                }
            }
        }
        for (int row = 0; row < 2; row++)
            for (int i = 0; i < 16; i++)
                Box("Finish checker", scenery, new Vector3(31.35f + i * 0.62f, 0.048f, row * 0.62f - 0.3f),
                    new Vector3(0.62f, 0.018f, 0.62f), (i + row) % 2 == 0 ? paint : rubber);
        var gantry = new GameObject("Start gantry").transform; gantry.SetParent(scenery, false);
        Box("Gantry left", gantry, new Vector3(29f, 2.2f, 0f), new Vector3(0.3f, 4.5f, 0.3f), metal);
        Box("Gantry right", gantry, new Vector3(43f, 2.2f, 0f), new Vector3(0.3f, 4.5f, 0.3f), metal);
        Box("Start header", gantry, new Vector3(36f, 4.2f, 0f), new Vector3(14.5f, 0.7f, 0.25f), carbon);
        Label("CIRCUIT  /  RACINGCAR", gantry, new Vector3(36f, 4.2f, -0.16f), 0.2f, new Vector2(12f, 0.6f), paint.color);
    }

    static void Ribbon(string name, float inside, float outside, float lift, Material material, bool collider = false, bool asphaltUV = false)
    {
        var vertices = new Vector3[(Sections + 1) * 2]; var uv = new Vector2[vertices.Length];
        var indices = new int[Sections * 6]; float length = 0f;
        for (int i = 0; i <= Sections; i++)
        {
            float t = i * 2f * Mathf.PI / Sections;
            if (i > 0) length += Vector3.Distance(Point(t), Point((i - 1) * 2f * Mathf.PI / Sections));
            vertices[i * 2] = Point(t) + Normal(t) * inside + Vector3.up * lift;
            vertices[i * 2 + 1] = Point(t) + Normal(t) * outside + Vector3.up * lift;
            uv[i * 2] = new Vector2(0, length / 3f);
            uv[i * 2 + 1] = new Vector2(asphaltUV ? (outside - inside) / 3f : 1f, length / 3f);
            if (i == Sections) continue;
            int v = i * 2, n = i * 6;
            // Normal points outward (+X at start); clockwise lap tangent is +Z.
            indices[n] = v; indices[n + 1] = v + 2; indices[n + 2] = v + 1;
            indices[n + 3] = v + 1; indices[n + 4] = v + 2; indices[n + 5] = v + 3;
        }
        // The ribbon's outward normal makes the order above face upward.
        MeshObject(name, scenery, vertices, uv, indices, material, collider);
    }

    static void BuildScenery()
    {
        // Shared primitives and materials allow instancing; no lights on individual scenery props.
        var pit = new GameObject("Pit buildings").transform; pit.SetParent(scenery, false);
        for (int i = 0; i < 5; i++)
        {
            Vector3 p = new Vector3(59f, 2f, -12f + i * 7f);
            Box("Pit garage", pit, p, new Vector3(10f, 4f, 6.7f), paint);
            Box("Garage shutter", pit, p + new Vector3(-5.03f, -0.5f, 0f), new Vector3(0.06f, 2.7f, 5.6f), carbon);
            Box("Garage amber trim", pit, p + new Vector3(-5.05f, 1.5f, 0f), new Vector3(0.08f, 0.25f, 6.7f), orange);
            Box("Pit roof", pit, p + Vector3.up * 2.2f, new Vector3(10.5f, 0.3f, 7f), metal);
        }
        Box("Pit apron", pit, new Vector3(52f, -0.05f, 2f), new Vector3(16f, 0.15f, 42f), gravel);
        for (int i = 0; i < 3; i++)
        {
            float t = 0.34f + i * 0.12f;
            Vector3 p = Point(t) + Normal(t) * 7.7f;
            Box("Distance sign support", scenery, p + Vector3.up * 0.6f, new Vector3(0.06f, 1.4f, 0.06f), metal);
            var board = Box("Brake distance board", scenery, p + Vector3.up * 1.35f, new Vector3(1.1f, 0.8f, 0.08f), paint);
            board.transform.rotation = Heading(t);
            var text = Label(((3 - i) * 50).ToString(), board.transform, new Vector3(0f, 0f, -0.6f), 0.15f, new Vector2(1f, 0.7f), carbon.color);
            // Parent has scale; cancel it so text retains a sane physical size.
            text.transform.localScale = new Vector3(1f / 1.1f, 1f / 0.8f, 1f / 0.08f);
        }
        var bark = Mat("Tree Bark", new Color(0.17f, 0.105f, 0.06f));
        var leaves = Mat("Tree Canopy", new Color(0.10f, 0.21f, 0.065f));
        for (int i = 0; i < 45; i++)
        {
            float t = i * 2f * Mathf.PI / 45;
            Vector3 p = Point(t) + Normal(t) * (22f + 9f * Mathf.Sin(i * 3.2f));
            if (p.x > 46f && Mathf.Abs(p.z) < 32f) continue;
            float h = 4f + (i % 4) * 0.6f;
            Cylinder("Tree trunk", scenery, p + Vector3.up * h * 0.4f, new Vector3(0.3f, h * 0.4f, 0.3f), bark);
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere); crown.name = "Tree canopy";
            crown.transform.SetParent(scenery, false); crown.transform.localPosition = p + Vector3.up * h;
            crown.transform.localScale = new Vector3(3.3f, 4.5f, 3.3f);
            crown.GetComponent<Renderer>().sharedMaterial = leaves;
            UnityEngine.Object.DestroyImmediate(crown.GetComponent<Collider>());
        }
        var hill = Mat("Distant Hills", new Color(0.27f, 0.34f, 0.26f));
        for (int i = 0; i < 14; i++)
        {
            float t = i * Mathf.PI * 2f / 14f;
            Vector3 center = new Vector3(Mathf.Cos(t) * 190f, -1f, Mathf.Sin(t) * 190f);
            var verts = new List<Vector3>(); var tris = new List<int>();
            verts.Add(center + Vector3.up * (22f + i % 4 * 9f));
            for (int j = 0; j < 12; j++) verts.Add(center + new Vector3(Mathf.Cos(j * Mathf.PI / 6f) * 53f, 0f, Mathf.Sin(j * Mathf.PI / 6f) * 53f));
            for (int j = 0; j < 12; j++) { tris.Add(0); tris.Add(1 + (j + 1) % 12); tris.Add(1 + j); }
            MeshObject("Landscape hill", scenery, verts.ToArray(), new Vector2[verts.Count], tris.ToArray(), hill);
        }
        // Material tint and roughness keep the scene readable without post processing.
    }

    static void BuildVehicle(ArcadeCar car)
    {
        var remove = new List<GameObject>();
        foreach (Transform child in car.transform)
            if (child.GetComponent<WheelCollider>() == null && Array.IndexOf(car.wheelVisuals, child) < 0
                && child.name != "Cockpit Display") remove.Add(child.gameObject);
        foreach (var child in remove) UnityEngine.Object.DestroyImmediate(child);
        car.gameObject.name = "Apex Roadster";
        var root = new GameObject("Roadster bodywork").transform; root.SetParent(car.transform, false);
        // Sculpted hood and rear deck with an open driver compartment, rather than a solid cabin block.
        Shell("Sculpted front hood", root, new[] { 0.75f, 1.0f, 1.45f, 1.86f },
            new[] { 0.79f, 0.82f, 0.79f, 0.65f }, new[] { 0.30f, 0.34f, 0.29f, 0.09f }, blue);
        Shell("Rear engine deck", root, new[] { -1.86f, -1.5f, -1.0f, -0.72f },
            new[] { 0.72f, 0.83f, 0.79f, 0.76f }, new[] { 0.12f, 0.31f, 0.36f, 0.3f }, blue);
        Box("Cabin floor", root, new Vector3(0f, -0.18f, -0.08f), new Vector3(1.52f, 0.06f, 1.65f), carbon);
        foreach (float side in new[] { -1f, 1f })
        {
            var sill = Box("Sculpted side sill", root, new Vector3(side * 0.76f, 0.045f, 0f), new Vector3(0.19f, 0.43f, 3.2f), blue);
            sill.transform.localRotation = Quaternion.Euler(0f, 0f, side * 4f);
            Box("Door inner padding", root, new Vector3(side * 0.65f, 0.19f, -0.03f), new Vector3(0.07f, 0.27f, 1.45f), carbon);
            Box("Side intake", root, new Vector3(side * 0.865f, 0.1f, -0.74f), new Vector3(0.025f, 0.21f, 0.5f), rubber);
            Box("Mirror support", root, new Vector3(side * 0.86f, 0.43f, 0.65f), new Vector3(0.25f, 0.05f, 0.07f), metal);
            Box("Mirror housing", root, new Vector3(side * 1.02f, 0.45f, 0.65f), new Vector3(0.25f, 0.13f, 0.19f), blue);
            // Reflective surface only; these are not live rear-view cameras.
            Box("Mirror reflective inset", root, new Vector3(side * 1.02f, 0.45f, 0.55f), new Vector3(0.21f, 0.095f, 0.009f), metal);
            Beam("Windshield pillar", root, new Vector3(side * 0.73f, 0.28f, 0.93f), new Vector3(side * 0.66f, 1.02f, 0.62f), 0.035f, metal);
            Box("LED headlight", root, new Vector3(side * 0.52f, 0.135f, 1.83f), new Vector3(0.34f, 0.045f, 0.035f), Emissive("Headlight lens", Color.white, 1.5f));
            Box("Seat base", root, new Vector3(side * 0.37f, 0.05f, -0.35f), new Vector3(0.5f, 0.13f, 0.68f), leather);
            var seat = Box("Bucket seat back", root, new Vector3(side * 0.37f, 0.36f, -0.65f), new Vector3(0.5f, 0.63f, 0.13f), leather);
            seat.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            Box("Seat headrest", root, new Vector3(side * 0.37f, 0.73f, -0.72f), new Vector3(0.29f, 0.22f, 0.13f), carbon);
        }
        Beam("Windshield top frame", root, new Vector3(-0.66f, 1.02f, 0.62f), new Vector3(0.66f, 1.02f, 0.62f), 0.028f, metal);
        // Clear windshield omitted in the dry daylight build to avoid an extra transparent layer.
        Box("Hood accent", root, new Vector3(0f, 0.344f, 1.05f), new Vector3(0.18f, 0.007f, 0.48f), orange);
        Box("Front splitter", root, new Vector3(0f, -0.11f, 1.85f), new Vector3(1.62f, 0.045f, 0.25f), carbon);
        Box("Rear diffuser", root, new Vector3(0f, -0.13f, -1.87f), new Vector3(1.48f, 0.11f, 0.12f), rubber);
        Box("Rear wing", root, new Vector3(0f, 0.68f, -1.65f), new Vector3(1.95f, 0.06f, 0.29f), carbon);
        foreach (float x in new[] { -0.5f, 0.5f }) Box("Wing support", root, new Vector3(x, 0.48f, -1.65f), new Vector3(0.04f, 0.4f, 0.16f), metal);
        for (int i = 0; i < 4; i++)
        {
            var wheel = car.wheelVisuals[i];
            for (int child = wheel.childCount - 1; child >= 0; child--) UnityEngine.Object.DestroyImmediate(wheel.GetChild(child).gameObject);
            var tire = Cylinder("Performance tire", wheel, Vector3.zero, new Vector3(0.74f, 0.135f, 0.74f), rubber);
            tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            float outward = i % 2 == 0 ? -1f : 1f;
            var rim = Cylinder("Alloy rim", wheel, new Vector3(outward * 0.142f, 0f, 0f), new Vector3(0.49f, 0.012f, 0.49f), metal);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var hub = Cylinder("Wheel hub", wheel, new Vector3(outward * 0.158f, 0f, 0f), new Vector3(0.10f, 0.02f, 0.10f), carbon);
            hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            for (int spoke = 0; spoke < 5; spoke++)
            {
                float a = spoke * Mathf.PI * 2f / 5f;
                var part = Box("Alloy spoke", wheel, new Vector3(outward * 0.165f, Mathf.Cos(a) * 0.13f, Mathf.Sin(a) * 0.13f), new Vector3(0.025f, 0.22f, 0.05f), carbon);
                part.transform.localRotation = Quaternion.Euler(spoke * 72f, 0f, 0f);
            }
        }
    }

    static void BuildCockpit(RaceSession race)
    {
        var car = race.car;
        var root = car.transform.Find("Roadster bodywork");
        Box("Sculpted dashboard", root, new Vector3(0f, 0.32f, 0.93f), new Vector3(1.47f, 0.24f, 0.24f), carbon);
        Box("Instrument binnacle", root, new Vector3(-0.35f, 0.59f, 0.92f), new Vector3(0.65f, 0.25f, 0.09f), rubber);
        var text = UnityEngine.Object.FindFirstObjectByType<RaceDashboard>().cockpitText;
        text.transform.localPosition = new Vector3(-0.35f, 0.59f, 0.867f);
        text.rectTransform.sizeDelta = new Vector2(0.60f, 0.21f);
        text.fontSizeMin = 0.3f; text.fontSizeMax = 0.9f;
        text.color = new Color(0.72f, 1f, 0.87f);
        Box("Center console", root, new Vector3(0f, 0.10f, 0.04f), new Vector3(0.17f, 0.18f, 1.3f), carbon);
        var wheel = new GameObject("Animated steering wheel").transform; wheel.SetParent(root, false);
        wheel.localPosition = new Vector3(-0.36f, 0.36f, 0.64f); wheel.localRotation = Quaternion.Euler(14f, 0f, 0f);
        Torus("Leather steering rim", wheel, 0.16f, 0.022f, rubber);
        var hub = Cylinder("Steering hub", wheel, Vector3.zero, new Vector3(0.115f, 0.019f, 0.115f), carbon);
        hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        foreach (float angle in new[] { 90f, 210f, 330f })
        {
            float a = angle * Mathf.Deg2Rad;
            Beam("Steering spoke", wheel, Vector3.zero, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.15f, 0.025f, metal);
        }
        Box("Steering center badge", wheel, new Vector3(0f, 0f, -0.028f), new Vector3(0.045f, 0.035f, 0.005f), orange);
        var tach = Cylinder("Tachometer face", root, new Vector3(0.19f, 0.47f, 0.86f), new Vector3(0.21f, 0.008f, 0.21f), rubber);
        tach.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var needle = new GameObject("RPM needle pivot").transform; needle.SetParent(root, false);
        needle.localPosition = new Vector3(0.19f, 0.47f, 0.843f);
        Box("RPM needle", needle, new Vector3(0f, 0.035f, 0f), new Vector3(0.005f, 0.075f, 0.005f), orange);
        for (int i = 0; i < 9; i++)
        {
            float a = (215f - i * 31f) * Mathf.Deg2Rad;
            var tick = Box("Tachometer tick", root, new Vector3(0.19f + Mathf.Cos(a) * 0.088f, 0.47f + Mathf.Sin(a) * 0.088f, 0.844f), new Vector3(0.012f, 0.004f, 0.003f), i > 6 ? red : paint);
            tick.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
        }
        var presentation = car.gameObject.AddComponent<CarPresentation>();
        presentation.car = car; presentation.steeringWheel = wheel; presentation.rpmNeedle = needle;
        presentation.brakeLights = new Renderer[2];
        for (int i = 0; i < 2; i++) presentation.brakeLights[i] = Box("Brake light", root, new Vector3(i == 0 ? -0.53f : 0.53f, 0.15f, -1.875f), new Vector3(0.38f, 0.055f, 0.03f), Emissive("Brake lens", new Color(1f, 0.025f, 0.01f), 0.15f)).GetComponent<Renderer>();
    }

    static Material Emissive(string name, Color color, float emission)
    {
        var material = Mat(name, color, 0.1f, 0.5f);
        material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * emission);
        return material;
    }

    static void AddEffects(ArcadeCar car)
    {
        var effect = car.gameObject.AddComponent<CarDrivingEffects>(); effect.car = car;
        effect.smoke = new ParticleSystem[4]; effect.dust = new ParticleSystem[4];
        var particles = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Soft driving particles" };
        particles.SetFloat("_Surface", 1); particles.SetFloat("_Blend", 0);
        particles.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); particles.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        particles.SetFloat("_ZWrite", 0); particles.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        particles.renderQueue = (int)RenderQueue.Transparent;
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        {
            float radius = new Vector2((x - 31.5f) / 31.5f, (y - 31.5f) / 31.5f).magnitude;
            pixels[y * 64 + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - radius), 2f));
        }
        texture.SetPixels(pixels); texture.Apply();
        string path = "Assets/Art/Upgrade/SoftParticle.png";
        File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        particles.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        AssetDatabase.CreateAsset(particles, "Assets/Art/Upgrade/Materials/Driving Particles.mat");
        effect.skidMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Skid Rubber", color = new Color(0.045f, 0.043f, 0.040f) };
        AssetDatabase.CreateAsset(effect.skidMaterial, "Assets/Art/Upgrade/Materials/Skid Rubber.mat");
        for (int i = 0; i < 4; i++)
        {
            effect.smoke[i] = Particle("Tire smoke " + i, car.transform, particles, new Color(0.78f, 0.78f, 0.73f, 0.32f), 0.4f, 0.85f, 32);
            effect.dust[i] = Particle("Surface dust " + i, car.transform, particles, new Color(0.48f, 0.37f, 0.20f, 0.48f), 0.24f, 0.7f, 48);
        }
        effect.sparks = Particle("Metal scrape sparks", car.transform, particles, new Color(1f, 0.55f, 0.08f, 1f), 0.05f, 0.25f, 40);
        var main = effect.sparks.main; main.gravityModifier = 1f;
    }

    static ParticleSystem Particle(string name, Transform parent, Material material, Color color, float size, float life, int budget)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var system = go.AddComponent<ParticleSystem>(); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main; main.playOnAwake = true; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = life; main.startSize = size; main.startSpeed = 0f; main.startColor = color; main.maxParticles = budget;
        var emission = system.emission; emission.enabled = false;
        var shape = system.shape; shape.enabled = false;
        var overLife = system.colorOverLifetime; overLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        overLife.color = gradient;
        var growth = system.sizeOverLifetime; growth.enabled = true;
        growth.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.7f));
        var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        return system;
    }

    static void Shell(string name, Transform parent, float[] z, float[] width, float[] top, Material material)
    {
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
        for (int i = 0; i < z.Length; i++)
        {
            vertices.Add(new Vector3(-width[i], -0.14f, z[i])); vertices.Add(new Vector3(-width[i], top[i] - 0.08f, z[i]));
            vertices.Add(new Vector3(-width[i] * 0.72f, top[i], z[i])); vertices.Add(new Vector3(width[i] * 0.72f, top[i], z[i]));
            vertices.Add(new Vector3(width[i], top[i] - 0.08f, z[i])); vertices.Add(new Vector3(width[i], -0.14f, z[i]));
            for (int j = 0; j < 6; j++) uv.Add(new Vector2(j / 5f, i / (float)(z.Length - 1)));
            if (i == 0) continue;
            for (int j = 0; j < 5; j++)
            {
                int a = (i - 1) * 6 + j, b = i * 6 + j;
                triangles.Add(a); triangles.Add(b); triangles.Add(a + 1); triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
            }
        }
        // End caps, oriented outwards. The bottom is hidden by the chassis.
        for (int j = 1; j < 5; j++) { triangles.Add(0); triangles.Add(j + 1); triangles.Add(j); }
        int last = (z.Length - 1) * 6;
        for (int j = 1; j < 5; j++) { triangles.Add(last); triangles.Add(last + j); triangles.Add(last + j + 1); }
        MeshObject(name, parent, vertices.ToArray(), uv.ToArray(), triangles.ToArray(), material);
    }

    static void Torus(string name, Transform parent, float radius, float tube, Material material)
    {
        const int around = 48, cross = 8;
        var vertices = new Vector3[(around + 1) * (cross + 1)]; var uv = new Vector2[vertices.Length]; var indices = new int[around * cross * 6];
        for (int i = 0; i <= around; i++) for (int j = 0; j <= cross; j++)
        {
            float a = i * 2f * Mathf.PI / around, b = j * 2f * Mathf.PI / cross;
            int v = i * (cross + 1) + j;
            vertices[v] = new Vector3(Mathf.Cos(a) * (radius + tube * Mathf.Cos(b)), Mathf.Sin(a) * (radius + tube * Mathf.Cos(b)), tube * Mathf.Sin(b));
            uv[v] = new Vector2(i / (float)around, j / (float)cross);
            if (i == around || j == cross) continue;
            int n = (i * cross + j) * 6, next = v + cross + 1;
            indices[n] = v; indices[n + 1] = next; indices[n + 2] = v + 1;
            indices[n + 3] = v + 1; indices[n + 4] = next; indices[n + 5] = next + 1;
        }
        MeshObject(name, parent, vertices, uv, indices, material);
    }

    static GameObject MeshObject(string name, Transform parent, Vector3[] vertices, Vector2[] uv, int[] triangles, Material material, bool collision = false)
    {
        var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, $"Assets/Art/Upgrade/Meshes/{name.Replace('/', '-')}-{meshSerial++}.asset");
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (collision) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        return go;
    }

    static GameObject Box(string name, Transform parent, Vector3 p, Vector3 scale, Material mat, bool collision = false)
        => Primitive(PrimitiveType.Cube, name, parent, p, scale, mat, collision);
    static GameObject Cylinder(string name, Transform parent, Vector3 p, Vector3 scale, Material mat)
        => Primitive(PrimitiveType.Cylinder, name, parent, p, scale, mat, false);
    static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 p, Vector3 scale, Material mat, bool collision)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = p; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static void Beam(string name, Transform parent, Vector3 a, Vector3 b, float thickness, Material material)
    {
        var beam = Box(name, parent, (a + b) * 0.5f, new Vector3(thickness, thickness, Vector3.Distance(a, b)), material);
        beam.transform.localRotation = Quaternion.LookRotation(b - a);
    }
    static TextMeshPro Label(string text, Transform parent, Vector3 p, float size, Vector2 bounds, Color color)
    {
        var label = new GameObject(text).AddComponent<TextMeshPro>(); label.transform.SetParent(parent, false);
        label.transform.localPosition = p; label.rectTransform.sizeDelta = bounds;
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.text = text; label.fontSize = size * 10f; label.alignment = TextAlignmentOptions.Center; label.color = color;
        return label;
    }

    [MenuItem("RacingCar/Polish Cockpit And Batch Scenery")]
    public static void Polish()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        var car = UnityEngine.Object.FindAnyObjectByType<ArcadeCar>();
        if (car.transform.Find("Downloaded Concept Car") != null) { DownloadedCarSetup.FitView(); return; }
        var rig = UnityEngine.Object.FindAnyObjectByType<RacerCameraRig>();
        var presentation = car.GetComponent<CarPresentation>();
        presentation.steeringWheel.localPosition = new Vector3(-0.36f, 0.36f, 0.64f);
        var rim = presentation.steeringWheel.Find("Leather steering rim").GetComponent<MeshFilter>();
        if (rim.sharedMesh.bounds.size.x > 0.38f) presentation.steeringWheel.localScale = Vector3.one * 0.88f;
        var dashboard = UnityEngine.Object.FindAnyObjectByType<RaceDashboard>();
        dashboard.cockpitText.transform.localPosition = new Vector3(-0.35f, 0.59f, 0.867f);
        dashboard.cockpitText.rectTransform.sizeDelta = new Vector2(0.60f, 0.21f);
        car.transform.Find("Roadster bodywork/Instrument binnacle").localPosition = new Vector3(-0.35f, 0.59f, 0.92f);
        car.transform.Find("Roadster bodywork/Instrument binnacle").localScale = new Vector3(0.65f, 0.25f, 0.09f);
        rig.desktopRestPitch = 10f;
        rig.cockpit = true;
        rig.transform.SetPositionAndRotation(car.transform.position + rig.cockpitOffset, Quaternion.identity);
        rig.raceCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(10f, 0f, 0f));
        var furniture = GameObject.Find("Visual Upgrade v1").transform;
        if (furniture.Find("Batched scenery") == null) BatchScenery(furniture);
        CreateEnvironmentReflection();
        PrefabUtility.SaveAsPrefabAsset(car.gameObject, RacingPrototypeBuilder.CarPath);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Validate();
        RacingPreview.Render();
        Debug.Log("RACINGCAR_VISUAL_POLISH_OK");
    }

    static void BatchScenery(Transform root)
    {
        var batchRoot = new GameObject("Batched scenery").transform; batchRoot.SetParent(root, false);
        var groups = new Dictionary<string, List<CombineInstance>>();
        var materials = new Dictionary<string, Material>();
        var originals = new List<GameObject>();
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || filter.GetComponent<TMP_Text>() != null || filter.name == "Grand Prix Circuit") continue;
            var material = renderer.sharedMaterial;
            Vector3 center = renderer.bounds.center;
            string key = material.name + "_" + Mathf.FloorToInt(center.x / 32f) + "_" + Mathf.FloorToInt(center.z / 32f);
            if (!groups.ContainsKey(key)) { groups[key] = new List<CombineInstance>(); materials[key] = material; }
            groups[key].Add(new CombineInstance { mesh = filter.sharedMesh, transform = filter.transform.localToWorldMatrix });
            originals.Add(filter.gameObject);
        }
        foreach (var group in groups)
        {
            var mesh = new Mesh { name = group.Key, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(group.Value.ToArray(), true, true);
            AssetDatabase.CreateAsset(mesh, "Assets/Art/Upgrade/Meshes/Batch-" + group.Key + ".asset");
            var go = new GameObject(group.Key, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(batchRoot, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = materials[group.Key];
        }
        foreach (var original in originals)
        {
            UnityEngine.Object.DestroyImmediate(original.GetComponent<MeshRenderer>());
            UnityEngine.Object.DestroyImmediate(original.GetComponent<MeshFilter>());
            // Retain colliders and sign transforms referenced by children.
        }
        Debug.Log($"Scenery batched: {originals.Count} renderers into {groups.Count} spatial material groups.");
    }

    static void CreateEnvironmentReflection()
    {
        const string path = "Assets/Art/Upgrade/DaylightReflection.asset";
        var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
        if (cube == null)
        {
            cube = new Cubemap(64, TextureFormat.RGBA32, true);
            for (int face = 0; face < 6; face++) for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float u = (x + 0.5f) / 32f - 1f, v = (y + 0.5f) / 32f - 1f;
                Vector3 dir = face == 0 ? new Vector3(1f, -v, -u) : face == 1 ? new Vector3(-1f, -v, u)
                    : face == 2 ? new Vector3(u, 1f, v) : face == 3 ? new Vector3(u, -1f, -v)
                    : face == 4 ? new Vector3(u, -v, 1f) : new Vector3(-u, -v, -1f);
                dir.Normalize();
                Color color = dir.y > 0f ? Color.Lerp(new Color(0.72f, 0.76f, 0.75f), new Color(0.30f, 0.49f, 0.68f), dir.y)
                    : Color.Lerp(new Color(0.32f, 0.33f, 0.24f), new Color(0.14f, 0.16f, 0.10f), -dir.y);
                float sun = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(dir, new Vector3(-0.4f, 0.8f, -0.3f).normalized)), 160f);
                cube.SetPixel((CubemapFace)face, x, y, color + new Color(1f, 0.9f, 0.7f) * sun);
            }
            cube.Apply(true, false); AssetDatabase.CreateAsset(cube, path);
        }
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = cube;
        RenderSettings.reflectionIntensity = 0.75f;
    }

    [MenuItem("RacingCar/Validate Visual Upgrade")]
    public static void Validate()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCar>();
        var rig = UnityEngine.Object.FindFirstObjectByType<RacerCameraRig>();
        if (!rig.cockpit || car.GetComponent<CarDrivingEffects>() == null || car.GetComponent<CarPresentation>() == null)
            throw new InvalidOperationException("Cockpit, presentation or effects are missing.");
        Physics.SyncTransforms();
        var track = GameObject.Find("Grand Prix Circuit").GetComponent<MeshCollider>();
        for (int i = 0; i < Sections; i++)
        {
            Vector3 p = Point(i * 2f * Mathf.PI / Sections);
            if (!track.Raycast(new Ray(p + Vector3.up * 3f, Vector3.down), out RaycastHit hit, 5f)
                || Mathf.Abs(hit.point.y - p.y) > 0.06f)
                throw new InvalidOperationException("Track contact failed at sample " + i);
        }
        foreach (var checkpoint in UnityEngine.Object.FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None))
        {
            Vector3 p = Point(checkpoint.index * 2f * Mathf.PI / 8f) + Vector3.up * 1.5f;
            if (Vector3.Distance(p, checkpoint.transform.position) > 0.02f) throw new InvalidOperationException("Checkpoint alignment failed.");
        }
        File.WriteAllText("Logs/visual-upgrade-checks.txt", "PASS: first-person default, cockpit/effects references, 320 road contact samples, eight checkpoint positions.\n");
        Debug.Log("RACINGCAR_VISUAL_CHECKS_OK");
    }
}
