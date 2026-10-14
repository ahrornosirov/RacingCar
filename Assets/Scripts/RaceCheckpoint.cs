using UnityEngine;

public sealed class RaceCheckpoint : MonoBehaviour
{
    public RaceSession race;
    public int index;

    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == race.car.Body)
            race.PassCheckpoint(index, transform.forward);
    }
}
