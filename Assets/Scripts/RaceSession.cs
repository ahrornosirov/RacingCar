using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RaceSession : MonoBehaviour
{
    public enum Phase { Ready, Countdown, Racing, Finished }
    public ArcadeCar car;
    public int checkpointCount = 8;
    [Min(1)] public int lapsToFinish = 3;
    [Min(0)] public float countdownSeconds = 3f;
    public string recordKey = "RacingCar.Circuit01.v1";
    public Vector3 startPosition;
    public Vector3 startEuler;
    public Phase CurrentPhase { get; private set; }
    public int CompletedLaps { get; private set; }
    public int LapNumber => Mathf.Min(CompletedLaps + 1, LapTarget);
    public int LapTarget => Mathf.Max(1, lapsToFinish);
    public int NextCheckpoint { get; private set; } = 1;
    public float LapTime { get; private set; }
    public float RaceTime { get; private set; }
    public float LastLap { get; private set; }
    public float BestLap { get; private set; }
    public float BestRace { get; private set; }
    public float CountdownRemaining { get; private set; }
    public bool ShowGo => CurrentPhase == Phase.Racing && goRemaining > 0f;
    public bool NewLapRecord { get; private set; }
    public bool NewRaceRecord { get; private set; }
    public int Resets { get; private set; }
    public bool Paused { get; private set; }
    public bool CanDrive => CurrentPhase == Phase.Racing && !Paused;
    public IReadOnlyList<float> LapTimes => lapTimes;
    public event Action Finished;
    public float Throttle { get; set; }
    public float Steering { get; set; }
    public float Brake { get; set; }
    public bool Handbrake { get; set; }
    readonly List<float> lapTimes = new List<float>();
    float goRemaining;

    void Awake() => LoadRecords();

    public void LoadRecords()
    {
        BestLap = ValidTime(PlayerPrefs.GetFloat(recordKey + ".BestLap"));
        BestRace = ValidTime(PlayerPrefs.GetFloat(recordKey + ".BestRace." + LapTarget));
    }

    static float ValidTime(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value) ? value : 0f;

    void Update()
    {
        if (!Paused)
        {
            if (CurrentPhase == Phase.Countdown)
            {
                CountdownRemaining = Mathf.Max(0f, CountdownRemaining - Time.deltaTime);
                if (CountdownRemaining <= 0f) { CurrentPhase = Phase.Racing; goRemaining = 0.8f; }
            }
            else if (CurrentPhase == Phase.Racing)
            {
                LapTime += Time.deltaTime;
                RaceTime += Time.deltaTime;
                goRemaining = Mathf.Max(0f, goRemaining - Time.deltaTime);
            }
        }
        car.SetInput(CanDrive ? Throttle : 0f, CanDrive ? Steering : 0f, CanDrive ? Brake : 1f, CanDrive && Handbrake);
    }

    public void PassCheckpoint(int index, Vector3 forward)
    {
        if (!CanDrive || index != NextCheckpoint || Vector3.Dot(car.Body.linearVelocity, forward) < 0.5f) return;
        if (index == 0)
        {
            LastLap = LapTime;
            lapTimes.Add(LastLap);
            if (ValidTime(LastLap) > 0f && (BestLap <= 0f || LastLap < BestLap))
            {
                BestLap = LastLap; NewLapRecord = true;
                PlayerPrefs.SetFloat(recordKey + ".BestLap", BestLap);
                PlayerPrefs.Save();
            }
            CompletedLaps++;
            LapTime = 0f;
            if (CompletedLaps >= LapTarget)
            {
                NewRaceRecord = ValidTime(RaceTime) > 0f && (BestRace <= 0f || RaceTime < BestRace);
                if (NewRaceRecord)
                {
                    BestRace = RaceTime;
                    PlayerPrefs.SetFloat(recordKey + ".BestRace." + LapTarget, BestRace);
                    PlayerPrefs.Save();
                }
                CurrentPhase = Phase.Finished;
                ClearInput();
                SetPaused(true);
                Finished?.Invoke();
                return;
            }
        }
        NextCheckpoint = (index + 1) % checkpointCount;
    }

    void ClearInput()
    {
        Throttle = Steering = Brake = 0f;
        Handbrake = false;
        car.SetInput(0f, 0f, 1f);
    }

    void ResetLap()
    {
        ClearInput();
        car.ResetTo(startPosition, Quaternion.Euler(startEuler));
        NextCheckpoint = 1;
        LapTime = 0f;
    }

    public void Restart()
    {
        if (CurrentPhase != Phase.Racing) return;
        Resets++;
        // Abandoned lap time stays in RaceTime, so resetting cannot improve a race record.
        ResetLap();
    }

    public void ReturnHome()
    {
        CurrentPhase = Phase.Ready;
        CountdownRemaining = goRemaining = 0f;
        ClearInput();
        car.ResetTo(startPosition, Quaternion.Euler(startEuler));
        SetPaused(true);
    }

    public void TogglePause() => SetPaused(!Paused);

    public void SetPaused(bool value)
    {
        Paused = value || CurrentPhase == Phase.Ready || CurrentPhase == Phase.Finished;
        Time.timeScale = Paused ? 0f : 1f;
        AudioListener.pause = Paused;
        if (Paused) ClearInput();
    }

    public void BeginNewRace()
    {
        CompletedLaps = 0;
        LastLap = RaceTime = 0f;
        NewLapRecord = NewRaceRecord = false;
        Resets = 0;
        lapTimes.Clear();
        ResetLap();
        CountdownRemaining = Mathf.Max(0f, countdownSeconds);
        CurrentPhase = CountdownRemaining > 0f ? Phase.Countdown : Phase.Racing;
        goRemaining = CountdownRemaining > 0f ? 0f : 0.8f;
        SetPaused(false);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
