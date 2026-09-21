using System.Collections.Generic;
using UnityEngine;

public class WarlockLightningStrike : MonoBehaviour
{
    public GameObject bolt;
    public Light flash;
    public Transform groundRing;
    float age;
    bool struck;
    public void Strike(Transform caster, float radius)
    {
        if (struck) return;
        struck = true;
        ApplyVisuals();
        var hitTargets = new HashSet<WarlockPracticeTarget>();
        // A short vertical volume catches characters at the ground contact, once per strike.
        foreach (var collider in Physics.OverlapCapsule(transform.position + Vector3.up * .15f,
                     transform.position + Vector3.up * 1.35f, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            var target = collider.GetComponentInParent<WarlockPracticeTarget>();
            if (!target || target.transform.IsChildOf(caster) || !hitTargets.Add(target)) continue;
            Vector3 direction = target.transform.position - transform.position; direction.y = 0;
            if (direction.sqrMagnitude < .01f) direction = target.transform.position - caster.position;
            if (direction.sqrMagnitude < .01f) direction = caster.forward;
            target.ReceiveThunderHit(direction);
        }
        foreach (var particles in GetComponentsInChildren<ParticleSystem>()) particles.Play();
        Destroy(gameObject, .9f);
    }
    void Update()
    {
        age += Time.deltaTime;
        ApplyVisuals();
    }
    void ApplyVisuals()
    {
        bool visible = age < .1f || (age >= .13f && age < .2f);
        if (bolt) bolt.SetActive(visible);
        if (flash) flash.intensity = visible ? 3f : 0;
        if (groundRing)
        {
            groundRing.gameObject.SetActive(age < .45f);
            groundRing.localScale = Vector3.one * Mathf.Lerp(.15f, 1.2f, age / .45f);
        }
    }
}
