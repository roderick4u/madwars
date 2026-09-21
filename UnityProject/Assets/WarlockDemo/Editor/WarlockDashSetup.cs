using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class WarlockDashSetup
{
    const string Folder = "Assets/WarlockDemo/";
    [MenuItem("Tools/Warlock/Install Q Dash")]
    public static void Install()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != Folder + "Scenes/WarlockArena.unity")
            throw new InvalidOperationException("Open WarlockArena in Edit mode first.");
        var player = GameObject.Find("Warlock Player");
        var cast = player ? player.GetComponent<WarlockFireballCast>() : null;
        if (!cast || !cast.animator) throw new InvalidOperationException("Missing arena player/animator.");
        string path = Folder + "Warlock_Dash.fbx";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        var clips = importer.clipAnimations;
        if (clips.Length == 0) clips = importer.defaultClipAnimations;
        if (clips.Length == 0) throw new InvalidOperationException("Dash FBX contains no animation.");
        bool changed = importer.animationType != ModelImporterAnimationType.Generic ||
            importer.avatarSetup != ModelImporterAvatarSetup.NoAvatar ||
            importer.animationCompression != ModelImporterAnimationCompression.Off || !importer.importAnimation;
        var clip = clips[0];
        changed |= clips.Length != 1 || clip.name != "Dash" || clip.firstFrame != 1 || clip.lastFrame != 52 || clip.loopTime;
        clip.name = "Dash"; clip.firstFrame = 1; clip.lastFrame = 52;
        clip.loopTime = false; clip.loopPose = false; clip.wrapMode = WrapMode.Once;
        if (changed)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.importAnimation = true; importer.clipAnimations = new[] { clip };
            importer.SaveAndReimport();
        }
        var imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(x => x.name == "Dash");
        // The Blender file includes a separate staff root. Remap the rig prefix to the
        // existing player's hierarchy and leave its gameplay/model root transform alone.
        var animation = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "WarlockDash.anim");
        if (!animation)
        {
            animation = new AnimationClip();
            AssetDatabase.CreateAsset(animation, Folder + "WarlockDash.anim");
        }
        animation.ClearCurves(); animation.name = "Dash"; animation.frameRate = 30;
        int bound = 0;
        var bones = cast.animator.GetComponentsInChildren<Transform>(true);
        foreach (var binding in AnimationUtility.GetCurveBindings(imported))
        {
            var mapped = binding;
            if (mapped.path.StartsWith("Warlock_Rig/")) mapped.path = mapped.path.Substring("Warlock_Rig/".Length);
            if (mapped.path == "Warlock_Rig") mapped.path = "";
            if (string.IsNullOrEmpty(mapped.path)) continue;
            if (!cast.animator.transform.Find(mapped.path))
            {
                string name = mapped.path.Split('/').Last();
                var matches = bones.Where(x => x != cast.animator.transform && x.name == name).ToArray();
                if (matches.Length != 1) continue;
                mapped.path = AnimationUtility.CalculateTransformPath(matches[0], cast.animator.transform);
            }
            AnimationUtility.SetEditorCurve(animation, mapped, AnimationUtility.GetEditorCurve(imported, binding));
            bound++;
        }
        if (bound < 30) throw new InvalidOperationException($"Dash rig binding failed: {bound}. Source: {string.Join(",", AnimationUtility.GetCurveBindings(imported).Select(x => x.path).Distinct())}. Target: {string.Join(",", bones.Select(x => x.name))}");
        EditorUtility.SetDirty(animation);
        var controller = cast.animator.runtimeAnimatorController as AnimatorController;
        if (!controller) throw new InvalidOperationException("Player needs an AnimatorController.");
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(x => x.state).FirstOrDefault(x => x.name == "Dash");
        if (!state) state = machine.AddState("Dash", new Vector3(420, 390));
        state.motion = animation; state.speed = 1; EditorUtility.SetDirty(controller);
        var dash = player.GetComponent<WarlockDashAbility>();
        if (!dash) dash = Undo.AddComponent<WarlockDashAbility>(player);
        Undo.RecordObjects(new UnityEngine.Object[] { cast, dash }, "Install Q Dash");
        dash.chargeTime = 19f / 30f; dash.travelEndTime = 26f / 30f;
        dash.impactTime = 1; dash.duration = animation.length; dash.hitRadius = .9f;
        dash.arenaHalfExtent = 8.5f;
        dash.afterimageMaterial = MakeMaterial("DashAfterimage.mat", new Color(.45f, .7f, 1, .48f));
        dash.impactMaterial = MakeMaterial("DashImpact.mat", new Color(1, .7f, .25f, 1));
        var grip = player.GetComponent<WarlockStaffGrip>();
        dash.staff = grip ? grip.staff : null;
        if (dash.staff)
        {
            // Find the staff's actual butt end from its mesh, not a hard-coded FBX axis.
            float lowest = float.PositiveInfinity;
            foreach (var filter in dash.staff.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.sharedMesh) continue;
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    Vector3 world = filter.transform.TransformPoint(vertex);
                    if (world.y >= lowest) continue;
                    lowest = world.y; dash.staffTipLocal = dash.staff.InverseTransformPoint(world);
                }
            }
        }
        cast.dashAbility = dash;
        EditorUtility.SetDirty(cast); EditorUtility.SetDirty(dash);
        foreach (var target in UnityEngine.Object.FindObjectsByType<WarlockPracticeTarget>(FindObjectsSortMode.None))
        {
            if (target.gameObject.scene != scene) continue;
            Undo.RecordObject(target, "Dash knockback values");
            target.dashPushSpeed = 6; target.horizontalDeceleration = 5; target.maximumPushSpeed = 9;
            EditorUtility.SetDirty(target);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log($"DASH INSTALL COMPLETE: Q -> cursor; {animation.name} ({animation.length:F2}s); push 6 / deceleration 5 / max 9; afterimages and staff impact.");
    }

    static Material MakeMaterial(string filename, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) throw new InvalidOperationException("URP Unlit shader not found.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + filename);
        if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, Folder + filename); }
        material.shader = shader; material.SetColor("_BaseColor", color);
        material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", (float)CullMode.Off);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetShaderPassEnabled("ShadowCaster", false);
        EditorUtility.SetDirty(material); return material;
    }
}
