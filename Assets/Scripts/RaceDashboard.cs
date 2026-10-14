using TMPro;
using UnityEngine;
using UnityEngine.XR;

public sealed class RaceDashboard : MonoBehaviour
{
    public RaceSession race;
    public TMP_Text cockpitText;
    public AudioSource engine;
    public RaceMenu menu;
    public RacerCameraRig view;
    GUIStyle titleStyle, bodyStyle;

    void Start()
    {
        // A small procedural engine loop avoids external audio dependencies in the prototype.
        const int sampleRate = 22050;
        float[] samples = new float[sampleRate];
        for (int i = 0; i < samples.Length; i++)
        {
            float phase = i * 2f * Mathf.PI * 60f / sampleRate;
            samples[i] = (Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2f) + 0.15f * Mathf.Sin(phase * 4f)) * 0.15f;
        }
        engine.clip = AudioClip.Create("Prototype engine", samples.Length, 1, sampleRate, false);
        engine.clip.SetData(samples, 0);
        engine.loop = true;
        engine.Play();
    }

    void Update()
    {
        engine.pitch = 0.65f + race.car.EngineRpm / 4500f;
        engine.volume = race.Paused ? 0f : 0.18f + Mathf.Abs(race.Throttle) * 0.12f;
        string gear = race.car.Gear < 0 ? "R" : race.car.Gear.ToString();
        cockpitText.text = $"{race.car.SpeedKph:0} <size=55%>KM/H</size>  <color=#FFBF60>{gear}</color>\n<size=55%>{race.car.EngineRpm:0} RPM   LAP {race.LapNumber}/{race.LapTarget}\n{FormatTime(race.LapTime)}  BEST {FormatTime(race.BestLap)}\n"
            + (race.CurrentPhase == RaceSession.Phase.Countdown ? "GET READY" : race.CurrentPhase == RaceSession.Phase.Finished ? "RACE COMPLETE" : race.Paused ? (XRSettings.isDeviceActive ? "PAUSED · X to resume" : "PAUSED · P to resume") : $"GATE {race.NextCheckpoint + 1}/{race.checkpointCount}") + "</size>";
    }

    public static string FormatTime(float value)
    {
        if (value <= 0f) return "--:--.--";
        int hundredths = Mathf.RoundToInt(value * 100f);
        return $"{hundredths / 6000:00}:{hundredths / 100 % 60:00}.{hundredths % 100:00}";
    }

    void OnGUI()
    {
        if (XRSettings.isDeviceActive || (menu != null && menu.IsOpen)) return;
        titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        bodyStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 17, normal = { textColor = new Color(0.8f, 0.9f, 1f) } };
        GUI.Box(new Rect(20, 20, 400, 68), GUIContent.none);
        GUI.Label(new Rect(34, 28, 380, 30), $"{race.car.SpeedKph:0} KM/H  /  " + (view == null || view.cockpit ? "FIRST PERSON" : "THIRD PERSON"), bodyStyle);
        GUI.Label(new Rect(34, 57, 380, 26), race.Paused ? "PAUSED — P to resume" : $"LAP {race.LapNumber}/{race.LapTarget}   {FormatTime(race.LapTime)}   GATE {race.NextCheckpoint + 1}/{race.checkpointCount}", bodyStyle);
        GUI.Box(new Rect(20, Screen.height - 62, Mathf.Min(Screen.width - 40, 1100), 42), GUIContent.none);
        GUI.Label(new Rect(32, Screen.height - 55, Screen.width - 60, 32), "W/S throttle/brake · A/D steer · C camera · R reset · P/Esc menu · Right mouse look/orbit · Home recenter", bodyStyle);
    }

    void OnDestroy()
    {
        if (engine != null && engine.clip != null) Destroy(engine.clip);
    }
}
