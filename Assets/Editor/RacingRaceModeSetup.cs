using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RacingRaceModeSetup
{
    [MenuItem("RacingCar/Install Three Lap Race Mode")]
    public static void Apply()
    {
        if (SceneManager.GetActiveScene().isDirty) throw new System.InvalidOperationException("Save the scene before installing race mode.");
        EditorSceneManager.OpenScene(RacingPrototypeBuilder.ScenePath);
        Directory.CreateDirectory("Backups/BeforeRaceMode");
        if (!File.Exists("Backups/BeforeRaceMode/RacingPrototype.unity")) File.Copy(RacingPrototypeBuilder.ScenePath, "Backups/BeforeRaceMode/RacingPrototype.unity");
        var menu = Object.FindAnyObjectByType<RaceMenu>();
        var race = menu.race;
        race.lapsToFinish = 3; race.countdownSeconds = 3;
        var font = menu.help.font;
        var textMaterial = menu.help.fontSharedMaterial;
        if (menu.resultDetails == null)
            menu.resultDetails = Label("Race results", menu.card, new Vector2(540, 365), new Vector2(0, -130), 25, font, textMaterial);
        menu.resultDetails.enableAutoSizing = true; menu.resultDetails.fontSizeMin = 20; menu.resultDetails.fontSizeMax = 25;
        menu.resultDetails.alignment = TextAlignmentOptions.TopLeft;
        menu.resultDetails.gameObject.SetActive(false);
        menu.summary.rectTransform.sizeDelta = new Vector2(540, 112);
        menu.summary.text = "CIRCUIT 01  /  3 LAPS\nBEST LAP   --:--.--\nBEST RACE   --:--.--";
        var signals = Object.FindAnyObjectByType<RaceCountdownUI>();
        if (signals == null) signals = race.gameObject.AddComponent<RaceCountdownUI>();
        signals.race = race; signals.menu = menu; signals.view = menu.view;
        if (signals.canvas == null)
        {
            var root = new GameObject("Countdown UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            signals.canvas = root.GetComponent<Canvas>(); signals.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            signals.canvas.sortingOrder = 95; signals.canvas.worldCamera = menu.view.raceCamera;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            var panel = new GameObject("Countdown panel", typeof(RectTransform), typeof(Image));
            var rect = panel.GetComponent<RectTransform>(); rect.SetParent(root.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(600, 320);
            var image = panel.GetComponent<Image>(); image.material = menu.card.GetComponent<Image>().material;
            image.color = new Color(0.035f, 0.055f, 0.075f, 0.94f); image.raycastTarget = false;
            signals.caption = Label("Countdown caption", rect, new Vector2(560, 40), new Vector2(0, -24), 24, font, textMaterial);
            signals.number = Label("Countdown number", rect, new Vector2(560, 180), new Vector2(0, -60), 145, font, textMaterial);
            signals.number.color = new Color(1, 0.60f, 0.18f); signals.number.fontStyle = FontStyles.Bold;
            signals.detail = Label("Race distance", rect, new Vector2(560, 40), new Vector2(0, -258), 24, font, textMaterial);
            signals.caption.text = "GET READY"; signals.number.text = "3"; signals.detail.text = "CIRCUIT 01  /  3 LAPS";
            root.SetActive(false);
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        RacingMenuSetup.Validate(); RacingMenuSetup.RenderHome();
        Debug.Log("RACINGCAR_THREE_LAP_SETUP_OK");
    }

    static TMP_Text Label(string name, Transform parent, Vector2 size, Vector2 position, float fontSize, TMP_FontAsset font, Material material)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1); rect.sizeDelta = size; rect.anchoredPosition = position;
        var text = go.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSharedMaterial = material;
        text.fontSize = fontSize; text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return text;
    }
}
