using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Arme low poly générée au démarrage et tir hitscan depuis le centre de la caméra.</summary>
public class FPSWeapon : MonoBehaviour
{
    [SerializeField] private Transform cameraRoot;
    [SerializeField, Min(0.05f)] private float shotsPerSecond = 5f;
    [SerializeField, Min(1f)] private float range = 100f;
    [SerializeField, Min(0f)] private float damage = 25f;
    [SerializeField] private Color weaponColor = new Color(0.16f, 0.19f, 0.22f);
    [SerializeField] private Color accentColor = new Color(0.82f, 0.36f, 0.12f);

    private InputAction fireAction;
    private float nextShotTime;
    private float flashUntil;
    private Material bodyMaterial;
    private Material accentMaterial;

    private void Awake()
    {
        if (cameraRoot == null && Camera.main != null)
            cameraRoot = Camera.main.transform;
        if (cameraRoot == null)
            cameraRoot = transform;

        bodyMaterial = CreateMaterial(weaponColor);
        accentMaterial = CreateMaterial(accentColor);
        BuildWeaponModel();
    }

    private void OnEnable()
    {
        fireAction = InputSystem.actions.FindAction("Player/Attack");
        if (fireAction != null)
            fireAction.Enable();
        else
            Debug.LogError("Action 'Player/Attack' introuvable. Vérifie InputSystem_Actions.", this);
    }

    private void OnDisable()
    {
        if (fireAction != null)
            fireAction.Disable();
    }

    private void Update()
    {
        if (fireAction != null && fireAction.WasPressedThisFrame() && Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + 1f / shotsPerSecond;
            Shoot();
        }
    }

    private void BuildWeaponModel()
    {
        MakePart("Receiver", new Vector3(0.29f, 0.18f, 0.52f), new Vector3(0f, -0.18f, 0.48f), bodyMaterial);
        MakePart("Barrel", new Vector3(0.09f, 0.09f, 0.44f), new Vector3(0f, -0.13f, 0.91f), accentMaterial);
        MakePart("Muzzle", new Vector3(0.14f, 0.14f, 0.11f), new Vector3(0f, -0.13f, 1.16f), bodyMaterial);
        MakePart("Top rail", new Vector3(0.18f, 0.07f, 0.34f), new Vector3(0f, -0.055f, 0.46f), accentMaterial);
        MakePart("Grip", new Vector3(0.14f, 0.28f, 0.17f), new Vector3(0f, -0.38f, 0.38f), bodyMaterial, new Vector3(-12f, 0f, 0f));
        MakePart("Magazine", new Vector3(0.17f, 0.25f, 0.19f), new Vector3(0f, -0.37f, 0.63f), accentMaterial, new Vector3(-8f, 0f, 0f));
        MakePart("Sight", new Vector3(0.07f, 0.11f, 0.08f), new Vector3(0f, -0.005f, 0.38f), bodyMaterial);
        MakePart("Sight front", new Vector3(0.05f, 0.09f, 0.05f), new Vector3(0f, -0.005f, 0.72f), accentMaterial);
    }

    private void MakePart(string partName, Vector3 size, Vector3 localPosition, Material material, Vector3 localRotation = default)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = partName;
        part.transform.SetParent(cameraRoot, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localRotation);
        part.transform.localScale = size;
        part.layer = gameObject.layer;
        Destroy(part.GetComponent<Collider>());
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void Shoot()
    {
        flashUntil = Time.time + 0.06f;
        Vector3 origin = cameraRoot.position;
        Vector3 direction = cameraRoot.forward;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            if (hit.collider.GetComponentInParent<ShootingTarget>() is ShootingTarget target)
                target.TakeDamage(damage);

            Debug.DrawLine(origin, hit.point, accentColor, 0.15f);
            return;
        }

        Debug.DrawRay(origin, direction * range, accentColor, 0.15f);
    }

    private void OnGUI()
    {
        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(centerX - 1f, centerY - 7f, 2f, 14f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - 7f, centerY - 1f, 14f, 2f), Texture2D.whiteTexture);

        if (Time.time < flashUntil)
        {
            GUI.color = new Color(1f, 0.72f, 0.2f, 0.9f);
            GUI.DrawTexture(new Rect(centerX - 3f, centerY - 3f, 6f, 6f), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("Aucun shader Lit ou Standard trouvé pour l'arme.");
            return null;
        }
        var material = new Material(shader) { color = color };
        return material;
    }
}
