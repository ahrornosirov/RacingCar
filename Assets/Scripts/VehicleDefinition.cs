using UnityEngine;

[CreateAssetMenu(menuName = "RacingCar/Vehicle")]
public sealed class VehicleDefinition : ScriptableObject
{
    public string id, displayName, description;
    public string accelerationLabel = "Balanced acceleration", brakingLabel = "Balanced brakes";
    [TextArea] public string credit;
    public ArcadeCar prefab;
    public Vector3 cockpitOffset, chaseOffset = new Vector3(0, 2.4f, -6.2f);
    public float cockpitPitch = 8f;
    public float mass = 1200f, topSpeedKph = 180f, reverseSpeedKph = 25f;
    public float motorTorque = 650f, brakeTorque = 2800f, steerAngle = 29f;
    public enum Drive { Rear, Front, All }
    public Drive drivetrain;
    public Vector3 centerOfMass = new Vector3(0, -0.3f, 0.1f);
    public float grip = 1f, suspensionSpring = 35000f, suspensionDamper = 4500f;
    public float steeringResponse = 4f, frontBrakeBias = 0.6f, downforce = 1.2f;
    public AnimationCurve torqueBySpeed = AnimationCurve.Linear(0, 1, 1, 0.4f);

    [Header("Steering experiment")]
    [Tooltip("Disable to compare with the original steering. Enabled on the Porsche first.")]
    public bool assistedSteering;
    [Min(0.1f)] public float steeringBuildUp = 2.2f;
    [Min(0.1f)] public float steeringReturn = 3.5f;
    [Tooltip("Road speed in km/h versus fraction of low-speed steering lock; independent of top speed.")]
    public AnimationCurve steeringBySpeed = new AnimationCurve(
        new Keyframe(0, 1), new Keyframe(20, 1), new Keyframe(50, 0.4f),
        new Keyframe(100, 0.16f), new Keyframe(160, 0.075f), new Keyframe(300, 0.025f));

    public void Apply(ArcadeCar car)
    {
        car.profile = this;
        car.GetComponent<Rigidbody>().mass = mass;
        car.motorTorque = motorTorque; car.brakeTorque = brakeTorque;
        car.maxSpeed = topSpeedKph / 3.6f; car.maxSteerAngle = steerAngle;
        foreach (var wheel in car.wheels)
        {
            var spring = wheel.suspensionSpring;
            spring.spring = suspensionSpring; spring.damper = suspensionDamper;
            wheel.suspensionSpring = spring;
            // Apply absolute stiffness so browsing the Garage never compounds grip.
            var forward = wheel.forwardFriction; forward.stiffness = grip; wheel.forwardFriction = forward;
            var side = wheel.sidewaysFriction; side.stiffness = grip; wheel.sidewaysFriction = side;
        }
    }
}
