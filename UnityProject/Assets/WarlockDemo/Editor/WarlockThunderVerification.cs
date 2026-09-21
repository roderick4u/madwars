using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Explicit, editor-only integration check. Does not modify saved scene state.
[InitializeOnLoad]
public static class WarlockThunderVerification
{
    const string PendingKey = "Warlock.ThunderVerification.Pending";
    static WarlockFireballCast cast;
    static WarlockPracticeTarget target;
    static GameObject fixture;
    static Rigidbody body;
    static Vector3 aim;
    static int stage, startCount, startHits;
    static float began, physicsStart, initialSpeed;
    static double timeout;
    static bool preview;

    static WarlockThunderVerification()
    {
        EditorApplication.playModeStateChanged += OnModeChanged;
    }

    [MenuItem("Tools/Warlock/Verify Thunder in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "WarlockArena")
            throw new InvalidOperationException("Open WarlockArena first.");
        SessionState.SetBool(PendingKey, true);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Warlock/Preview Thunder Impact")]
    public static void Preview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(PendingKey + ".Preview", true);
        Run();
    }

    static void OnModeChanged(PlayModeStateChange mode)
    {
        if (mode == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
        {
            SessionState.SetBool(PendingKey, false);
            preview = SessionState.GetBool(PendingKey + ".Preview", false);
            SessionState.SetBool(PendingKey + ".Preview", false);
            stage = 0;
            timeout = EditorApplication.timeSinceStartup + 20;
            EditorApplication.update += Tick;
        }
        if (mode == PlayModeStateChange.ExitingPlayMode) Cleanup();
    }

    static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }

    static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup < timeout, "Verification timed out.");
            if (stage == 0)
            {
                if (Time.timeSinceLevelLoad < .5f) return;
                cast = UnityEngine.Object.FindFirstObjectByType<WarlockFireballCast>();
                target = UnityEngine.Object.FindFirstObjectByType<WarlockPracticeTarget>();
                Check(cast && target && cast.lightningPrefab, "Missing player, target, or thunder prefab.");
                // The menu click can be delivered as a fireball on the first Play frame.
                if (cast.IsCasting) return;
                Check(target.thunderPushSpeed == 6 && target.horizontalDeceleration == 5 && target.maximumPushSpeed == 9, "Scene push values must be 6 / 5 / 9.");
                Check(cast.animator.HasState(0, Animator.StringToHash("Base Layer.LightStrike")), "Animator is missing LightStrike.");
                var fx = cast.lightningPrefab.GetComponent<WarlockLightningStrike>();
                Check(fx && fx.bolt && fx.groundRing && fx.flash, "Missing thunder FX references.");
                Check(fx.bolt.GetComponent<LineRenderer>() && fx.groundRing.GetComponent<LineRenderer>(), "Thunder must use a bolt and ring, not placeholder cubes.");
                cast.GetComponent<WarlockDemoMovement>().StopForCast();
                startCount = cast.LightningStrikesSummoned;
                startHits = target.ThunderHits;
                aim = target.transform.position + Vector3.right * .25f;
                cast.RequestLightning(aim);
                began = Time.time;
                stage = 1;
            }
            else if (stage == 1 && Time.time - began > .25f)
            {
                var state = cast.animator.GetCurrentAnimatorStateInfo(0);
                Check(state.IsName("LightStrike"), $"LightStrike state is not playing: hash={state.shortNameHash}, next={cast.animator.GetNextAnimatorStateInfo(0).shortNameHash}, casting={cast.IsCasting}, lightning={cast.IsLightning}, dt={Time.time - began}, enabled={cast.animator.enabled}, speed={cast.animator.speed}, object={cast.name}.");
                var clips = cast.animator.GetCurrentAnimatorClipInfo(0);
                Check(clips.Length > 0 && clips[0].clip.name == "LightStrike", "Wrong animation bound to LightStrike.");
                Check(Mathf.Abs(clips[0].clip.length - 1.6f) < .05f, "LightStrike should last 1.6 seconds.");
                Check(cast.LightningStrikesSummoned == startCount, "Thunder released before the impact frame.");
                stage = 2;
            }
            else if (stage == 2 && preview && cast.LightningStrikesSummoned > startCount)
            {
                Cleanup();
                EditorApplication.isPaused = true;
                Debug.Log("Thunder impact preview paused. Resume or exit Play mode when finished.");
            }
            else if (stage == 2 && Time.time - began > .9f)
            {
                Check(cast.LightningStrikesSummoned == startCount + 1, "Thunder did not release at 0.8 seconds.");
                Check(Vector3.Distance(cast.LastLightningPosition, aim) < .001f, "Strike missed the requested world position.");
                Check(target.ThunderHits == startHits + 1, "Thunder must hit target once per strike.");
                Check(target.GetComponent<Rigidbody>().linearVelocity.magnitude > .1f, "Thunder did not push the target.");
                stage = 3;
            }
            else if (stage == 3 && Time.time - began > 1.95f)
            {
                Check(!cast.IsCasting && cast.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Cast did not return to Idle.");
                Check(!UnityEngine.Object.FindFirstObjectByType<WarlockLightningStrike>(), "Thunder FX did not clean up.");
                fixture = new GameObject("Thunder verification physics fixture");
                fixture.transform.position = new Vector3(0, 30, 0);
                body = fixture.AddComponent<Rigidbody>();
                body.useGravity = false;
                var probe = fixture.AddComponent<WarlockPracticeTarget>();
                body.linearVelocity = Vector3.up * 2;
                probe.ReceiveThunderHit(Vector3.right);
                Check(Mathf.Abs(body.linearVelocity.x - 6) < .001f && body.linearVelocity.y == 2, "Push must add 6 and preserve vertical speed.");
                probe.ReceiveThunderHit(Vector3.right);
                probe.ReceiveThunderHit(Vector3.right);
                Check(Mathf.Abs(body.linearVelocity.x - 9) < .001f, "Multiple impacts exceeded the speed cap of 9.");
                physicsStart = Time.fixedTime;
                initialSpeed = body.linearVelocity.x;
                stage = 4;
            }
            else if (stage == 4 && Time.fixedTime - physicsStart >= .2f)
            {
                float expected = initialSpeed - 5 * (Time.fixedTime - physicsStart);
                Check(Mathf.Abs(body.linearVelocity.x - expected) < .02f, "Horizontal deceleration is not 5 units/second squared.");
                UnityEngine.Object.Destroy(fixture);
                // Feed the real input path instead of calling RequestLightning for this check.
                Check(Mouse.current != null && Keyboard.current != null, "Mouse/keyboard devices unavailable.");
                aim = cast.transform.position + new Vector3(2, 0, 1);
                Vector3 screen = cast.viewCamera.WorldToScreenPoint(aim);
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = new Vector2(screen.x, screen.y) });
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.R));
                began = Time.time;
                startCount = cast.LightningStrikesSummoned;
                stage = 5;
            }
            else if (stage == 5 && Time.time - began > .2f)
            {
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                Check(cast.IsLightning, "R-key input did not start a thunder cast.");
                stage = 6;
            }
            else if (stage == 6 && Time.time - began > 1.95f)
            {
                Check(cast.LightningStrikesSummoned == startCount + 1, "One R press must summon exactly one thunder strike.");
                Check(Vector3.Distance(cast.LastLightningPosition, aim) < .15f, "R strike did not land at the projected cursor position.");
                Debug.Log("THUNDER VERIFICATION PASS: supplied LightStrike clip; 0.8s release / 1.6s cast; R cursor targeting; one hit; push 6; same-tick cap 9; deceleration 5; Idle return; FX cleanup.");
                Cleanup();
                EditorApplication.isPlaying = false;
            }
        }
        catch (Exception error)
        {
            Debug.LogError("THUNDER VERIFICATION FAILED: " + error.Message);
            Cleanup();
            EditorApplication.isPlaying = false;
        }
    }

    static void Cleanup()
    {
        EditorApplication.update -= Tick;
        if (fixture) UnityEngine.Object.Destroy(fixture);
    }
}
