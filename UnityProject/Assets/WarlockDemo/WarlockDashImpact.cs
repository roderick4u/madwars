using UnityEngine;
using UnityEngine.Rendering;

public class WarlockDashImpact : MonoBehaviour
{
    LineRenderer ring;
    Light flash;
    float age;
    MaterialPropertyBlock properties;
    void Awake() { properties = new MaterialPropertyBlock(); }
    public static void Create(Vector3 position, Material material)
    {
        var root = new GameObject("Dash Staff Impact"); root.transform.position = position;
        var fx = root.AddComponent<WarlockDashImpact>();
        fx.ring = root.AddComponent<LineRenderer>();
        fx.ring.useWorldSpace = false; fx.ring.loop = true; fx.ring.positionCount = 48;
        fx.ring.sharedMaterial = material; fx.ring.widthMultiplier = .045f;
        fx.ring.shadowCastingMode = ShadowCastingMode.Off; fx.ring.receiveShadows = false;
        fx.flash = root.AddComponent<Light>(); fx.flash.type = LightType.Point;
        fx.flash.color = new Color(1, .7f, .25f); fx.flash.range = 3;
        fx.Apply(); Destroy(root, .45f);
    }
    void Update() { age += Time.deltaTime; Apply(); }
    void Apply()
    {
        float radius = Mathf.Lerp(.12f, 1.2f, age / .45f);
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2 / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, .02f, Mathf.Sin(angle) * radius));
        }
        properties.SetColor("_BaseColor", new Color(1, .7f, .25f, Mathf.Clamp01(1 - age / .45f)));
        ring.SetPropertyBlock(properties);
        flash.intensity = Mathf.Max(0, 3 * (1 - age / .15f));
    }
}
