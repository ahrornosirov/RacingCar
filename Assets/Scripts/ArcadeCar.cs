using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class ArcadeCar : MonoBehaviour
{
    public VehicleDefinition profile;
    public WheelCollider[] wheels;
    public Transform[] wheelVisuals;
    public float motorTorque = 900f;
    public float brakeTorque = 2800f;
    public float maxSpeed = 38f;
    public float maxSteerAngle = 29f;
    public Rigidbody Body { get; private set; }
    public float SpeedKph => Body == null ? 0f : Body.linearVelocity.magnitude * 3.6f;
    public float Throttle { get; private set; }
    public bool AssistedSteering => profile != null && profile.assistedSteering;
    public float Steering => AssistedSteering ? appliedSteer * SteeringLockFraction(SpeedKph) : steer;
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
    float steer, brake, appliedSteer;
    bool handbrake, parkedPreview;

    void Awake() => InitializeBody();

    void InitializeBody()
    {
        Body = GetComponent<Rigidbody>();
        Body.centerOfMass = profile != null ? profile.centerOfMass : new Vector3(0f, -0.3f, 0.1f);
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
        if (AssistedSteering)
        {
            // Unwind before building steering in the opposite direction. Small key taps
            // stay small; holding the key still reaches full available steering lock.
            bool reversing = appliedSteer * steer < 0f;
            bool unwinding = reversing || Mathf.Abs(steer) < Mathf.Abs(appliedSteer);
            float rate = unwinding ? profile.steeringReturn : profile.steeringBuildUp;
            if (!unwinding) rate *= Mathf.Lerp(1f, 0.65f, Mathf.Clamp01(speed * 3.6f / 100f));
            appliedSteer = Mathf.MoveTowards(appliedSteer, reversing ? 0f : steer, Mathf.Max(0.1f, rate) * Time.fixedDeltaTime);
        }
        else
            appliedSteer = profile == null ? steer : Mathf.MoveTowards(appliedSteer, steer, profile.steeringResponse * Time.fixedDeltaTime);
        Body.centerOfMass = profile != null ? profile.centerOfMass : new Vector3(0f, -0.3f, 0.1f);
        float angle = appliedSteer * (AssistedSteering ? maxSteerAngle * SteeringLockFraction(speed * 3.6f)
            : Mathf.Lerp(maxSteerAngle, 10f, Mathf.Clamp01(speed / maxSpeed)));
        float forwardSpeed = Vector3.Dot(Body.linearVelocity, transform.forward);
        bool opposingMotion = Mathf.Abs(forwardSpeed) > 1f && Throttle * forwardSpeed < 0f;
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].steerAngle = i < 2 ? angle : 0f;
            bool driven = profile == null || profile.drivetrain == VehicleDefinition.Drive.Rear ? i >= 2
                : profile.drivetrain == VehicleDefinition.Drive.Front ? i < 2 : true;
            float limit = Throttle < 0f && profile != null ? profile.reverseSpeedKph / 3.6f : maxSpeed;
            float curve = profile == null ? 1f : Mathf.Max(0f, profile.torqueBySpeed.Evaluate(speed / maxSpeed));
            float distribution = profile != null && profile.drivetrain == VehicleDefinition.Drive.All ? 0.5f : 1f;
            wheels[i].motorTorque = driven && speed < limit && !opposingMotion ? Throttle * motorTorque * curve * distribution : 0f;
            float bias = profile == null ? 1f : (i < 2 ? profile.frontBrakeBias : 1f - profile.frontBrakeBias) * 2f;
            float limiterBrake = profile != null && speed > limit + 0.5f && Mathf.Abs(Throttle) > 0.01f ? 0.3f : 0f;
            wheels[i].brakeTorque = Mathf.Max(Mathf.Max(brake, limiterBrake), opposingMotion ? Mathf.Abs(Throttle) : 0f) * brakeTorque * bias
                + (handbrake && i >= 2 ? brakeTorque : 0f);
        }
        Body.AddForce(-transform.up * speed * speed * (profile == null ? 1.2f : profile.downforce));
    }

    float SteeringLockFraction(float speedKph) => profile.steeringBySpeed != null
        ? Mathf.Clamp01(profile.steeringBySpeed.Evaluate(speedKph)) : 1f;

    void LateUpdate()
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].GetWorldPose(out var position, out var rotation);
            if (parkedPreview) { position = wheels[i].transform.position; rotation = transform.rotation; }
            wheelVisuals[i].SetPositionAndRotation(position, rotation);
        }
    }

    public void PlaceParkedPreview()
    {
        // Menus freeze physics. Position the tires on the road instead of showing the airborne race spawn.
        Physics.SyncTransforms();
        float gap = float.PositiveInfinity;
        foreach (var wheel in wheels)
        {
            var hits = Physics.RaycastAll(wheel.transform.position + Vector3.up * 2f, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            foreach (var hit in hits)
                if (!hit.transform.IsChildOf(transform) && hit.distance < nearest)
                    nearest = hit.distance;
            if (!float.IsInfinity(nearest)) gap = Mathf.Min(gap, nearest - 2f - wheel.radius);
        }
        if (!float.IsInfinity(gap)) ResetTo(transform.position - Vector3.up * gap, transform.rotation);
        parkedPreview = true;
    }

    public void ResetTo(Vector3 position, Quaternion rotation)
    {
        if (Body == null) InitializeBody();
        SetInput(0f, 0f, 0f);
        appliedSteer = 0f;
        foreach (var wheel in wheels) wheel.steerAngle = 0f;
        parkedPreview = false;
        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        Body.position = position;
        Body.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
        Body.WakeUp();
        ResetVersion++;
    }
}
