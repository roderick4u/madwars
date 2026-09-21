using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class WarlockFireballRecoveryVerification
{
    const string Pending = "Warlock.VerifyFireballRecovery";
    static WarlockFireballCast cast;
    static WarlockDemoMovement movement;
    static int stage, shots, trial;
    static float began;
    static Vector3 start;
    static double deadline;
    static WarlockFireballRecoveryVerification() { EditorApplication.playModeStateChanged += Mode; }
    [MenuItem("Tools/Warlock/Verify Fireball Recovery")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(Pending, true);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.isPaused = false;
        EditorApplication.isPlaying = true;
    }
    static void Mode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            stage = trial = 0; deadline = EditorApplication.timeSinceStartup + 25;
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.ExitingPlayMode) Cleanup();
    }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Begin()
    {
        movement.StopForCast();
        start = cast.transform.position; shots = cast.ShotsFired;
        began = Time.time;
        cast.RequestCast(start + Vector3.forward * 6);
        if (trial == 0) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W));
        stage = 1;
    }
    static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup < deadline, "Timeout");
            if (stage == 0)
            {
                if (Time.timeSinceLevelLoad < 1.2f) return;
                cast = UnityEngine.Object.FindFirstObjectByType<WarlockFireballCast>();
                Check(cast && Keyboard.current != null && Mouse.current != null, "Missing player/input");
                if (cast.IsCasting) return;
                movement = cast.GetComponent<WarlockDemoMovement>();
                Begin();
            }
            float elapsed = Time.time - began;
            if (stage == 1 && elapsed >= cast.releaseTime * .4f)
            {
                Check(cast.MovementLocked && cast.ShotsFired == shots && Vector3.Distance(start, cast.transform.position) < .001f, "Moved/fired before release");
                if (trial == 1)
                {
                    Vector3 screen = cast.viewCamera.WorldToScreenPoint(start + Vector3.right * 3);
                    var mouse = new MouseState { position = new Vector2(screen.x, screen.y) }.WithButton(MouseButton.Right);
                    InputSystem.QueueStateEvent(Mouse.current, mouse);
                }
                stage = 2;
            }
            else if (stage == 2 && elapsed >= cast.releaseTime * .7f)
            {
                Check(cast.MovementLocked && Vector3.Distance(start, cast.transform.position) < .001f, "Preparation movement lock failed");
                if (trial == 1) InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = Mouse.current.position.ReadValue() });
                stage = 3;
            }
            else if (stage == 3 && elapsed >= cast.releaseTime + .18f)
            {
                Check(cast.ShotsFired == shots + 1 && !cast.MovementLocked, "Release must spawn exactly one projectile and unlock movement");
                if (trial < 2)
                    Check(!cast.IsCasting && movement.locomotion == "Run" && Vector3.Distance(start, cast.transform.position) > .03f, "Movement did not interrupt recovery");
                else
                    Check(cast.IsRecovery && Vector3.Distance(start, cast.transform.position) < .001f, "Idle recovery was interrupted without input");
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                movement.StopForCast();
                if (trial < 2) cast.animator.CrossFadeInFixedTime("Idle", .12f);
                stage = 4;
            }
            else if (stage == 4 && elapsed > cast.castDuration + .3f)
            {
                Check(cast.ShotsFired == shots + 1 && !cast.IsCasting, "Duplicate shot or stuck casting");
                if (++trial < 3) Begin();
                else
                {
                    Debug.Log("FIREBALL RECOVERY PASS: preparation locked; exactly one projectile; held WASD and buffered right-click move after release; smooth Run; idle recovery completes; no duplicate shots.");
                    Cleanup(); EditorApplication.isPlaying = false;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("FIREBALL RECOVERY FAILED: " + e.Message);
            Cleanup(); EditorApplication.isPlaying = false;
        }
    }
    static void Cleanup()
    {
        EditorApplication.update -= Tick;
        if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        if (Mouse.current != null) InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = Mouse.current.position.ReadValue() });
    }
}
