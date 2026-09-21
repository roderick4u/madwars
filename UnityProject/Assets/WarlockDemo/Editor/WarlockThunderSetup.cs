using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One-time editor wiring for the LightStrike animation and cursor-targeted thunder ability.
public static class WarlockThunderSetup
{
    const string ModelPath = "Assets/WarlockDemo/Warlock_LightStrike.fbx";
    const string ControllerPath = "Assets/WarlockDemo/WarlockLocomotion.controller";
    const string PrefabPath = "Assets/WarlockDemo/LightningStrike.prefab";
    const string ScenePath = "Assets/WarlockDemo/Scenes/WarlockArena.unity";
    const string GlowMaterialPath = "Assets/WarlockDemo/ThunderGlow.mat";

    [MenuItem("Tools/Warlock/Repair Thunder Ability")]
    public static void RunNow()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogError("Open WarlockArena in Edit mode before repairing thunder.");
            return;
        }
        Configure();
    }

    static void Configure()
    {
        if (!ConfigureAnimationImport())
        {
            Debug.LogError("Could not import the LightStrike animation.");
            return;
        }

        var prefab = EnsureLightningPrefab();
        var clip = LoadLightStrikeClip();
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (!prefab || !clip || !controller)
        {
            Debug.LogError("Thunder setup is missing its prefab, animation, or controller.");
            return;
        }

        EnsureAnimatorState(controller, clip);
        if (!ConfigureArenaScene(prefab))
        {
            Debug.LogError("Thunder setup could not find the arena player.");
            return;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("WarlockThunderSetup: LightStrike animation, thunder FX, R input, and push stats are wired into WarlockArena.");
    }

    static bool ConfigureAnimationImport()
    {
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (!importer)
            return false;

        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0)
            clips = new[] { new ModelImporterClipAnimation() };

        bool changed = importer.animationType != ModelImporterAnimationType.Generic ||
                       importer.avatarSetup != ModelImporterAvatarSetup.NoAvatar ||
                       !importer.importAnimation ||
                       importer.animationCompression != ModelImporterAnimationCompression.Off;

        foreach (var clip in clips)
        {
            changed |= clip.name != "LightStrike" ||
                       !Mathf.Approximately(clip.firstFrame, 1f) ||
                       !Mathf.Approximately(clip.lastFrame, 49f) ||
                       clip.loopTime || clip.loopPose;
            clip.name = "LightStrike";
            clip.firstFrame = 1f;
            clip.lastFrame = 49f;
            clip.loopTime = false;
            clip.loopPose = false;
            clip.wrapMode = WrapMode.Once;
        }

        if (!changed)
            return true;

        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
        importer.importAnimation = true;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
        return true;
    }

    static AnimationClip LoadLightStrikeClip()
    {
        return AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => clip.name == "LightStrike" && !clip.name.StartsWith("__preview__"));
    }

    static void EnsureAnimatorState(AnimatorController controller, AnimationClip clip)
    {
        var layer = controller.layers[0];
        var state = layer.stateMachine.states
            .Select(entry => entry.state)
            .FirstOrDefault(candidate => candidate.name == "LightStrike");
        if (!state)
            state = layer.stateMachine.AddState("LightStrike", new Vector3(420f, 140f, 0f));

        state.motion = clip;
        state.speed = 1f;
        EditorUtility.SetDirty(controller);
    }

    static Material EnsureGlowMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        var material = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);
        if (!material)
        {
            material = new Material(shader) { name = "Thunder Glow" };
            AssetDatabase.CreateAsset(material, GlowMaterialPath);
        }

        material.shader = shader;
        var color = new Color(0.55f, 0.85f, 1f, 1f);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetColor("_EmissionColor", color * 2.5f);
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject EnsureLightningPrefab()
    {
        var glow = EnsureGlowMaterial();
        var root = new GameObject("Lightning Strike");
        var strike = root.AddComponent<WarlockLightningStrike>();

        var bolt = new GameObject("Bolt");
        bolt.transform.SetParent(root.transform, false);
        var boltLine = ConfigureLine(bolt.AddComponent<LineRenderer>(), glow, 0.095f, false);
        boltLine.positionCount = 9;
        boltLine.SetPositions(new[]
        {
            new Vector3(0.00f, 5.8f, 0.00f), new Vector3(-0.16f, 4.9f, 0.03f),
            new Vector3(0.12f, 4.1f, -0.02f), new Vector3(-0.08f, 3.2f, 0.02f),
            new Vector3(0.17f, 2.35f, -0.03f), new Vector3(-0.12f, 1.55f, 0.01f),
            new Vector3(0.08f, 0.82f, -0.02f), new Vector3(-0.03f, 0.32f, 0.01f),
            new Vector3(0.00f, 0.06f, 0.00f)
        });

        var branch = new GameObject("Bolt Branch");
        branch.transform.SetParent(bolt.transform, false);
        var branchLine = ConfigureLine(branch.AddComponent<LineRenderer>(), glow, 0.045f, false);
        branchLine.positionCount = 5;
        branchLine.SetPositions(new[]
        {
            new Vector3(0.08f, 2.35f, -0.03f), new Vector3(0.48f, 1.9f, -0.02f),
            new Vector3(0.23f, 1.45f, 0.00f), new Vector3(0.55f, 1.02f, -0.01f),
            new Vector3(0.43f, 0.62f, 0.00f)
        });

        var ring = new GameObject("Ground Ring");
        ring.transform.SetParent(root.transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        var ringLine = ConfigureLine(ring.AddComponent<LineRenderer>(), glow, 0.055f, true);
        ringLine.positionCount = 32;
        for (int i = 0; i < ringLine.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / ringLine.positionCount;
            ringLine.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
        }

        var flashObject = new GameObject("Thunder Flash");
        flashObject.transform.SetParent(root.transform, false);
        flashObject.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        var flash = flashObject.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(0.2f, 0.75f, 1f);
        flash.range = 6f;
        flash.intensity = 0f;

        var particlesObject = new GameObject("Thunder Sparks");
        particlesObject.transform.SetParent(root.transform, false);
        particlesObject.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        var particles = particlesObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.35f;
        main.startLifetime = 0.35f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
        main.startColor = new Color(0.25f, 0.8f, 1f, 1f);
        main.maxParticles = 32;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.42f;
        var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = glow;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        strike.bolt = bolt;
        strike.flash = flash;
        strike.groundRing = ring.transform;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    static LineRenderer ConfigureLine(LineRenderer line, Material material, float width, bool loop)
    {
        line.useWorldSpace = false;
        line.loop = loop;
        line.widthMultiplier = width;
        line.numCornerVertices = 3;
        line.numCapVertices = 3;
        line.textureMode = LineTextureMode.Stretch;
        line.sharedMaterial = material;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.18f, 0.72f, 1f), 0.45f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.85f, 0.7f),
                new GradientAlphaKey(0.1f, 1f)
            });
        line.colorGradient = gradient;
        return line;
    }

    static bool ConfigureArenaScene(GameObject lightningPrefab)
    {
        var scene = SceneManager.GetActiveScene();
        var player = GameObject.Find("Warlock Player");
        if (scene.path != ScenePath) return false;

        if (!player)
            return false;

        bool changed = false;
        var cast = player.GetComponent<WarlockFireballCast>();
        if (cast)
        {
            changed |= cast.lightningPrefab != lightningPrefab;
            cast.lightningPrefab = lightningPrefab;
            changed |= !Mathf.Approximately(cast.lightningReleaseTime, 0.8f);
            cast.lightningReleaseTime = 0.8f;
            changed |= !Mathf.Approximately(cast.lightningDuration, 1.6f);
            cast.lightningDuration = 1.6f;
            changed |= !Mathf.Approximately(cast.lightningHitRadius, 0.7f);
            cast.lightningHitRadius = 0.7f;
            if (changed)
                EditorUtility.SetDirty(cast);
        }

        foreach (var target in Object.FindObjectsByType<WarlockPracticeTarget>(FindObjectsSortMode.None))
        {
            bool targetChanged = !Mathf.Approximately(target.thunderPushSpeed, 6f) ||
                                 !Mathf.Approximately(target.horizontalDeceleration, 5f) ||
                                 !Mathf.Approximately(target.maximumPushSpeed, 9f);
            target.thunderPushSpeed = 6f;
            target.horizontalDeceleration = 5f;
            target.maximumPushSpeed = 9f;
            if (targetChanged)
            {
                changed = true;
                EditorUtility.SetDirty(target);
            }
        }

        if (changed || scene.isDirty)
            EditorSceneManager.SaveScene(scene);
        return cast != null;
    }
}
