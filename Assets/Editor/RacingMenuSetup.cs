using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RacingMenuSetup
{
    static readonly Color Navy = new Color(0.035f, 0.055f, 0.075f, 0.98f);
    static readonly Color Muted = new Color(0.60f, 0.69f, 0.73f);
    static readonly Color Amber = new Color(1f, 0.43f, 0.10f);
    static TMP_FontAsset font;

    [MenuItem("RacingCar/Install Race Menu")]
    public static void Apply()
    {
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the scene before installing the menu.");
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        if (UnityEngine.Object.FindAnyObjectByType<RaceMenu>() != null) throw new InvalidOperationException("Race menu is already installed.");
        Directory.CreateDirectory("Backups/BeforeMenu");
        File.Copy(RacingPrototypeBuilder.ScenePath, "Backups/BeforeMenu/RacingPrototype.unity", true);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        var race = UnityEngine.Object.FindAnyObjectByType<RaceSession>();
        var rig = UnityEngine.Object.FindAnyObjectByType<RacerCameraRig>();
        var menu = race.gameObject.AddComponent<RaceMenu>();
        menu.race = race; menu.view = rig;
        race.GetComponent<RaceDriverInput>().menu = menu;
        var dashboard = race.GetComponent<RaceDashboard>(); dashboard.menu = menu; dashboard.view = rig;

        var root = new GameObject("Race Menu UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        menu.canvas = root.GetComponent<Canvas>(); menu.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menu.canvas.worldCamera = rig.raceCamera; menu.canvas.planeDistance = 1f; menu.canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        menu.desktopBrand = Rect("Desktop Branding", root.transform, new Vector2(900, 450), new Vector2(0, 1), new Vector2(0, 1), new Vector2(85, -78)).gameObject;
        Label("Series", menu.desktopBrand.transform, "QUEST RACING  /  CIRCUIT 01", 24, new Vector2(820, 35), 0, 0, Muted, TextAlignmentOptions.Left);
        Label("Brand", menu.desktopBrand.transform, "RACINGCAR", 90, new Vector2(840, 120), 0, 48, Color.white, TextAlignmentOptions.Left);
        Label("Tagline", menu.desktopBrand.transform, "EVERY LAP COUNTS.", 27, new Vector2(800, 45), 0, 178, Amber, TextAlignmentOptions.Left);

        menu.card = Rect("Menu Card", root.transform, new Vector2(620, 780), new Vector2(0.76f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        var background = menu.card.gameObject.AddComponent<Image>(); background.color = Navy;
        var line = Rect("Accent", menu.card, new Vector2(620, 4), new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero).gameObject.AddComponent<Image>(); line.color = Amber; line.raycastTarget = false;
        menu.title = Label("Title", menu.card, "READY TO RACE?", 36, new Vector2(540, 48), 0, 30, Color.white);
        menu.subtitle = Label("Subtitle", menu.card, "Choose your view and take the starting line.", 20, new Vector2(540, 36), 0, 80, Muted);
        menu.cameraLabel = Label("Camera label", menu.card, "CAMERA  /  FIRST PERSON", 15, new Vector2(540, 18), 0, 180, Muted);
        menu.play = Button("Play", menu.card, "START RACE", 118, true);
        menu.firstPerson = Button("First person", menu.card, "FIRST PERSON   /   SELECTED", 200);
        menu.thirdPerson = Button("Third person", menu.card, "THIRD PERSON", 274);
        menu.restart = Button("Restart lap", menu.card, "RESTART LAP", 348);
        menu.controls = Button("Controls", menu.card, "CONTROLS", 348);
        menu.mainMenu = Button("Main menu", menu.card, "MAIN MENU", 496);
        menu.quit = Button("Quit", menu.card, "QUIT GAME", 422);
        menu.back = Button("Back", menu.card, "BACK", 580, true);
        menu.help = Label("Controls details", menu.card, "", 20, new Vector2(540, 430), 0, 126, new Color(0.84f, 0.89f, 0.91f), TextAlignmentOptions.TopLeft);
        menu.help.enableAutoSizing = true; menu.help.fontSizeMin = 16; menu.help.fontSizeMax = 20;
        menu.summary = Label("Race summary", menu.card, "CIRCUIT 01\n8 CHECKPOINTS  /  TIMED LAPS", 22, new Vector2(540, 85), 0, 540, Muted);
        menu.footer = Label("Footer", menu.card, "Mouse / arrows + Enter   |   Quest: left stick + A\nC / Y changes the camera. P / X pauses the race.", 17, new Vector2(540, 55), 0, 692, Muted);
        menu.footer.enableAutoSizing = true; menu.footer.fontSizeMin = 12; menu.footer.fontSizeMax = 17;
        menu.restart.gameObject.SetActive(false); menu.mainMenu.gameObject.SetActive(false); menu.back.gameObject.SetActive(false); menu.help.gameObject.SetActive(false);
        BuildChaseHud(menu, rig);
        BuildEventSystem();

        SettleCar(race.car);
        rig.showcase = true; rig.cockpit = true;
        rig.transform.position = race.car.transform.position + new Vector3(-3.3f, 1.25f, 3.5f);
        rig.transform.LookAt(race.car.transform.position - Vector3.right * 1.05f);
        rig.raceCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Validate();
        RenderHome();
        Debug.Log("RACINGCAR_MENU_SETUP_OK");
    }

    static void BuildChaseHud(RaceMenu menu, RacerCameraRig rig)
    {
        var go = new GameObject("Quest Chase HUD", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(rig.transform, false);
        var rectangle = (RectTransform)go.transform; rectangle.sizeDelta = new Vector2(720, 150);
        rectangle.localPosition = new Vector3(0, -0.48f, 1.7f); rectangle.localScale = Vector3.one * 0.0012f;
        menu.chaseHud = go.GetComponent<Canvas>(); menu.chaseHud.renderMode = RenderMode.WorldSpace; menu.chaseHud.worldCamera = rig.raceCamera;
        menu.chaseHud.sortingOrder = 90;
        var image = go.AddComponent<Image>(); image.color = Navy; image.raycastTarget = false;
        menu.chaseHudText = Label("Chase telemetry", go.transform, "0 KM/H    LAP 1\n00:00.00    GATE 2/8", 38, new Vector2(690, 120), 0, 15, Color.white);
        go.SetActive(false);
    }

    static void BuildEventSystem()
    {
        Directory.CreateDirectory("Assets/Input");
        var original = new DefaultInputActions();
        // Quest A/B are handled once by RaceMenu's native controller path.
        original.UI.Submit.ChangeBinding(0).WithPath("<Keyboard>/enter");
        original.UI.Submit.AddBinding("<Keyboard>/numpadEnter");
        original.UI.Submit.AddBinding("<Keyboard>/space");
        original.UI.Submit.AddBinding("<Gamepad>/buttonSouth");
        original.UI.Cancel.ChangeBinding(0).WithPath("<Keyboard>/escape");
        original.UI.Cancel.AddBinding("<Gamepad>/buttonEast");
        File.WriteAllText("Assets/Input/Menu.inputactions", original.asset.ToJson());
        original.Dispose();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Input/Menu.inputactions");
        var eventSystem = new GameObject("Menu Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var module = eventSystem.GetComponent<InputSystemUIInputModule>(); module.UnassignActions(); module.actionsAsset = asset;
        module.move = Reference(asset, "Navigate"); module.submit = Reference(asset, "Submit"); module.cancel = Reference(asset, "Cancel");
        module.point = Reference(asset, "Point"); module.leftClick = Reference(asset, "Click"); module.rightClick = Reference(asset, "RightClick");
        module.middleClick = Reference(asset, "MiddleClick"); module.scrollWheel = Reference(asset, "ScrollWheel");
    }

    static InputActionReference Reference(InputActionAsset asset, string action)
    {
        var reference = InputActionReference.Create(asset.FindAction("UI/" + action, true));
        AssetDatabase.CreateAsset(reference, "Assets/Input/Menu" + action + ".asset");
        return reference;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }

    static TextMeshProUGUI Label(string name, Transform parent, string content, float size, Vector2 bounds, float x, float y, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var rect = Rect(name, parent, bounds, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(x, -y));
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font;
        text.text = content; text.fontSize = size; text.color = color; text.alignment = alignment; text.raycastTarget = false;
        return text;
    }

    static Button Button(string name, Transform parent, string content, float y, bool primary = false)
    {
        var rect = Rect(name, parent, new Vector2(540, 64), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -y));
        var image = rect.gameObject.AddComponent<Image>(); image.color = primary ? Amber : new Color(0.11f, 0.16f, 0.20f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f); colors.selectedColor = new Color(1.35f, 1.35f, 1.35f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f); colors.fadeDuration = 0.08f; button.colors = colors;
        Label("Label", rect, content, 23, new Vector2(500, 64), 0, 0, Color.white, TextAlignmentOptions.MidlineLeft);
        return button;
    }

    static void SettleCar(ArcadeCar car)
    {
        var previous = Physics.simulationMode;
        try
        {
            Physics.simulationMode = SimulationMode.Script; Physics.SyncTransforms(); car.SetInput(0, 0, 0);
            for (int i = 0; i < 100; i++) { car.StepPhysics(); Physics.Simulate(0.02f); }
            for (int i = 0; i < car.wheels.Length; i++) { car.wheels[i].GetWorldPose(out var p, out var q); car.wheelVisuals[i].SetPositionAndRotation(p, q); }
        }
        finally { Physics.simulationMode = previous; }
    }

    public static void RenderHome()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        Canvas.ForceUpdateCanvases();
        foreach (var text in UnityEngine.Object.FindObjectsByType<TMP_Text>()) text.ForceMeshUpdate();
        var menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>(); menu.view.poseDriver.enabled = false;
        RacingPreview.Export(menu.view.raceCamera, "Previews/main-menu.png");
    }

    [MenuItem("RacingCar/Finish Menu Layout")]
    public static void FinishLayout()
    {
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>();
        var ui = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/UI/MenuImages.mat");
        if (ui == null)
        {
            ui = new Material(Shader.Find("RacingCar/MenuUIOverlay"));
            AssetDatabase.CreateAsset(ui, "Assets/Art/UI/MenuImages.mat");
        }
        var overlay = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/UI/MenuText.mat");
        if (overlay == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            overlay = new Material(font.material) { shader = Shader.Find("TextMeshPro/Mobile/Distance Field Overlay") };
            AssetDatabase.CreateAsset(overlay, "Assets/Art/UI/MenuText.mat");
        }
        foreach (var image in menu.canvas.GetComponentsInChildren<Image>(true)) image.material = ui;
        foreach (var text in menu.canvas.GetComponentsInChildren<TMP_Text>(true)) text.fontSharedMaterial = overlay;
        menu.chaseHud.GetComponent<Image>().material = ui;
        menu.chaseHudText.fontSharedMaterial = overlay;
        menu.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menu.card.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f);
        var brand = menu.desktopBrand.GetComponentsInChildren<TMP_Text>();
        brand[0].color = new Color(0.08f, 0.13f, 0.17f);
        brand[1].fontStyle = FontStyles.Bold;
        var rig = menu.view;
        rig.transform.position = menu.race.car.transform.position + new Vector3(-3.3f, 1.25f, 3.5f);
        rig.transform.LookAt(menu.race.car.transform.position - Vector3.right * 1.05f);
        rig.raceCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets(); Validate(); RenderHome();
    }

    public static void Validate()
    {
        var menu = UnityEngine.Object.FindAnyObjectByType<RaceMenu>();
        if (menu == null || menu.canvas == null || menu.card == null || menu.chaseHud == null || menu.summary == null
            || menu.race.GetComponent<RaceDriverInput>().menu != menu || menu.race.GetComponent<RaceDashboard>().menu != menu)
            throw new InvalidOperationException("Menu scene references are incomplete.");
        foreach (var button in new[] { menu.play, menu.firstPerson, menu.thirdPerson, menu.restart, menu.controls, menu.mainMenu, menu.quit, menu.back })
            if (button == null || button.GetComponentInChildren<TMP_Text>() == null) throw new InvalidOperationException("A menu button is incomplete.");
        var module = UnityEngine.Object.FindAnyObjectByType<InputSystemUIInputModule>();
        if (module == null || module.move.action == null || module.point.action == null || module.submit.action == null || module.leftClick.action == null)
            throw new InvalidOperationException("Menu input actions are missing.");
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/menu-setup-checks.txt", "PASS: menu buttons, scene references, chase HUD and persisted UI input actions.\n");
        Debug.Log("RACINGCAR_MENU_VALIDATION_OK");
    }
}
