using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RacingPreview
{
    [MenuItem("RacingCar/Render Downloaded Car Preview")]
    public static void RenderDownloadedCar()
    {
        Render();
        var rig = UnityEngine.Object.FindAnyObjectByType<RacerCameraRig>();
        var car = rig.car;
        rig.raceCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        rig.transform.position = car.transform.position + car.transform.rotation * new Vector3(-3.3f, 1.25f, 3.5f);
        rig.transform.LookAt(car.transform.position + Vector3.up * 0.05f);
        Export(rig.raceCamera, "Previews/downloaded-car-exterior.png");
    }

    public static void Render()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        var rig = UnityEngine.Object.FindFirstObjectByType<RacerCameraRig>();
        var car = rig.car;
        // Settle the real suspension so previews show tires touching the road.
        var previousSimulation = Physics.simulationMode;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            car.SetInput(0f, 0f, 0f);
            for (int i = 0; i < 100; i++) { car.StepPhysics(); Physics.Simulate(0.02f); }
            for (int i = 0; i < car.wheels.Length; i++)
            {
                car.wheels[i].GetWorldPose(out var position, out var rotation);
                car.wheelVisuals[i].SetPositionAndRotation(position, rotation);
            }
        }
        finally { Physics.simulationMode = previousSimulation; }
        var camera = rig.raceCamera;
        rig.poseDriver.enabled = false;
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity;
        var display = UnityEngine.Object.FindFirstObjectByType<RaceDashboard>().cockpitText;
        display.text = "0 <size=55%>KM/H</size>  <color=#FFBF60>1</color>\n<size=55%>900 RPM   LAP 1\n00:00.00  BEST --:--.--\nGATE 2/8</size>";
        display.ForceMeshUpdate();
        foreach (var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None)) label.ForceMeshUpdate();
        Directory.CreateDirectory("Previews");

        rig.transform.position = car.transform.position + new Vector3(0f, 3.5f, -7f);
        rig.transform.LookAt(car.transform.position + Vector3.up * 0.5f);
        Export(camera, "Previews/prototype-chase.png");
        rig.transform.SetPositionAndRotation(car.transform.position + rig.cockpitOffset, Quaternion.identity);
        camera.transform.localRotation = Quaternion.Euler(rig.desktopRestPitch, 0f, 0f);
        Export(camera, "Previews/prototype-cockpit.png");
        Debug.Log("RACINGCAR_PREVIEW_OK");
    }

    static void Export(Camera camera, string path)
    {
        var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        target.Create();
        var previous = RenderTexture.active;
        var image = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
        try
        {
            // Warm the pipeline before exporting; the first request initializes material and lighting state.
            for (int i = 0; i < 2; i++)
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
