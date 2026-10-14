using UnityEngine;

public sealed class RaceSession : MonoBehaviour
{
    public ArcadeCar car;
    public int checkpointCount = 8;
    public Vector3 startPosition;
    public Vector3 startEuler;
    public int CompletedLaps { get; private set; }
    public int NextCheckpoint { get; private set; } = 1;
    public float LapTime { get; private set; }
    public float LastLap { get; private set; }
    public float BestLap { get; private set; }
    public bool Paused { get; private set; }
    public float Throttle { get; set; }
    public float Steering { get; set; }
    public float Brake { get; set; }
    public bool Handbrake { get; set; }

    void Update()
    {
        if (!Paused) LapTime += Time.deltaTime;
        car.SetInput(Paused ? 0f : Throttle, Steering, Paused ? 1f : Brake, Handbrake);
    }

    public void PassCheckpoint(int index, Vector3 forward)
    {
        if (Paused || index != NextCheckpoint || Vector3.Dot(car.Body.linearVelocity, forward) < 0.5f) return;
        if (index == 0)
        {
            LastLap = LapTime;
            if (BestLap <= 0f || LastLap < BestLap) BestLap = LastLap;
            CompletedLaps++;
            LapTime = 0f;
        }
        NextCheckpoint = (index + 1) % checkpointCount;
    }

    public void Restart()
    {
        Throttle = Steering = Brake = 0f;
        Handbrake = false;
        car.ResetTo(startPosition, Quaternion.Euler(startEuler));
        NextCheckpoint = 1;
        LapTime = 0f;
    }

    public void TogglePause()
    {
        SetPaused(!Paused);
    }

    public void SetPaused(bool value)
    {
        Paused = value;
        Time.timeScale = Paused ? 0f : 1f;
        AudioListener.pause = Paused;
    }

    public void BeginNewRace()
    {
        CompletedLaps = 0;
        LastLap = 0f;
        Restart();
        SetPaused(false);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
