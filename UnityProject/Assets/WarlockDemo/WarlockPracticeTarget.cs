using UnityEngine;

// Reusable practice target: records hits and flashes without dying.
[RequireComponent(typeof(Rigidbody))]
public class WarlockPracticeTarget : MonoBehaviour
{
    public float fireballPushSpeed = 4f;
    public float staffPushSpeed = 5f;
    public float thunderPushSpeed = 6f;
    public float dashPushSpeed = 6f;
    public float horizontalDeceleration = 5f;
    public float maximumPushSpeed = 9f;
    Rigidbody body;
    public int FireballHits { get; private set; }
    public int StaffHits { get; private set; }
    public int ThunderHits { get; private set; }
    public int DashHits { get; private set; }
    public string LastAbility { get; private set; }
    Renderer[] renderers;
    MaterialPropertyBlock block;
    float flash;
    Color color;
    void Awake() { body = GetComponent<Rigidbody>(); renderers = GetComponentsInChildren<SkinnedMeshRenderer>(); block = new MaterialPropertyBlock(); }
    public void ReceiveHit(bool staff, Vector3 pushDirection)
    {
        if (staff) StaffHits++; else FireballHits++;
        LastAbility = staff ? "Staff" : "Fireball";
        ApplyHit(pushDirection, staff ? staffPushSpeed : fireballPushSpeed);
    }
    public void ReceiveThunderHit(Vector3 pushDirection)
    {
        ThunderHits++;
        LastAbility = "Thunder";
        ApplyHit(pushDirection, thunderPushSpeed);
    }
    public void ReceiveDashHit(Vector3 pushDirection)
    {
        DashHits++;
        LastAbility = "Dash";
        ApplyHit(pushDirection, dashPushSpeed);
    }
    void ApplyHit(Vector3 pushDirection, float pushSpeed)
    {
        color = new Color(3f, .1f, .1f);
        flash = .3f;
        pushDirection.y = 0;
        if (pushDirection.sqrMagnitude > .0001f)
        {
            // Wake settled targets and accumulate a small, bounded horizontal impulse.
            body.WakeUp();
            Vector3 velocity = body.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0, velocity.z);
            Vector3 next = Vector3.ClampMagnitude(horizontal + pushDirection.normalized * pushSpeed, maximumPushSpeed);
            // Apply immediately so multiple impacts in one physics tick still obey the cap.
            body.linearVelocity = new Vector3(next.x, velocity.y, next.z);
        }
    }
    void FixedUpdate()
    {
        Vector3 velocity = body.linearVelocity;
        Vector3 horizontal = Vector3.MoveTowards(new Vector3(velocity.x, 0, velocity.z), Vector3.zero, horizontalDeceleration * Time.fixedDeltaTime);
        if (horizontal.x != velocity.x || horizontal.z != velocity.z)
            body.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
    }
    void LateUpdate()
    {
        if (flash <= 0) return;
        flash = Mathf.Max(0, flash - Time.deltaTime);
        foreach (var renderer in renderers)
        {
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", Color.Lerp(Color.white, color, flash / .3f));
            renderer.SetPropertyBlock(block);
        }
    }
    void OnDisable() { if (renderers != null) foreach (var renderer in renderers) if(renderer) renderer.SetPropertyBlock(null); }
}
