using UnityEngine;

// Deterministic wheel mechanics; tracking, UI and vehicle physics are separate consumers.
public sealed class HandWheelState
{
    public struct Hand
    {
        public bool tracked, closed;
        public Vector3 position; // Metres in the stationary wheel plane (not its rotating visual).
        public float pinch;
    }
    sealed class Grip { public bool held, ready; public float bearing; }
    readonly Grip left = new Grip(), right = new Grip();
    public float Angle { get; private set; }
    public float Steering { get; private set; }
    public float Throttle { get; private set; }
    public float Brake { get; private set; } = 0.65f;
    public bool LeftHeld => left.held;
    public bool RightHeld => right.held;
    public bool PedalsArmed { get; private set; }
    public const float MaxAngle = 110f;
    public float radius = 0.18f;

    public void Reset()
    {
        left.held = right.held = left.ready = right.ready = false;
        Angle = Steering = Throttle = 0; Brake = 0.65f; PedalsArmed = false;
    }

    public void DisarmPedals() { PedalsArmed = false; Throttle = 0; Brake = 0.65f; }

    public void Step(Hand l, Hand r, float dt, float speedKph, bool reverse, bool blocked)
    {
        dt = Mathf.Clamp(dt, 0, 0.05f);
        if (blocked) { Reset(); return; }
        float delta = 0; int count = 0;
        GripStep(left, l, ref delta, ref count);
        GripStep(right, r, ref delta, ref count);
        if (count > 0) Angle = Mathf.Clamp(Angle + delta / count, -MaxAngle, MaxAngle);
        else Angle = Mathf.MoveTowards(Angle, 0, 120f * dt);
        Steering = Mathf.Lerp(Steering, -Angle / MaxAngle, 1 - Mathf.Exp(-14f * dt));
        // Either lost hand disables pedals, even if the other hand can still steer.
        // A fresh release of both pinches is required after every interruption.
        if (!l.tracked || !r.tracked || !(left.held || right.held)) { DisarmPedals(); return; }
        if (!PedalsArmed)
        {
            if (l.pinch < 0.15f && r.pinch < 0.15f) PedalsArmed = true;
            Throttle = 0; Brake = 0.65f; return;
        }
        Brake = Mathf.InverseLerp(0.2f, 0.85f, l.pinch);
        float demand = Mathf.InverseLerp(0.2f, 0.85f, r.pinch) * 0.55f;
        // Gentle prototype speed cap. Full vehicle tuning is retained in controller mode.
        float limit = reverse ? 12f : 60f;
        if (speedKph >= limit) demand = 0;
        if (speedKph > limit + 2f) Brake = Mathf.Max(Brake, 0.3f);
        if (Brake > 0.05f) { Throttle = 0; return; }
        float target = reverse ? -demand : demand;
        Throttle = Mathf.MoveTowards(Throttle, target, (demand == 0 ? 5f : 0.7f) * dt);
    }

    void GripStep(Grip grip, Hand hand, ref float delta, ref int count)
    {
        if (!hand.tracked) { grip.held = grip.ready = false; return; }
        if (!hand.closed) { grip.held = false; grip.ready = true; return; }
        float r = new Vector2(hand.position.x, hand.position.y).magnitude;
        bool near = Mathf.Abs(r - radius) < (grip.held ? 0.14f : 0.09f)
            && Mathf.Abs(hand.position.z) < (grip.held ? 0.22f : 0.13f) && r > 0.055f;
        if (!near) { if (grip.held) grip.ready = false; grip.held = false; return; }
        float bearing = Mathf.Atan2(hand.position.y, hand.position.x) * Mathf.Rad2Deg;
        if (!grip.held)
        {
            if (!grip.ready) return;
            grip.held = true; grip.bearing = bearing;
        }
        float change = Mathf.DeltaAngle(grip.bearing, bearing);
        grip.bearing = bearing;
        // Reject tracking teleports instead of injecting a sudden full-lock input.
        if (Mathf.Abs(change) > 60f) { grip.held = grip.ready = false; return; }
        delta += change; count++;
    }
}
