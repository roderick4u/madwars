using UnityEngine;

public class WarlockDashAfterimage : MonoBehaviour
{
    Mesh[] ownedMeshes;
    Renderer[] renderers;
    MaterialPropertyBlock properties;
    void Awake() { properties = new MaterialPropertyBlock(); }
    float age;
    const float Lifetime = .36f;
    public void Initialize(Mesh[] meshes)
    {
        ownedMeshes = meshes;
        renderers = GetComponentsInChildren<Renderer>();
        ApplyColor();
        Destroy(gameObject, Lifetime);
    }
    void Update() { age += Time.deltaTime; ApplyColor(); }
    void ApplyColor()
    {
        if (renderers == null) return;
        var color = new Color(.45f, .7f, 1f, .48f * Mathf.Clamp01(1 - age / Lifetime));
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        foreach (var renderer in renderers) if (renderer) renderer.SetPropertyBlock(properties);
    }
    void OnDestroy()
    {
        if (ownedMeshes != null) foreach (var mesh in ownedMeshes) if (mesh) Destroy(mesh);
    }
}
