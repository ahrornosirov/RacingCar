using UnityEngine;

[DefaultExecutionOrder(-30)]
public sealed class RaceTrackSelection : MonoBehaviour
{
    public const string SelectionKey = "RacingCar.SelectedTrack";
    public TrackDefinition[] tracks;
    public GameObject circuitEnvironment, circuitCheckpoints;
    public RaceSession race;
    public RaceGarage garage;
    public RacerCameraRig view;
    public int PreviewIndex { get; private set; } = -1;
    public int SelectedIndex { get; private set; }
    public TrackDefinition Current => PreviewIndex >= 0 ? tracks[PreviewIndex] : tracks[0];
    GameObject loadedEnvironment;

    void Start()
    {
        int saved = System.Array.FindIndex(tracks, t => t.id == PlayerPrefs.GetString(SelectionKey));
        SelectedIndex = saved >= 0 ? saved : 0;
        if (!Preview(SelectedIndex)) { SelectedIndex = 0; Preview(0); }
    }

    public bool Preview(int index)
    {
        if (race.CurrentPhase != RaceSession.Phase.Ready || index < 0 || index >= tracks.Length) return false;
        if (PreviewIndex == index) return true;
        var definition = tracks[index];
        if (index > 0 && definition.environmentPrefab == null) return false;
        var checkpointSource = index == 0 ? circuitCheckpoints : definition.environmentPrefab;
        var sourceCheckpoints = checkpointSource.GetComponentsInChildren<RaceCheckpoint>(true);
        if (sourceCheckpoints.Length < 2) return false;
        var seen = new bool[sourceCheckpoints.Length];
        foreach (var checkpoint in sourceCheckpoints)
        {
            if (checkpoint.index < 0 || checkpoint.index >= seen.Length || seen[checkpoint.index]) return false;
            seen[checkpoint.index] = true;
        }
        if (loadedEnvironment != null)
        {
            loadedEnvironment.SetActive(false);
            Destroy(loadedEnvironment);
        }
        circuitEnvironment.SetActive(index == 0);
        circuitCheckpoints.SetActive(index == 0);
        if (index > 0)
        {
            loadedEnvironment = Instantiate(definition.environmentPrefab);
            loadedEnvironment.SetActive(true);
        }
        else loadedEnvironment = null;
        var checkpointRoot = index == 0 ? circuitCheckpoints : loadedEnvironment;
        var checkpoints = checkpointRoot.GetComponentsInChildren<RaceCheckpoint>(true);
        foreach (var checkpoint in checkpoints) checkpoint.race = race;
        race.checkpointCount = checkpoints.Length;
        race.startPosition = definition.startPosition;
        race.startEuler = definition.startEuler;
        PreviewIndex = index;
        garage.SetTrackRecordKey(definition.recordKey);
        Physics.SyncTransforms();
        race.ReturnHome();
        view.SetShowcase(true);
        return true;
    }

    public void Browse(int direction) => Preview((PreviewIndex + direction + tracks.Length) % tracks.Length);
    public void Confirm()
    {
        if (race.CurrentPhase != RaceSession.Phase.Ready) return;
        SelectedIndex = PreviewIndex;
        PlayerPrefs.SetString(SelectionKey, Current.id);
        PlayerPrefs.Save();
    }
    public void Cancel() => Preview(SelectedIndex);
}
