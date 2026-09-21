using UnityEngine;

public class WarlockFireballProjectile : MonoBehaviour
{
    public GameObject impactPrefab;
    public float speed = 12f, radius = .18f, lifetime = 6f;
    public LayerMask collisionMask = Physics.DefaultRaycastLayers;
    public static int ImpactCount { get; private set; }
    Transform owner;
    Vector3 direction;
    float age;
    bool launched, impacted;
    public void Launch(Transform caster, Vector3 forward)
    {
        owner = caster; direction = forward.normalized; launched = true;
        foreach (var trail in GetComponentsInChildren<TrailRenderer>()) trail.Clear();
    }
    bool Valid(Collider other)
    {
        return other && !other.transform.IsChildOf(transform) &&
            (!owner || !other.transform.IsChildOf(owner)) &&
            !other.GetComponentInParent<WarlockFireballProjectile>();
    }
    void FixedUpdate()
    {
        if (!launched || impacted) return;
        age += Time.fixedDeltaTime;
        if (age >= lifetime) { Destroy(gameObject); return; }
        // Check initial overlaps as well as sweeping the entire step, including thin walls.
        foreach (var other in Physics.OverlapSphere(transform.position, radius, collisionMask, QueryTriggerInteraction.Ignore))
            if (Valid(other)) { Impact(other.ClosestPoint(transform.position), -direction, other); return; }
        float step = speed * Time.fixedDeltaTime, nearest = float.PositiveInfinity;
        RaycastHit closest = default;
        foreach (var hit in Physics.SphereCastAll(transform.position, radius, direction, step, collisionMask, QueryTriggerInteraction.Ignore))
            if (Valid(hit.collider) && hit.distance < nearest) { nearest = hit.distance; closest = hit; }
        if (!float.IsPositiveInfinity(nearest)) { Impact(closest.point, closest.normal, closest.collider); return; }
        transform.position += direction * step;
    }
    void Impact(Vector3 point, Vector3 normal, Collider collider)
    {
        if (impacted) return;
        impacted = true; ImpactCount++;
        var target = collider.GetComponentInParent<WarlockPracticeTarget>();
        if (target) target.ReceiveHit(false, direction);
        if (impactPrefab)
        {
            var splash = Instantiate(impactPrefab, point + normal * .025f, Quaternion.LookRotation(normal));
            foreach (var particles in splash.GetComponentsInChildren<ParticleSystem>()) particles.Play();
            Destroy(splash, 1.5f);
        }
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
