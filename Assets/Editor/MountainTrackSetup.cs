using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MountainTrackSetup
{
    const string Source = "Assets/Snow mountain tracks/s1.unity";
    const string Output = "Assets/Art/Mountain";
    [Serializable] class Geometry { public Vector3[] vertices; public int[] triangles; }
    public static void Inspect()
    {
        EditorSceneManager.OpenScene(Source);
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            Debug.Log("MOUNTAIN_ROOT " + root.name + " " + root.transform.position);
        foreach (var mesh in UnityEngine.Object.FindObjectsByType<MeshFilter>())
        {
            Debug.Log("MOUNTAIN_MESH " + mesh.name + " " + mesh.sharedMesh.vertexCount + " " + mesh.GetComponent<Renderer>().bounds);
            if (mesh.sharedMesh.name.ToLowerInvariant().Contains("road"))
                File.WriteAllText("Logs/mountain-road.json", JsonUtility.ToJson(new Geometry {
                    vertices = mesh.sharedMesh.vertices.Select(v => mesh.transform.TransformPoint(v)).ToArray(), triangles = mesh.sharedMesh.triangles }));
        }
        foreach (var terrain in UnityEngine.Object.FindObjectsByType<Terrain>())
            Debug.Log("MOUNTAIN_TERRAIN " + terrain.transform.position + " " + terrain.terrainData.size);
    }

    public static void Apply()
    {
        Directory.CreateDirectory(Output);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(Source);
        var sourceRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var root = new GameObject("Mountain Race Tracks");
        MeshFilter road = null;
        var materials = new System.Collections.Generic.Dictionary<Material, Material>();
        foreach (var item in sourceRoots)
        {
            if (item.GetComponent<Camera>() != null || item.GetComponent<Light>() != null) continue;
            item.transform.SetParent(root.transform, true);
            foreach (var filter in item.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh.name == "road") road = filter;
                if (filter.GetComponent<MeshCollider>() == null) filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                var renderer = filter.GetComponent<Renderer>();
                renderer.sharedMaterials = renderer.sharedMaterials.Select(original =>
                {
                    if (materials.TryGetValue(original, out var converted)) return converted;
                    converted = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    converted.name = original.name;
                    converted.SetTexture("_BaseMap", original.mainTexture);
                    converted.SetColor("_BaseColor", original.color);
                    converted.SetTextureScale("_BaseMap", original.mainTextureScale);
                    converted.SetTextureOffset("_BaseMap", original.mainTextureOffset);
                    converted.SetFloat("_Smoothness", 0.15f);
                    string path = Output + "/" + original.name.Replace('/', '-') + ".mat";
                    if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(converted, path); materials.Add(original, converted);
                    return converted;
                }).ToArray();
            }
        }
        if (road == null) throw new Exception("The downloaded road mesh is missing.");
        road.gameObject.name = "Mountain Asphalt";
        road.gameObject.AddComponent<RoadSurface>();
        var terrain = root.GetComponentInChildren<Terrain>();
        var data = UnityEngine.Object.Instantiate(terrain.terrainData);
        data.treeInstances = Array.Empty<TreeInstance>(); data.treePrototypes = Array.Empty<TreePrototype>();
        string terrainPath = Output + "/MountainTerrain.asset";
        if (AssetDatabase.LoadAssetAtPath<TerrainData>(terrainPath) != null) AssetDatabase.DeleteAsset(terrainPath);
        AssetDatabase.CreateAsset(data, terrainPath);
        terrain.terrainData = data; terrain.GetComponent<TerrainCollider>().terrainData = data;
        terrain.heightmapPixelError = 12; terrain.basemapDistance = 250; terrain.drawInstanced = true;
        terrain.drawTreesAndFoliage = false;
        var terrainMaterial = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
        string terrainMatPath = Output + "/Terrain.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(terrainMatPath) != null) AssetDatabase.DeleteAsset(terrainMatPath);
        AssetDatabase.CreateAsset(terrainMaterial, terrainMatPath); terrain.materialTemplate = terrainMaterial;
        var vertices = road.sharedMesh.vertices;
        var route = new Vector3[vertices.Length / 2 - 1];
        for (int i = 0; i < route.Length; i++) route[i] = road.transform.TransformPoint((vertices[i * 2] + vertices[i * 2 + 1]) * 0.5f);
        for (int i = 0; i < route.Length; i += 5)
            if (!road.GetComponent<MeshCollider>().Raycast(new Ray(route[i] + Vector3.up * 3, Vector3.down), out _, 4))
                throw new Exception("Road collision gap at route sample " + i);
        File.WriteAllText("Logs/mountain-route.json", JsonUtility.ToJson(new Geometry {vertices = route}));
        float length = 0;
        for (int i = 0; i < route.Length; i++) length += Vector3.Distance(route[i], route[(i + 1) % route.Length]);
        float travelled = 0; int gateNumber = 0;
        for (int i = 0; i < route.Length && gateNumber < 16; i++)
        {
            if (travelled >= length * gateNumber / 16f)
            {
                var gate = new GameObject("Mountain Checkpoint " + gateNumber, typeof(BoxCollider), typeof(RaceCheckpoint));
                gate.transform.SetParent(root.transform, false);
                gate.transform.position = route[i] + Vector3.up * 2;
                gate.transform.rotation = Quaternion.LookRotation(route[(i + 3) % route.Length] - route[i]);
                var collider = gate.GetComponent<BoxCollider>(); collider.isTrigger = true; collider.size = new Vector3(20, 8, 3);
                gate.GetComponent<RaceCheckpoint>().index = gateNumber++;
            }
            travelled += Vector3.Distance(route[i], route[(i + 1) % route.Length]);
        }
        if (gateNumber != 16) throw new Exception("Mountain checkpoint placement failed.");
        var finish = new GameObject("Start finish markings").transform;
        finish.SetParent(root.transform, false);
        finish.position = route[0] + Vector3.up * 0.035f;
        finish.rotation = Quaternion.LookRotation(route[3] - route[0]);
        for (int color = 0; color < 2; color++)
        {
            string path = Output + "/Finish-" + color + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.color = color == 0 ? Color.white : Color.black;
            for (int tile = 0; tile < 28; tile++)
            {
                int x = tile % 14, z = tile / 14;
                if ((x + z) % 2 != color) continue;
                var square = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.Object.DestroyImmediate(square.GetComponent<Collider>());
                square.transform.SetParent(finish, false);
                square.transform.localPosition = new Vector3(x - 6.5f, 0, z * 0.5f - 0.25f);
                square.transform.localScale = new Vector3(1, 0.01f, 0.5f);
                square.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }
        Directory.CreateDirectory("Assets/Prefabs/Tracks");
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Tracks/Mountain.prefab");
        var definition = AssetDatabase.LoadAssetAtPath<TrackDefinition>("Assets/Tracks/Mountain.asset");
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<TrackDefinition>();
            AssetDatabase.CreateAsset(definition, "Assets/Tracks/Mountain.asset");
        }
        definition.id = "mountain"; definition.displayName = "Mountain Race Tracks";
        definition.description = $"Mountain road circuit\n{length / 1000f:F1} KM / elevation changes / wide asphalt";
        definition.credit = "Mountain race tracks: CRAFTED DIGITALS\nUnity Asset Store license";
        definition.recordKey = "RacingCar.Mountain.v1";
        definition.environmentPrefab = prefab;
        definition.startPosition = route[4] + Vector3.up * 0.8f;
        var direction = route[8] - route[4]; direction.y = 0;
        definition.startEuler = Quaternion.LookRotation(direction).eulerAngles;
        EditorUtility.SetDirty(definition); AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        var selection = UnityEngine.Object.FindAnyObjectByType<RaceTrackSelection>();
        selection.tracks = new[] { selection.tracks[0], definition };
        EditorSceneManager.MarkSceneDirty(selection.gameObject.scene);
        EditorSceneManager.SaveScene(selection.gameObject.scene);
        Debug.Log("MOUNTAIN_INSTALLED length " + length + " gates " + gateNumber);
    }
}
