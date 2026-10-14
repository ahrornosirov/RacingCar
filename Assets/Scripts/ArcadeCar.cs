using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class ArcadeCar : MonoBehaviour
{
    public WheelCollider[] wheels;
    public Transform[] wheelVisuals;
    public float motorTorque = 900f;
    public float brakeTorque = 2800f;
    public float maxSpeed = 38f;
    public float maxSteerAngle = 29f;
    public Rigidbody Body { get; private set; }
    public float SpeedKph => Body == null ? 0f : Body.linearVelocity.magnitude * 3.6f;
    public float Throttle { get; private set; }
    public float Steering => steer;
    public float Braking => brake;
    public int Gear => Throttle < 0f ? -1 : Mathf.Clamp(1 + (int)(SpeedKph / 27f), 1, 5);
    // A virtual automatic drivetrain for instruments and sound; wheel contact drives RPM.
    public float EngineRpm
    {
        get
        {
            if (wheels == null || wheels.Length < 4) return 900f;
            float wheelRpm = Mathf.Abs((wheels[2].rpm + wheels[3].rpm) * 0.5f);
            float ratio = Gear <= 1 ? 3.2f : Gear == 2 ? 2.2f : Gear == 3 ? 1.6f : Gear == 4 ? 1.2f : 0.95f;
            return Mathf.Clamp(900f + wheelRpm * ratio * 3.8f + Mathf.Abs(Throttle) * 600f, 900f, 7500f);
        }
    }
    public int ResetVersion { get; private set; }
    float steer, brake;
    bool handbrake;

    void Awake() => InitializeBody();

    void InitializeBody()
    {
        Body = GetComponent<Rigidbody>();
        Body.centerOfMass = new Vector3(0f, -0.3f, 0.1f);
    }

    public void SetInput(float acceleration, float steering, float braking, bool parkingBrake = false)
    {
        Throttle = Mathf.Clamp(acceleration, -1f, 1f);
        steer = Mathf.Clamp(steering, -1f, 1f);
        brake = Mathf.Clamp01(braking);
        handbrake = parkingBrake;
    }

    void FixedUpdate() => StepPhysics();

    // Also used by the editor physics check, which advances the actual physics scene manually.
    public void StepPhysics()
    {
        if (Body == null) InitializeBody();
        float speed = Body.linearVelocity.magnitude;
        float angle = steer * Mathf.Lerp(maxSteerAngle, 10f, Mathf.Clamp01(speed / maxSpeed));
        float forwardSpeed = Vector3.Dot(Body.linearVelocity, transform.forward);
        bool opposingMotion = Mathf.Abs(forwardSpeed) > 1f && Throttle * forwardSpeed < 0f;
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].steerAngle = i < 2 ? angle : 0f;
            wheels[i].motorTorque = i >= 2 && speed < maxSpeed && !opposingMotion ? Throttle * motorTorque : 0f;
            wheels[i].brakeTorque = Mathf.Max(brake, opposingMotion ? Mathf.Abs(Throttle) : 0f) * brakeTorque
                + (handbrake && i >= 2 ? brakeTorque : 0f);
        }
        Body.AddForce(-transform.up * speed * speed * 1.2f);
    }

    void LateUpdate()
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].GetWorldPose(out var position, out var rotation);
            wheelVisuals[i].SetPositionAndRotation(position, rotation);
        }
    }

    public void ResetTo(Vector3 position, Quaternion rotation)
    {
        if (Body == null) InitializeBody();
        SetInput(0f, 0f, 0f);
        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        Body.position = position;
        Body.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
        Body.WakeUp();
        ResetVersion++;
    }
}
