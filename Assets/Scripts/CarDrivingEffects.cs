using UnityEngine;

// Small contact-driven effects with a fixed skidmark budget. No extra rendering cameras.
public sealed class CarDrivingEffects : MonoBehaviour
{
    public ArcadeCar car;
    public ParticleSystem[] smoke;
    public ParticleSystem[] dust;
    public ParticleSystem sparks;
    public Material skidMaterial;
    public const int MarkBudget = 512;
    public int ActiveMarks => Mathf.Min(markCount, MarkBudget);
    readonly Vector3[] lastContact = new Vector3[4];
    readonly bool[] marking = new bool[4];
    readonly Vector3[] vertices = new Vector3[MarkBudget * 4];
    readonly int[] triangles = new int[MarkBudget * 6];
    readonly Vector2[] uvs = new Vector2[MarkBudget * 4];
    Mesh skidMesh;
    GameObject skidObject;
    int markCount, resetVersion;
    float sampleTime, lastSpark;

    void Start()
    {
        skidObject = new GameObject("Bounded tire marks", typeof(MeshFilter), typeof(MeshRenderer));
        skidMesh = new Mesh { name = "Recycled skidmarks" };
        skidMesh.MarkDynamic();
        for (int i = 0; i < MarkBudget; i++)
        {
            int v = i * 4, t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            uvs[v] = Vector2.zero; uvs[v + 1] = Vector2.right;
            uvs[v + 2] = Vector2.up; uvs[v + 3] = Vector2.one;
        }
        skidMesh.vertices = vertices; skidMesh.triangles = triangles; skidMesh.uv = uvs;
        skidObject.GetComponent<MeshFilter>().sharedMesh = skidMesh;
        var renderer = skidObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = skidMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        resetVersion = car.ResetVersion;
    }

    void Update()
    {
        if (resetVersion != car.ResetVersion)
        {
            resetVersion = car.ResetVersion;
            ClearEffects();
        }
        // Particle simulation uses scaled time, so pausing preserves marks without emitting.
        if (Time.timeScale == 0f) return;
        sampleTime += Time.deltaTime;
        if (sampleTime < 0.04f) return;
        sampleTime = 0f;
        bool changed = false;
        for (int i = 0; i < car.wheels.Length; i++)
        {
            if (!car.wheels[i].GetGroundHit(out WheelHit hit) || car.SpeedKph < 7f)
            {
                marking[i] = false;
                continue;
            }
            bool asphalt = hit.collider.name == "Grand Prix Circuit" || hit.collider.name.StartsWith("Curb");
            float slip = Mathf.Max(Mathf.Abs(hit.sidewaysSlip), Mathf.Abs(hit.forwardSlip));
            Vector3 position = hit.point + hit.normal * 0.015f;
            if (!asphalt)
            {
                Emit(dust[i], position, hit.normal * 0.6f - car.transform.forward * 0.5f, 2);
                marking[i] = false;
                continue;
            }
            if (slip < 0.22f)
            {
                marking[i] = false;
                continue;
            }
            if (slip > 0.32f) Emit(smoke[i], position, hit.normal * 0.5f, 1);
            if (marking[i] && Vector3.Distance(lastContact[i], position) < 3f && Vector3.Distance(lastContact[i], position) > 0.08f)
            {
                Vector3 side = Vector3.Cross(hit.normal, position - lastContact[i]).normalized * 0.12f;
                int v = (markCount++ % MarkBudget) * 4;
                vertices[v] = lastContact[i] - side; vertices[v + 1] = lastContact[i] + side;
                vertices[v + 2] = position - side; vertices[v + 3] = position + side;
                changed = true;
            }
            lastContact[i] = position;
            marking[i] = true;
        }
        if (changed)
        {
            skidMesh.vertices = vertices;
            skidMesh.RecalculateBounds();
        }
    }

    void OnCollisionEnter(Collision collision) => Impact(collision);
    void OnCollisionStay(Collision collision) => Impact(collision);

    void Impact(Collision collision)
    {
        if (Time.timeScale == 0f || Time.time - lastSpark < 0.15f || collision.relativeVelocity.magnitude < 2f
            || !collision.collider.name.StartsWith("Guardrail") || collision.contactCount == 0) return;
        lastSpark = Time.time;
        var contact = collision.GetContact(0);
        Emit(sparks, contact.point, contact.normal * 2f + Vector3.up, 7);
    }

    static void Emit(ParticleSystem particles, Vector3 position, Vector3 velocity, int count)
    {
        var parameters = new ParticleSystem.EmitParams { position = position, velocity = velocity, applyShapeToPosition = false };
        particles.Emit(parameters, count);
    }

    public void ClearEffects()
    {
        foreach (var effect in smoke) effect.Clear();
        foreach (var effect in dust) effect.Clear();
        sparks.Clear();
        System.Array.Clear(marking, 0, marking.Length);
        System.Array.Clear(vertices, 0, vertices.Length);
        markCount = 0;
        if (skidMesh != null) { skidMesh.vertices = vertices; skidMesh.RecalculateBounds(); }
    }

    void OnDestroy()
    {
        if (skidObject != null) Destroy(skidObject);
        if (skidMesh != null) Destroy(skidMesh);
    }
}
