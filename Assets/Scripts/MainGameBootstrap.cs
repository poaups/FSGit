using UnityEngine;

/// <summary>
/// Construit le terrain d'essai de MainGame (sol, murs, caisses, lumière, cibles).
/// Le joueur est le prefab Player placé dans la scène ; s'il manque, il est instancié ici.
/// </summary>
public class MainGameBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 playerSpawn = new Vector3(0f, 0.1f, 0f);

    [Header("Options")]
    [Tooltip("Crée une lumière directionnelle. À désactiver si la scène en a déjà une.")]
    [SerializeField] private bool createLighting = true;
    [Tooltip("Ajoute escaliers, rampe et plateformes pour tester les déplacements.")]
    [SerializeField] private bool createMovementCourse = false;
    [SerializeField] private bool showControlsHint = false;

    private void Start()
    {
        EnsurePlayer();
        CreateGround();
        CreateBoundaryWalls();
        CreateCrates();
        if (createLighting)
            CreateLighting();
        if (createMovementCourse)
            CreateMovementCourse();
        CreateTargets();
    }

    private void OnGUI()
    {
        if (!showControlsHint)
            return;

        GUI.Label(new Rect(16f, 12f, 700f, 28f),
            "ZQSD / WASD : déplacement   |   Maj : courir   |   Espace : sauter   |   Clic : tirer   |   R : recharger   |   Échap : libérer la souris");
    }

    /// <summary>Escaliers, rampe et plateformes de hauteurs croissantes pour essayer marche, saut et pentes.</summary>
    private static void CreateMovementCourse()
    {
        Color stoneColor = new Color(0.5f, 0.54f, 0.6f);
        Color accentColor = new Color(0.82f, 0.45f, 0.18f);

        // Escalier de 6 marches (0,25 m chacune) menant à une plate-forme haute.
        const float stepHeight = 0.25f;
        for (int i = 0; i < 6; i++)
        {
            float height = stepHeight * (i + 1);
            CreateBlock("Stair " + (i + 1), new Vector3(-8f, height * 0.5f, 4f + i * 0.6f), new Vector3(3f, height, 0.6f), stoneColor);
        }
        CreateBlock("Stair Landing", new Vector3(-8f, 0.75f, 8.85f), new Vector3(3f, 1.5f, 2.5f), accentColor);

        // Rampe à 20° (la limite de pente du joueur est 45°).
        GameObject ramp = CreateBlock("Ramp", new Vector3(11f, 1f, 5f), new Vector3(3f, 0.3f, 6f), stoneColor);
        ramp.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);

        // Plateformes à sauter : 0,5 m, 0,9 m puis 1,1 m (le saut atteint environ 1,2 m).
        CreateBlock("Jump Platform 1", new Vector3(-12f, 0.25f, -6f), new Vector3(2f, 0.5f, 2f), accentColor);
        CreateBlock("Jump Platform 2", new Vector3(-12f, 0.45f, -3f), new Vector3(2f, 0.9f, 2f), accentColor);
        CreateBlock("Jump Platform 3", new Vector3(-12f, 0.55f, 0f), new Vector3(2f, 1.1f, 2f), accentColor);
    }

    private void EnsurePlayer()
    {
        if (FindFirstObjectByType<FPSPlayerController>() != null)
            return;

        if (playerPrefab == null)
        {
            Debug.LogWarning("Aucun joueur dans la scène et aucun prefab Player assigné à MainGameBootstrap.", this);
            return;
        }

        Instantiate(playerPrefab, playerSpawn, Quaternion.identity);
    }

    private static void CreateGround()
    {
        GameObject ground = CreateBlock("Ground", new Vector3(0f, -0.5f, 12f), new Vector3(30f, 1f, 50f), new Color(0.23f, 0.31f, 0.27f));
        ground.isStatic = true;
    }

    private static void CreateBoundaryWalls()
    {
        Color wallColor = new Color(0.32f, 0.35f, 0.4f);
        CreateBlock("Wall Back", new Vector3(0f, 1.5f, -13.5f), new Vector3(30f, 3f, 1f), wallColor);
        CreateBlock("Wall Front", new Vector3(0f, 1.5f, 37.5f), new Vector3(30f, 3f, 1f), wallColor);
        CreateBlock("Wall Left", new Vector3(-15.5f, 1.5f, 12f), new Vector3(1f, 3f, 52f), wallColor);
        CreateBlock("Wall Right", new Vector3(15.5f, 1.5f, 12f), new Vector3(1f, 3f, 52f), wallColor);
    }

    private static void CreateCrates()
    {
        Color crateColor = new Color(0.55f, 0.4f, 0.24f);
        CreateBlock("Crate A", new Vector3(5f, 0.75f, 8f), new Vector3(1.5f, 1.5f, 1.5f), crateColor);
        CreateBlock("Crate B", new Vector3(6.6f, 0.75f, 8.4f), new Vector3(1.5f, 1.5f, 1.5f), crateColor);
        CreateBlock("Crate C", new Vector3(5.8f, 2.25f, 8.2f), new Vector3(1.5f, 1.5f, 1.5f), crateColor);
        CreateBlock("Crate D", new Vector3(-5f, 0.75f, 10f), new Vector3(2f, 1.5f, 1f), crateColor);

        // Caisse physique : elle bouge quand on lui tire dessus.
        GameObject physicsCrate = CreateBlock("Crate (physics)", new Vector3(0f, 0.4f, 8f), new Vector3(0.8f, 0.8f, 0.8f), new Color(0.7f, 0.5f, 0.28f));
        physicsCrate.AddComponent<Rigidbody>().mass = 2f;
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
        CreateTarget("Target Far Left", new Vector3(-8f, 1.1f, 24f));
        CreateTarget("Target Far Right", new Vector3(8f, 1.1f, 24f));
        CreateTarget("Target Back", new Vector3(0f, 1.1f, 32f));
    }

    private static void CreateTarget(string targetName, Vector3 position)
    {
        var target = new GameObject(targetName);
        target.transform.position = position;
        target.AddComponent<ShootingTarget>();
    }

    private static GameObject CreateBlock(string blockName, Vector3 position, Vector3 scale, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = blockName;
        block.transform.position = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = LowPolyUtil.CreateMaterial(color, 0.1f);
        return block;
    }
}
