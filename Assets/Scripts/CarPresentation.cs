using UnityEngine;

public sealed class CarPresentation : MonoBehaviour
{
    public ArcadeCar car;
    public Transform steeringWheel;
    public Transform rpmNeedle;
    public Renderer[] brakeLights;
    Quaternion steeringRest, needleRest;
    MaterialPropertyBlock lightProperties;

    void Awake()
    {
        steeringRest = steeringWheel.localRotation;
        if (rpmNeedle != null) needleRest = rpmNeedle.localRotation;
        lightProperties = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        steeringWheel.localRotation = steeringRest * Quaternion.Euler(0f, 0f, -car.Steering * 110f);
        if (rpmNeedle != null) rpmNeedle.localRotation = needleRest * Quaternion.Euler(0f, 0f, Mathf.Lerp(125f, -125f, car.EngineRpm / 8000f));
        Color emission = new Color(1f, 0.025f, 0.01f) * (car.Braking > 0.05f ? 3f : 0.15f);
        lightProperties.SetColor("_EmissionColor", emission);
        foreach (var light in brakeLights) light.SetPropertyBlock(lightProperties);
    }
}
