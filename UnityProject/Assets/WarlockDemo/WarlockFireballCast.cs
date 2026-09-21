using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(100)]
public class WarlockFireballCast : MonoBehaviour
{
    // LightStrike is wired by WarlockThunderSetup into the R-key ability path.
    public Animator animator;
    public Camera viewCamera;
    public Transform castingHand;
    public GameObject projectilePrefab;
    public float releaseTime = .5f, castDuration = 1f;
    public float hitDuration = 1.2f;
    public TrailRenderer staffHitTrail;
    public Light staffHitFlash;
    public GameObject staffImpactPrefab;
    public GameObject lightningPrefab;
    public float lightningReleaseTime = .8f, lightningDuration = 1.6f;
    public float lightningHitRadius = .7f;
    public WarlockDashAbility dashAbility;
    public bool IsDash { get; private set; }
    public int LightningStrikesSummoned { get; private set; }
    public Vector3 LastLightningPosition { get; private set; }
    public bool IsLightning { get; private set; }
    public int StaffHitsStarted { get; private set; }
    public bool IsStaffHit { get; private set; }
    public bool IsCasting { get; private set; }
    public bool IsDashRecovery => IsCasting && IsDash && dashAbility && dashAbility.HasImpacted;
    public bool IsFireball => IsCasting && !IsDash && !IsStaffHit && !IsLightning;
    public bool IsRecovery => IsDashRecovery || (IsFireball && released);
    public bool MovementLocked => IsCasting && !IsRecovery;
    public void CancelDashRecovery()
    {
        if (!IsDashRecovery) return;
        CancelRecovery();
    }
    public void CancelRecovery()
    {
        if (!IsRecovery) return;
        string previousAnimation = IsDash ? "Dash" : "Cast";
        IsCasting = false;
        IsDash = false;
        // Locomotion owns the next blend; do not force an Idle frame between cast and run.
        if (movement) movement.locomotion = previousAnimation;
    }
    public int ShotsFired { get; private set; }
    enum Ability { Fireball, Staff, Lightning, Dash }
    readonly Queue<(Vector3 aim, Ability ability)> pending = new Queue<(Vector3, Ability)>();
    Vector3 target;
    float elapsed;
    bool released;
    readonly HashSet<WarlockPracticeTarget> struckTargets = new HashSet<WarlockPracticeTarget>();
    Vector3 previousTip;
    bool trackingTip;
    WarlockDemoMovement movement;

