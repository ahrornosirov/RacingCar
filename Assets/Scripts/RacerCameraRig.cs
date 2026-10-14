using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.InputSystem;

public sealed class RacerCameraRig : MonoBehaviour
{
    public ArcadeCar car;
    public Camera raceCamera;
    public TrackedPoseDriver poseDriver;
    public Vector3 cockpitOffset = new Vector3(-0.3f, 0.65f, 0.05f);
    public bool cockpit = true;
    public float desktopRestPitch = 10f;
    public Vector3 chaseOffset = new Vector3(0f, 2.4f, -6.2f);
    public bool showcase;
    bool wasXR;
    bool snapNext = true;
    float lookYaw, lookPitch, seatHeight, orbitYaw, orbitPitch;
    readonly RaycastHit[] cameraHits = new RaycastHit[32];
    Vector3 headOrigin;
    float headYaw;

    void Start() => lookPitch = desktopRestPitch;

    public void ToggleDesktopView() => SetView(!cockpit);
    public void SetView(bool firstPerson)
    {
        cockpit = firstPerson;
        snapNext = true;
        Recenter();
    }
    public void SetShowcase(bool value) { showcase = value; snapNext = true; }

    public void Recenter()
    {
        headOrigin = raceCamera.transform.localPosition;
        headYaw = raceCamera.transform.localEulerAngles.y;
        lookYaw = 0f;
        lookPitch = desktopRestPitch;
        orbitYaw = orbitPitch = 0f;
    }

    void LateUpdate()
    {
        bool xr = XRSettings.isDeviceActive;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float adjustment = (keyboard.pageUpKey.isPressed ? 1f : 0f) - (keyboard.pageDownKey.isPressed ? 1f : 0f);
            seatHeight = Mathf.Clamp(seatHeight + adjustment * Time.unscaledDeltaTime * 0.12f, -0.12f, 0.18f);
            if (keyboard.homeKey.wasPressedThisFrame) Recenter();
        }
        if (poseDriver.enabled != xr) poseDriver.enabled = xr;
        if (xr != wasXR)
        {
            if (xr) Recenter();
            wasXR = xr;
        }
        var yaw = Quaternion.Euler(0f, car.transform.eulerAngles.y, 0f);
        if (xr)
        {
            // Both views retain HMD tracking. Chase view has a level horizon and no forced pitch.
            transform.rotation = yaw * Quaternion.Euler(0f, -headYaw, 0f);
            Vector3 offset = cockpit && !showcase ? cockpitOffset + Vector3.up * seatHeight : chaseOffset;
            Vector3 position = car.transform.position + yaw * offset;
            if (!cockpit || showcase) position = AvoidObstacles(position);
            transform.position = position - transform.rotation * headOrigin;
        }
        else if (showcase)
        {
            transform.position = car.transform.position + yaw * new Vector3(-3.3f, 1.25f, 3.5f);
            transform.rotation = Quaternion.LookRotation(car.transform.position + yaw * Vector3.right * 1.05f - transform.position);
            raceCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        else if (cockpit)
        {
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                lookYaw = Mathf.Clamp(lookYaw + delta.x * 0.12f, -110f, 110f);
                lookPitch = Mathf.Clamp(lookPitch - delta.y * 0.12f, -55f, 65f);
            }
            transform.SetPositionAndRotation(car.transform.position + yaw * (cockpitOffset + Vector3.up * seatHeight), yaw);
            raceCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(lookPitch, lookYaw, 0f));
        }
        else
        {
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                orbitYaw = Mathf.Clamp(orbitYaw + delta.x * 0.12f, -160f, 160f);
                orbitPitch = Mathf.Clamp(orbitPitch - delta.y * 0.12f, -12f, 22f);
            }
            Vector3 target = car.transform.position + yaw * Quaternion.Euler(orbitPitch, orbitYaw, 0f) * chaseOffset;
            target = AvoidObstacles(target);
            Vector3 position = snapNext ? target : Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
            transform.position = AvoidObstacles(position);
            transform.rotation = Quaternion.LookRotation(car.transform.position + Vector3.up * 0.1f - transform.position);
            raceCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        snapNext = false;
    }

    Vector3 AvoidObstacles(Vector3 desired)
    {
        Vector3 origin = car.transform.position + Vector3.up * 0.5f;
        Vector3 direction = desired - origin;
        float distance = direction.magnitude;
        if (distance < 0.01f) return desired;
        direction /= distance;
        int count = Physics.SphereCastNonAlloc(origin, 0.16f, direction, cameraHits, distance, ~0, QueryTriggerInteraction.Ignore);
        float nearest = distance;
        for (int i = 0; i < count; i++)
            if (!cameraHits[i].transform.IsChildOf(car.transform)) nearest = Mathf.Min(nearest, Mathf.Max(0.2f, cameraHits[i].distance - 0.1f));
        return origin + direction * nearest;
    }
}
