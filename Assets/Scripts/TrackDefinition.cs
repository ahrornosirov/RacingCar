using UnityEngine;

[CreateAssetMenu(menuName = "RacingCar/Track")]
public sealed class TrackDefinition : ScriptableObject
{
    public string id, displayName;
    [TextArea] public string description, credit;
    public string recordKey;
    [Tooltip("Null only for the original circuit already in the scene. Other maps contain their road, scenery and ordered RaceCheckpoint triggers.")]
    public GameObject environmentPrefab;
    public Vector3 startPosition, startEuler;
}