    void Awake() { movement = GetComponent<WarlockDemoMovement>(); }
    void Update()
    {
        if (Time.timeScale <= 0) return;
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()) &&
            TryGetCursorAim(true, out Vector3 dashAim))
            RequestDash(dashAim);
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            RequestStaffHit();
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()) &&
            TryGetCursorAim(true, out Vector3 lightningAim))
            RequestLightning(lightningAim);
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            if (TryGetCursorAim(false, out Vector3 aim)) RequestCast(aim);
        }
        if (IsCasting)
        {
            elapsed += Time.deltaTime;
            if (IsDash) dashAbility.MoveDash(elapsed);
        }
        else if (pending.Count > 0) { var next = pending.Dequeue(); BeginCast(next.aim, next.ability); }
    }

    bool TryGetCursorAim(bool groundTarget, out Vector3 aim)
    {
        aim = default;
        if (!viewCamera || Mouse.current == null) return false;
        Ray ray = viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        float nearest = float.PositiveInfinity;
        foreach (var hit in Physics.RaycastAll(ray, 100, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<WarlockFireballProjectile>()) continue;
            if (hit.distance >= nearest) continue;
            nearest = hit.distance; aim = hit.point;
            var practiceTarget = hit.collider.GetComponentInParent<WarlockPracticeTarget>();
            if (groundTarget && practiceTarget) aim.y = practiceTarget.transform.position.y;
        }
        if (!float.IsPositiveInfinity(nearest)) return true;
        if (new Plane(Vector3.up, transform.position).Raycast(ray, out float distance)) { aim = ray.GetPoint(distance); return true; }
        return false;
    }

    public void RequestCast(Vector3 aim)
    {
        if (!projectilePrefab || !animator || !castingHand) return;
        CancelRecovery();
        if (IsCasting) pending.Enqueue((aim, Ability.Fireball)); else BeginCast(aim, Ability.Fireball);
    }
    public void RequestStaffHit()
    {
        if (!animator) return;
        CancelRecovery();
        Vector3 aim = transform.position + transform.forward * 5f;
        if (IsCasting) pending.Enqueue((aim, Ability.Staff)); else BeginCast(aim, Ability.Staff);
    }
    public void RequestLightning(Vector3 aim)
    {
        if (!animator || !lightningPrefab) return;
        CancelRecovery();
        // Snapshot the cursor's world position at the key press, including queued casts.
        if (IsCasting) pending.Enqueue((aim, Ability.Lightning)); else BeginCast(aim, Ability.Lightning);
    }
    public void RequestDash(Vector3 aim)
    {
        if (!animator || !dashAbility || !dashAbility.enabled) return;
        CancelRecovery();
        if (IsCasting) pending.Enqueue((aim, Ability.Dash)); else BeginCast(aim, Ability.Dash);
    }
    void BeginCast(Vector3 aim, Ability ability)
    {
        target = aim; elapsed = 0; released = false; IsCasting = true;
        IsStaffHit = ability == Ability.Staff;
        IsLightning = ability == Ability.Lightning;
        IsDash = ability == Ability.Dash;
        struckTargets.Clear(); trackingTip = false;
        if (IsStaffHit) StaffHitsStarted++;
        if (staffHitTrail) { staffHitTrail.emitting = false; staffHitTrail.Clear(); }
        if (movement) movement.StopForCast();
        Vector3 facing = aim - transform.position; facing.y = 0;
        if (facing.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(facing);
        if (IsDash) dashAbility.BeginDash(aim);
        animator.CrossFadeInFixedTime(IsDash ? "Dash" : IsLightning ? "LightStrike" : IsStaffHit ? "Hit" : "Cast", .06f, 0, 0);
    }
    void LateUpdate()
    {
        if (!IsCasting) return;
        if (IsDash) dashAbility.UpdateDashEffects(elapsed);
        bool trailActive = IsStaffHit && elapsed >= 13f / 30f && elapsed <= 23f / 30f;
        if (trailActive && staffHitTrail)
        {
            Vector3 tip = staffHitTrail.transform.position;
            Vector3 from = trackingTip ? previousTip : tip;
            foreach (var collider in Physics.OverlapCapsule(from, tip, .22f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var target = collider.GetComponentInParent<WarlockPracticeTarget>();
                if (target && !target.transform.IsChildOf(transform) && struckTargets.Add(target))
                {
                    target.ReceiveHit(true, target.transform.position - transform.position);
                    if (staffImpactPrefab)
                    {
                        Vector3 contact = collider.ClosestPoint(tip);
                        Vector3 normal = contact - collider.bounds.center;
                        if (normal.sqrMagnitude < .0001f) normal = -transform.forward;
                        var impact = Instantiate(staffImpactPrefab, contact, Quaternion.LookRotation(normal));
                        foreach (var particles in impact.GetComponentsInChildren<ParticleSystem>()) particles.Play();
                        Destroy(impact, 1.5f);
                    }
                }
            }
            previousTip = tip; trackingTip = true;
        }
        else trackingTip = false;
        if (staffHitTrail) staffHitTrail.emitting = trailActive;
        if (staffHitFlash) staffHitFlash.intensity = trailActive ? 1.3f * Mathf.Sin(Mathf.PI * Mathf.InverseLerp(13f / 30f, 23f / 30f, elapsed)) : 0;
        // After Animator evaluation and the staff constraint: launch from the animated free hand.
        if (IsLightning && !released && elapsed >= lightningReleaseTime)
        {
            released = true;
            var strike = Instantiate(lightningPrefab, target, Quaternion.identity);
            strike.GetComponent<WarlockLightningStrike>().Strike(transform, lightningHitRadius);
            LastLightningPosition = target;
            LightningStrikesSummoned++;
        }
        if (!IsDash && !IsStaffHit && !IsLightning && !released && elapsed >= releaseTime)
        {
            released = true;
            Vector3 origin = castingHand.position + transform.forward * .2f;
            Vector3 direction = target - origin;
            if (direction.sqrMagnitude < .001f) direction = transform.forward;
            var shot = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
            shot.GetComponent<WarlockFireballProjectile>().Launch(transform, direction.normalized);
            ShotsFired++;
        }
        if (elapsed >= (IsDash ? dashAbility.duration : IsLightning ? lightningDuration : IsStaffHit ? hitDuration : castDuration))
        {
            IsCasting = false;
            IsStaffHit = false;
            IsLightning = false;
            IsDash = false;
            if (staffHitTrail) staffHitTrail.emitting = false;
            if (staffHitFlash) staffHitFlash.intensity = 0;
            animator.CrossFadeInFixedTime("Idle", .12f);
            if (movement) movement.locomotion = "Idle";
        }
    }
    void OnDisable() {
        pending.Clear(); IsCasting = false; IsStaffHit = false; IsLightning = false; IsDash = false;
        if (staffHitTrail) { staffHitTrail.emitting = false; staffHitTrail.Clear(); }
        if (staffHitFlash) staffHitFlash.intensity = 0;
    }
}
