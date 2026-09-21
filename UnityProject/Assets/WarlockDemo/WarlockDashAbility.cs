using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// The cast controller owns input/queuing so Q cannot overlap E, R, or fireball.
public class WarlockDashAbility : MonoBehaviour
{
    public float chargeTime = 19f / 30f;
    public float travelEndTime = 26f / 30f;
    public float impactTime = 1f;
    public float duration = 2.1f;
    public float hitRadius = .9f;
    public float arenaHalfExtent = 8.5f;
    public Material afterimageMaterial;
    public Material impactMaterial;
    public Transform staff;
    public Vector3 staffTipLocal;
    public int DashesStarted { get; private set; }
    public int Impacts { get; private set; }
    public int AfterimagesCreated { get; private set; }
    public Vector3 LastDestination { get; private set; }
    public Vector3 LastImpactPosition { get; private set; }
    public bool HasImpacted => impacted;
    Vector3 start;
    bool impacted, blocked;
    int nextGhost;
    SkinnedMeshRenderer[] skins;

    public void BeginDash(Vector3 cursor)
    {
        start = transform.position;
        Vector3 delta = cursor - start; delta.y = 0;
        // Limit distance along the SAME line, not by clamping X/Z independently.
        float t = 1;
        if (Mathf.Abs(delta.x) > .0001f)
            t = Mathf.Min(t, ((delta.x > 0 ? arenaHalfExtent : -arenaHalfExtent) - start.x) / delta.x);
        if (Mathf.Abs(delta.z) > .0001f)
            t = Mathf.Min(t, ((delta.z > 0 ? arenaHalfExtent : -arenaHalfExtent) - start.z) / delta.z);
        LastDestination = start + delta * Mathf.Clamp01(t);
        impacted = false; blocked = false; nextGhost = 0;
        skins = GetComponentsInChildren<SkinnedMeshRenderer>();
        DashesStarted++;
    }

    public void MoveDash(float elapsed)
    {
        if (elapsed < chargeTime || blocked || impacted) return;
        float fraction = Mathf.InverseLerp(chargeTime, travelEndTime, elapsed);
        Vector3 desired = Vector3.Lerp(start, LastDestination, Mathf.SmoothStep(0, 1, fraction));
        Vector3 from = transform.position;
        Vector3 step = desired - from;
        float distance = step.magnitude;
        if (distance > .0001f)
        {
            float allowed = distance;
            // Sweep every frame to avoid tunnelling through solid arena props at dash speed.
            foreach (var hit in Physics.CapsuleCastAll(from + Vector3.up * .35f,
                         from + Vector3.up * 1.1f, .24f, step / distance, distance,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform) ||
                    hit.collider.GetComponentInParent<WarlockPracticeTarget>() ||
                    hit.collider.GetComponentInParent<WarlockFireballProjectile>()) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0, hit.distance - .025f));
            }
            desired = from + step / distance * allowed;
            if (allowed < distance) { blocked = true; LastDestination = desired; }
        }
        transform.position = desired;
    }

    public void UpdateDashEffects(float elapsed)
    {
        if (elapsed >= chargeTime && elapsed <= impactTime && afterimageMaterial)
        {
            float progress = Mathf.InverseLerp(chargeTime, travelEndTime, elapsed);
            while (nextGhost < 6 && progress >= nextGhost / 6f)
            {
                Vector3 location = Vector3.Lerp(start, LastDestination, Mathf.SmoothStep(0, 1, nextGhost / 6f));
                if (Vector3.Distance(start, LastDestination) > .1f) CreateAfterimage(location);
                nextGhost++;
            }
        }
        if (!impacted && elapsed >= impactTime)
        {
            impacted = true; Impacts++;
            Vector3 contact = staff ? staff.TransformPoint(staffTipLocal) : transform.position + transform.forward * .4f;
            contact.y = transform.position.y + .025f;
            LastImpactPosition = contact;
            var hitTargets = new HashSet<WarlockPracticeTarget>();
            foreach (var collider in Physics.OverlapCapsule(contact + Vector3.up * .1f,
                         contact + Vector3.up * 1.35f, hitRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var target = collider.GetComponentInParent<WarlockPracticeTarget>();
                if (!target || target.transform.IsChildOf(transform) || !hitTargets.Add(target)) continue;
                Vector3 direction = target.transform.position - contact; direction.y = 0;
                if (direction.sqrMagnitude < .01f) direction = transform.forward;
                target.ReceiveDashHit(direction);
            }
            if (impactMaterial) WarlockDashImpact.Create(contact, impactMaterial);
        }
    }

    void CreateAfterimage(Vector3 location)
    {
        var root = new GameObject("Dash Afterimage");
        Vector3 offset = location - transform.position;
        var ownedMeshes = new List<Mesh>();
        foreach (var skin in skins)
        {
            if (!skin || !skin.enabled || !skin.gameObject.activeInHierarchy) continue;
            var mesh = new Mesh { name = "Dash baked pose" };
            skin.BakeMesh(mesh); ownedMeshes.Add(mesh);
            AddGhostMesh(root.transform, skin.transform, mesh, skin.sharedMaterials.Length, offset);
        }
        if (staff)
        {
            foreach (var filter in staff.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh) AddGhostMesh(root.transform, filter.transform, filter.sharedMesh,
                    filter.sharedMesh.subMeshCount, offset);
        }
        root.AddComponent<WarlockDashAfterimage>().Initialize(ownedMeshes.ToArray());
        AfterimagesCreated++;
    }

    void AddGhostMesh(Transform parent, Transform source, Mesh mesh, int slots, Vector3 offset)
    {
        var child = new GameObject("Silhouette");
        child.transform.SetParent(parent, false);
        child.transform.SetPositionAndRotation(source.position + offset, source.rotation);
        child.transform.localScale = source.lossyScale;
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = child.AddComponent<MeshRenderer>();
        var materials = new Material[Mathf.Max(1, slots)];
        for (int i = 0; i < materials.Length; i++) materials[i] = afterimageMaterial;
        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
    }
}
