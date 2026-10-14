using System;
using System.IO;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class RacingPrototypeBuilder
{
    public const string ScenePath = "Assets/Scenes/RacingPrototype.unity";
    public const string CarPath = "Assets/Prefabs/PrototypeCar.prefab";
    const int Segments = 96;
    const float RadiusX = 36f, RadiusZ = 60f, HalfWidth = 5f;

    [MenuItem("RacingCar/Create Driving Prototype")]
    public static void Create()
    {
        if (File.Exists(ScenePath))
            throw new InvalidOperationException("The prototype already exists. Open Assets/Scenes/RacingPrototype.unity to keep your scene edits.");
        Directory.CreateDirectory("Assets/Art/Materials");
        Directory.CreateDirectory("Assets/Art/Meshes");
        Directory.CreateDirectory("Assets/Prefabs");
        AssetDatabase.Refresh();
        ImportTextResources();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var grass = Material("Grass", new Color(0.13f, 0.25f, 0.21f));
        var road = Material("Asphalt", new Color(0.11f, 0.14f, 0.19f));
        var white = Material("Pearl", new Color(0.88f, 0.94f, 0.96f));
        var orange = Material("Amber", new Color(1f, 0.42f, 0.1f));
        var blue = Material("Car Blue", new Color(0.035f, 0.43f, 0.85f), 0.35f);
        var black = Material("Rubber", new Color(0.035f, 0.045f, 0.06f));

        Cube("Ground", null, new Vector3(0f, -0.2f, 0f), new Vector3(130f, 0.4f, 180f), grass);
        BuildRoad(road);
        var scenery = new GameObject("Circuit Furniture").transform;
        for (int i = 0; i < Segments; i++)
        {
            float t = i * Mathf.PI * 2f / Segments;
            float next = (i + 1) * Mathf.PI * 2f / Segments;
            Barrier(scenery, t, next, HalfWidth + 0.5f, i % 4 < 2 ? white : orange);
            Barrier(scenery, t, next, -HalfWidth - 0.5f, i % 4 < 2 ? white : orange);
            if (i % 2 == 0)
            {
                var dash = Cube("Lane dash", scenery, Point(t) + Vector3.up * 0.045f, new Vector3(0.14f, 0.025f, 1.8f), white, false);
                dash.transform.rotation = Heading(t);
            }
        }
        for (int i = 0; i < 10; i++)
            Cube("Finish stripe", scenery, new Vector3(RadiusX - 4.5f + i, 0.065f, 0f), new Vector3(1f, 0.035f, 0.75f), i % 2 == 0 ? white : black, false);

        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.3f;
        sun.color = new Color(1f, 0.91f, 0.8f);
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.55f, 0.65f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.46f, 0.65f, 0.78f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 110f;
        RenderSettings.fogEndDistance = 220f;

        var car = BuildCar(blue, orange, black, white);
        car.transform.position = new Vector3(RadiusX, 0.95f, 2f);
        PrefabUtility.SaveAsPrefabAsset(car.gameObject, CarPath);
        var race = new GameObject("Race Session").AddComponent<RaceSession>();
        race.car = car;
        race.startPosition = car.transform.position;
        race.startEuler = car.transform.eulerAngles;
        for (int i = 0; i < race.checkpointCount; i++)
        {
            float t = i * 2f * Mathf.PI / race.checkpointCount;
            var gate = new GameObject($"Checkpoint {i + 1}");
            gate.transform.SetPositionAndRotation(Point(t) + Vector3.up * 1.5f, Heading(t));
            var trigger = gate.AddComponent<BoxCollider>();
            trigger.size = new Vector3(HalfWidth * 2f, 4f, 1f);
            trigger.isTrigger = true;
            var checkpoint = gate.AddComponent<RaceCheckpoint>();
            checkpoint.index = i;
            checkpoint.race = race;
            Cube("Left gate post", gate.transform, new Vector3(-4.9f, -0.2f, 0f), new Vector3(0.18f, 2.6f, 0.18f), orange, false);
            Cube("Right gate post", gate.transform, new Vector3(4.9f, -0.2f, 0f), new Vector3(0.18f, 2.6f, 0.18f), orange, false);
        }
        var rig = BuildCamera(car);
        var input = race.gameObject.AddComponent<RaceDriverInput>();
        input.race = race;
        input.view = rig;
        var dashboard = race.gameObject.AddComponent<RaceDashboard>();
        dashboard.race = race;
        dashboard.engine = car.gameObject.AddComponent<AudioSource>();
        dashboard.engine.playOnAwake = false;
        dashboard.engine.spatialBlend = 0f;
        var display = new GameObject("Cockpit Display").AddComponent<TextMeshPro>();
        display.transform.SetParent(car.transform, false);
        display.transform.localPosition = new Vector3(-0.22f, 0.44f, 0.77f);
        display.rectTransform.sizeDelta = new Vector2(0.95f, 0.25f);
        display.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        display.fontSize = 1.2f;
        display.enableAutoSizing = true;
        display.fontSizeMin = 0.5f;
        display.fontSizeMax = 1.2f;
        display.alignment = TextAlignmentOptions.Center;
        display.color = new Color(0.65f, 1f, 0.93f);
        dashboard.cockpitText = display;
        Cube("Dashboard panel", car.transform, new Vector3(-0.22f, 0.44f, 0.79f), new Vector3(1f, 0.28f, 0.03f), black, false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("RACINGCAR_PROTOTYPE_CREATED: " + ScenePath);
    }

    static void ImportTextResources()
    {
        if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) return;
        throw new InvalidOperationException("Restore the project's Assets/TextMesh Pro resources, or import TMP Essential Resources from Window > TextMeshPro before generating the scene.");
    }

    public static void RepairCockpitFont()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EditorSceneManager.OpenScene(ScenePath);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) throw new InvalidOperationException("The cockpit font resource is missing.");
        var display = UnityEngine.Object.FindFirstObjectByType<RaceDashboard>().cockpitText;
        display.font = font;
        display.text = "0 <size=60%>KM/H</size>   LAP 1\n00:00.00   BEST --:--.--\nGATE 2/8";
        display.ForceMeshUpdate();
        EditorUtility.SetDirty(display);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        RacingPreview.Render();
    }

    static Material Material(string name, Color color, float metallic = 0f)
    {
        string path = $"Assets/Art/Materials/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color, enableInstancing = true };
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", metallic > 0f ? 0.55f : 0.15f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collision = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static Vector3 Point(float t) => new Vector3(RadiusX * Mathf.Cos(t), 0.03f, RadiusZ * Mathf.Sin(t));
    static Vector3 Normal(float t) => new Vector3(Mathf.Cos(t) / RadiusX, 0f, Mathf.Sin(t) / RadiusZ).normalized;
    static Quaternion Heading(float t) => Quaternion.LookRotation(new Vector3(-RadiusX * Mathf.Sin(t), 0f, RadiusZ * Mathf.Cos(t)));

    static void BuildRoad(Material material)
    {
        var vertices = new Vector3[(Segments + 1) * 2];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[Segments * 6];
        for (int i = 0; i <= Segments; i++)
        {
            float t = i * Mathf.PI * 2f / Segments;
            vertices[i * 2] = Point(t) - Normal(t) * HalfWidth;
            vertices[i * 2 + 1] = Point(t) + Normal(t) * HalfWidth;
            uv[i * 2] = new Vector2(0f, i);
            uv[i * 2 + 1] = new Vector2(1f, i);
            if (i == Segments) continue;
            int a = i * 2, n = i * 6;
            triangles[n] = a; triangles[n + 1] = a + 2; triangles[n + 2] = a + 1;
            triangles[n + 3] = a + 1; triangles[n + 4] = a + 2; triangles[n + 5] = a + 3;
        }
        var mesh = new Mesh { name = "Oval Circuit", vertices = vertices, uv = uv, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, "Assets/Art/Meshes/OvalCircuit.asset");
        var go = new GameObject("Oval Circuit", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        go.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    static void Barrier(Transform parent, float t, float next, float offset, Material material)
    {
        Vector3 a = Point(t) + Normal(t) * offset, b = Point(next) + Normal(next) * offset;
        var go = Cube("Safety barrier", parent, (a + b) * 0.5f + Vector3.up * 0.3f, new Vector3(0.4f, 0.6f, Vector3.Distance(a, b) + 0.1f), material);
        go.transform.rotation = Quaternion.LookRotation(b - a);
    }

    static ArcadeCar BuildCar(Material blue, Material orange, Material black, Material white)
    {
        var go = new GameObject("Prototype Car");
        var body = go.AddComponent<Rigidbody>();
        body.mass = 900f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearDamping = 0.04f;
        body.angularDamping = 0.8f;
        var collision = go.AddComponent<BoxCollider>();
        collision.center = new Vector3(0f, 0.04f, 0f);
        collision.size = new Vector3(1.65f, 0.5f, 3.7f);
        var car = go.AddComponent<ArcadeCar>();
        car.wheels = new WheelCollider[4];
        car.wheelVisuals = new Transform[4];
        Cube("Body", go.transform, Vector3.zero, new Vector3(1.65f, 0.43f, 3.7f), blue, false);
        Cube("Hood", go.transform, new Vector3(0f, 0.25f, 1.05f), new Vector3(1.52f, 0.12f, 1.4f), blue, false);
        Cube("Racing stripe", go.transform, new Vector3(0f, 0.315f, 1.05f), new Vector3(0.25f, 0.012f, 1.4f), orange, false);
        Cube("Rear wing", go.transform, new Vector3(0f, 0.55f, -1.55f), new Vector3(1.95f, 0.13f, 0.4f), black, false);
        Cube("Left seat", go.transform, new Vector3(-0.4f, 0.28f, -0.38f), new Vector3(0.5f, 0.5f, 0.65f), black, false);
        Cube("Right seat", go.transform, new Vector3(0.4f, 0.28f, -0.38f), new Vector3(0.5f, 0.5f, 0.65f), black, false);
        for (int side = -1; side <= 1; side += 2)
            Cube("Headlight", go.transform, new Vector3(side * 0.55f, 0.12f, 1.865f), new Vector3(0.35f, 0.12f, 0.03f), white, false);
        for (int i = 0; i < 4; i++)
        {
            var wheel = new GameObject("Wheel physics " + i).AddComponent<WheelCollider>();
            wheel.transform.SetParent(go.transform, false);
            wheel.transform.localPosition = new Vector3(i % 2 == 0 ? -0.92f : 0.92f, -0.22f, i < 2 ? 1.2f : -1.2f);
            wheel.radius = 0.37f;
            wheel.mass = 25f;
            wheel.suspensionDistance = 0.2f;
            wheel.suspensionSpring = new JointSpring { spring = 35000f, damper = 4500f, targetPosition = 0.5f };
            var visual = new GameObject("Wheel visual " + i).transform;
            visual.SetParent(go.transform, false);
            visual.localPosition = wheel.transform.localPosition;
            var tyre = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tyre.name = "Tyre";
            tyre.transform.SetParent(visual, false);
            tyre.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tyre.transform.localScale = new Vector3(0.74f, 0.15f, 0.74f);
            tyre.GetComponent<Renderer>().sharedMaterial = black;
            UnityEngine.Object.DestroyImmediate(tyre.GetComponent<Collider>());
            car.wheels[i] = wheel;
            car.wheelVisuals[i] = visual;
        }
        return car;
    }

    static RacerCameraRig BuildCamera(ArcadeCar car)
    {
        var root = new GameObject("Seated XR Origin");
        root.transform.position = car.transform.position + new Vector3(0f, 3.5f, -7f);
        var origin = root.AddComponent<XROrigin>();
        var offset = new GameObject("Camera Offset");
        offset.transform.SetParent(root.transform, false);
        origin.CameraFloorOffsetObject = offset;
        origin.CameraYOffset = 0f;
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
        var cameraObject = new GameObject("Race Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(offset.transform, false);
        var camera = cameraObject.GetComponent<Camera>();
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 250f;
        camera.fieldOfView = 70f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.46f, 0.65f, 0.78f);
        origin.Camera = camera;
        var driver = cameraObject.AddComponent<TrackedPoseDriver>();
        driver.positionInput = new InputActionProperty(new InputAction("Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
        driver.rotationInput = new InputActionProperty(new InputAction("Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
        driver.trackingStateInput = new InputActionProperty(new InputAction("Head Tracking", InputActionType.Value, "<XRHMD>/trackingState", expectedControlType: "Integer"));
        var rig = root.AddComponent<RacerCameraRig>();
        rig.car = car;
        rig.raceCamera = camera;
        rig.poseDriver = driver;
        return rig;
    }
}
