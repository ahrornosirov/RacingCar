using UnityEngine;

[DefaultExecutionOrder(-20)]
public sealed class RaceGarage : MonoBehaviour
{
    public const string SelectionKey = "RacingCar.SelectedVehicle";
    public VehicleDefinition[] vehicles;
    public RaceSession race;
    public RacerCameraRig view;
    public RaceDashboard dashboard;
    public int PreviewIndex { get; private set; } = -1;
    public int SelectedIndex { get; private set; }
    public VehicleDefinition Current => vehicles[PreviewIndex];
    string baseRecordKey;

    void Start()
    {
        if (string.IsNullOrEmpty(baseRecordKey)) baseRecordKey = race.recordKey;
        string saved = PlayerPrefs.GetString(SelectionKey);
        SelectedIndex = System.Array.FindIndex(vehicles, v => v.id == saved);
        if (SelectedIndex < 0) SelectedIndex = 0;
        PreviewIndex = -1;
        Preview(SelectedIndex);
    }

    public bool Preview(int index)
    {
        // Vehicle changes are only legal before a race; pause cannot change its car or records.
        if (race.CurrentPhase != RaceSession.Phase.Ready || index < 0 || index >= vehicles.Length) return false;
        if (PreviewIndex == index) return true;
        var definition = vehicles[index];
        var old = race.car;
        var oldAudio = dashboard.engine;
        var clip = oldAudio.clip;
        old.gameObject.SetActive(false);
        var car = Instantiate(definition.prefab, race.startPosition, Quaternion.Euler(race.startEuler));
        car.name = definition.displayName;
        definition.Apply(car);
        race.car = view.car = car;
        dashboard.cockpitText = car.GetComponent<VehicleCockpit>().display;
        dashboard.engine = car.GetComponent<AudioSource>();
        // The dashboard owns the procedural clip; carry it across vehicle swaps.
        if (clip != null)
        {
            oldAudio.clip = null;
            dashboard.engine.clip = clip; dashboard.engine.loop = true; dashboard.engine.Play();
        }
        view.cockpitOffset = definition.cockpitOffset;
        view.chaseOffset = definition.chaseOffset;
        view.desktopRestPitch = definition.cockpitPitch;
        view.ResetSeat();
        PreviewIndex = index;
        RefreshRecords();
        race.ReturnHome();
        Destroy(old.gameObject);
        return true;
    }

    public void Browse(int direction) => Preview((PreviewIndex + direction + vehicles.Length) % vehicles.Length);
    public void Confirm()
    {
        SelectedIndex = PreviewIndex;
        PlayerPrefs.SetString(SelectionKey, Current.id); PlayerPrefs.Save();
    }
    public void Cancel() => Preview(SelectedIndex);

    public void SetTrackRecordKey(string key)
    {
        baseRecordKey = key;
        RefreshRecords();
    }

    void RefreshRecords()
    {
        string id = PreviewIndex >= 0 ? Current.id : "concept";
        race.recordKey = id == "concept" ? baseRecordKey : baseRecordKey + "." + id;
        race.LoadRecords();
    }
}
