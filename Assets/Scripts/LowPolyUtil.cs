using UnityEngine;

/// <summary>Petits outils pour fabriquer des objets low poly à partir de primitives, sans asset externe.</summary>
public static class LowPolyUtil
{
    private static Material baseMaterial;

    /// <summary>
    /// Crée un matériau uni de la couleur donnée. On part du matériau par défaut des primitives :
    /// il suit le pipeline de rendu du projet (URP) et, contrairement à Shader.Find, il n'est pas retiré du build.
    /// </summary>
    public static Material CreateMaterial(Color color, float smoothness = 0.2f)
    {
        if (baseMaterial == null)
        {
            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMaterial = probe.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(probe);
        }

        var material = new Material(baseMaterial) { color = color };
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    /// <summary>Crée une pièce (cube, cylindre...) sans collider, rattachée à un parent.</summary>
    public static Transform CreatePart(
        PrimitiveType type,
        string partName,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3 localEuler,
        Material material,
        bool castShadows = true)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.layer = parent.gameObject.layer;

        var collider = part.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        var transform = part.transform;
        transform.SetParent(parent, false);
        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.Euler(localEuler);
        transform.localScale = localScale;

        var renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        if (!castShadows)
        {
            // Pièces d'arme : pas d'ombre propre, elle donnerait des artefacts devant la caméra.
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        return transform;
    }
}
