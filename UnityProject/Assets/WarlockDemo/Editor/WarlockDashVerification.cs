using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class WarlockDashVerification
{
    const string KeyName = "Warlock.DashVerification";
    static WarlockFireballCast cast;
    static WarlockDashAbility dash;
    static WarlockPracticeTarget target;
    static GameObject fixture, obstacle;
    static Rigidbody probeBody;
    static Vector3 start, aim, modelOffset;
    static int stage, hits, shots, strikes, dashes, impacts, ghosts;
    static float began, fixedStart;
    static double timeout;
    static bool preview;
    static WarlockDashVerification() { EditorApplication.playModeStateChanged += Mode; }

    [MenuItem("Tools/Warlock/Verify Q Dash")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(KeyName, true);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.isPaused = false;
        EditorApplication.isPlaying = true;
    }
    [MenuItem("Tools/Warlock/Preview Q Dash")]
    public static void Preview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(KeyName + ".Preview", true); Run();
    }
    static void Mode(PlayModeStateChange mode)
    {
        if (mode == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(KeyName, false))
        {
            SessionState.SetBool(KeyName, false);
            preview = SessionState.GetBool(KeyName + ".Preview", false);
            SessionState.SetBool(KeyName + ".Preview", false);
            stage = 0; timeout = EditorApplication.timeSinceStartup + 35;
            EditorApplication.update += Tick;
        }
        if (mode == PlayModeStateChange.ExitingPlayMode) Cleanup();
    }
    static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup < timeout, "Timed out.");
            if (stage == 0)
            {
                if (Time.timeSinceLevelLoad < 1.2f) return;
                cast = UnityEngine.Object.FindFirstObjectByType<WarlockFireballCast>();
                target = UnityEngine.Object.FindFirstObjectByType<WarlockPracticeTarget>();
                Check(cast && target && cast.dashAbility, "Dash was not installed in the scene.");
                if (cast.IsCasting) return;
                dash = cast.dashAbility;
                Check(target.dashPushSpeed == 6 && target.horizontalDeceleration == 5 && target.maximumPushSpeed == 9, "Wrong scene knockback values.");
                Check(dash.afterimageMaterial && dash.impactMaterial && dash.staff, "Missing VFX/staff references.");
                Check(cast.animator.HasState(0, Animator.StringToHash("Base Layer.Dash")), "Missing Dash animation state.");
                cast.GetComponent<WarlockDemoMovement>().StopForCast();
                start = cast.transform.position;
                aim = start + new Vector3(3, 0, 3);
                target.GetComponent<Rigidbody>().position = new Vector3(-7, 0, 6);
                Physics.SyncTransforms();
                modelOffset = cast.transform.InverseTransformPoint(cast.animator.transform.position);
                began = Time.time; hits = target.DashHits; shots = cast.ShotsFired;
                strikes = cast.LightningStrikesSummoned; dashes = dash.DashesStarted;
                impacts = dash.Impacts; ghosts = dash.AfterimagesCreated;
                // Camera remains still until Q is read. This exercises the actual Q/cursor path.
                Vector3 screen = cast.viewCamera.WorldToScreenPoint(aim);
                Check(Mouse.current != null && Keyboard.current != null, "No keyboard/mouse.");
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = new Vector2(screen.x, screen.y) });
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Q));
                stage = 1;
            }
            else if (stage == 1 && Time.time - began > .2f)
            {
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                Check(cast.IsDash && dash.DashesStarted == dashes + 1, "Q did not start one dash.");
                Check(Vector3.Distance(dash.LastDestination, aim) < .15f, $"Cursor mismatch: {dash.LastDestination} vs {aim}.");
                Check(Vector3.Distance(cast.transform.position, start) < .001f, "Moved before charging.");
                Check(cast.animator.GetCurrentAnimatorStateInfo(0).IsName("Dash"), "Dash animation is not playing.");
                var info = cast.animator.GetCurrentAnimatorClipInfo(0);
                Check(info.Length > 0 && info[0].clip.name == "Dash" && Mathf.Abs(info[0].clip.length - 1.7f) < .05f && Mathf.Abs(dash.duration - info[0].clip.length) < .001f, "Wrong Blender clip or duration.");
                var body = target.GetComponent<Rigidbody>();
                body.position = aim + cast.transform.forward * .55f; body.linearVelocity = Vector3.zero;
                Physics.SyncTransforms(); stage = 2;
            }
            else if (stage == 2 && preview && Time.time - began >= .85f)
            {
                Cleanup(); EditorApplication.isPaused = true;
                Debug.Log("DASH PREVIEW paused with afterimages. Resume to see the staff impact.");
            }
            else if (stage == 2 && Time.time - began > .91f)
            {
                Check(Vector3.Distance(cast.transform.position, dash.LastDestination) < .03f, "Dash did not reach the cursor destination.");
                Check(Vector3.Distance(cast.transform.InverseTransformPoint(cast.animator.transform.position), modelOffset) < .03f, "FBX root motion drifted away from gameplay root.");
                Check(dash.AfterimagesCreated >= ghosts + 4, "Afterimages were not emitted.");
                Check(dash.Impacts == impacts, "Impact fired before frame 31.");
                Check(cast.MovementLocked, "Movement unlocked before impact.");
                stage = 3;
            }
            else if (stage == 3 && Time.time - began > 1.14f)
            {
                Check(dash.Impacts == impacts + 1 && target.DashHits == hits + 1, "Finishing hit must apply once to the target.");
                Check(target.LastAbility == "Dash" && target.GetComponent<Rigidbody>().linearVelocity.magnitude > .1f, "Dash did not push the target.");
                Check(cast.ShotsFired == shots && cast.LightningStrikesSummoned == strikes, "Q accidentally fired another ability.");
                Check(cast.IsDashRecovery && !cast.MovementLocked, "Movement is still locked after impact.");
                start = cast.transform.position;
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W));
                stage = 31;
            }
            else if (stage == 31 && Time.time - began > 1.4f)
            {
                Check(!cast.IsCasting && !cast.IsDash && Vector3.Distance(start, cast.transform.position) > .1f, "W did not cancel recovery and move before the clip ended.");
                Check(cast.GetComponent<WarlockDemoMovement>().locomotion == "Run", "Recovery did not blend into Run.");
                Check(dash.Impacts == impacts + 1 && target.DashHits == hits + 1, "Recovery movement repeated the hit.");
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                stage = 4;
            }
            else if (stage == 4 && Time.time - began > 2.0f)
            {
                Check(!cast.IsCasting && cast.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Dash did not finish and return to Idle.");
                Check(!UnityEngine.Object.FindFirstObjectByType<WarlockDashAfterimage>() && !UnityEngine.Object.FindFirstObjectByType<WarlockDashImpact>(), "Dash FX leaked.");
                fixture = new GameObject("Dash test physics fixture"); fixture.transform.position = Vector3.up * 30;
                probeBody = fixture.AddComponent<Rigidbody>(); probeBody.useGravity = false;
                var probe = fixture.AddComponent<WarlockPracticeTarget>();
                probeBody.linearVelocity = Vector3.up * 2; probe.ReceiveDashHit(Vector3.right);
                Check(Mathf.Abs(probeBody.linearVelocity.x - 6) < .001f && probeBody.linearVelocity.y == 2, "Dash push must be 6 and preserve vertical velocity.");
                probe.ReceiveDashHit(Vector3.right); probe.ReceiveDashHit(Vector3.right);
                Check(Mathf.Abs(probeBody.linearVelocity.x - 9) < .001f, "Same-frame hits exceed max speed 9.");
                fixedStart = Time.fixedTime; stage = 5;
            }
            else if (stage == 5 && Time.fixedTime - fixedStart > .2f)
            {
                Check(Mathf.Abs(probeBody.linearVelocity.x - (9 - 5 * (Time.fixedTime - fixedStart))) < .03f, "Wrong horizontal deceleration.");
                UnityEngine.Object.Destroy(fixture);
                // A solid barrier across a short dash must stop the player before it.
                start = cast.transform.position;
                obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = "Dash verification barrier";
                obstacle.transform.position = start + new Vector3(1, .8f, 0);
                obstacle.transform.localScale = new Vector3(.2f, 1.6f, 2);
                Physics.SyncTransforms();
                cast.RequestDash(start + Vector3.right * 3); began = Time.time; stage = 6;
            }
            else if (stage == 6 && Time.time - began > .95f)
            {
                Check(cast.transform.position.x > start.x && cast.transform.position.x < start.x + .75f, "Dash tunneled through a solid obstacle.");
                Check(Mathf.Abs(cast.transform.position.z - start.z) < .001f, "Dash left its straight line.");
                stage = 7;
            }
            else if (stage == 7 && Time.time - began > 2.3f)
            {
                UnityEngine.Object.Destroy(obstacle);
                // Boundary clamp must preserve direction on a diagonal cursor request.
                start = cast.transform.position;
                cast.RequestDash(start + new Vector3(-50, 0, 20)); began = Time.time; stage = 8;
            }
            else if (stage == 8 && Time.time - began > 2.3f)
            {
                Vector3 delta = cast.transform.position - start;
                Check(Mathf.Abs(cast.transform.position.x) <= 8.501f && Mathf.Abs(cast.transform.position.z) <= 8.501f, "Dash escaped the arena.");
                Check(Vector3.Cross(delta, new Vector3(-50, 0, 20)).magnitude < .02f, "Boundary clamp changed dash direction.");
                Debug.Log("DASH VERIFICATION PASS: Q + cursor; Blender Dash 1.7s / 52 frames; locked until staff impact; W cancels recovery into Run before clip end; one hit; push 6 / deceleration 5 / cap 9; obstacle sweep; arena boundary; Idle and FX cleanup.");
                Cleanup(); EditorApplication.isPlaying = false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("DASH VERIFICATION FAILED: " + e.Message);
            Cleanup(); EditorApplication.isPlaying = false;
        }
    }
    static void Cleanup()
    {
        EditorApplication.update -= Tick;
        if (fixture) UnityEngine.Object.Destroy(fixture);
        if (obstacle) UnityEngine.Object.Destroy(obstacle);
        if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
    }
}
