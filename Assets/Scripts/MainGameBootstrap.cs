using UnityEngine;

/// <summary>Construit un petit terrain d'essai et instancie le joueur FPS.</summary>
public class MainGameBootstrap : MonoBehaviour
{
    private void Start()
    {
        CreateGround();
        CreateLighting();
        CreateTargets();
    }

    private static void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.5f, 12f);
        ground.transform.localScale = new Vector3(30f, 1f, 50f);
        SetColor(ground, new Color(0.23f, 0.31f, 0.27f));
    }

    private static void CreateLighting()
    {
        var lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.94f, 0.82f);
        light.shadows = LightShadows.Soft;
        RenderSettings.ambientLight = new Color(0.42f, 0.46f, 0.52f);
    }

    private static void CreateTargets()
    {
        CreateTarget("Target Center", new Vector3(0f, 1.1f, 14f));
        CreateTarget("Target Left", new Vector3(-3f, 1.1f, 17f));
        CreateTarget("Target Right", new Vector3(3f, 1.1f, 17f));
    }

    private static void CreateTarget(string targetName, Vector3 position)
    {
        var target = new GameObject(targetName);
        target.transform.position = position;
        target.AddComponent<ShootingTarget>();
    }

    private static void SetColor(GameObject target, Color color)
    {
        var renderer = target.GetComponent<Renderer>();
        if (renderer == null)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        renderer.material = new Material(shader) { color = color };
    }
}
