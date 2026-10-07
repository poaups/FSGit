using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Arme low poly (fabriquée par code à partir de primitives) avec tir hitscan depuis le centre de la caméra,
/// chargeur, rechargement (touche R), recul et petit HUD.
/// </summary>
public class FPSWeapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraRoot;

    [Header("Tir")]
    [SerializeField, Min(0.05f)] private float shotsPerSecond = 8f;
    [Tooltip("Vrai : on peut garder le clic enfoncé. Faux : un tir par clic.")]
    [SerializeField] private bool automatic = true;
    [SerializeField, Min(1f)] private float range = 100f;
    [SerializeField, Min(0f)] private float damage = 25f;
    [SerializeField, Min(0f)] private float impactForce = 6f;

    [Header("Munitions")]
    [SerializeField, Min(1)] private int magazineSize = 30;
    [SerializeField, Min(0)] private int reserveAmmo = 90;
    [SerializeField] private bool infiniteReserve = false;
    [SerializeField, Min(0.2f)] private float reloadTime = 1.7f;

    [Header("Recul")]
    [Tooltip("Degrés dont la visée monte à chaque tir.")]
    [SerializeField, Min(0f)] private float recoilPitch = 0.9f;
    [SerializeField, Min(0f)] private float recoilYaw = 0.3f;
    [SerializeField, Min(0f)] private float kickBack = 0.06f;
    [SerializeField, Min(0f)] private float kickRotation = 4f;
    [SerializeField, Min(1f)] private float recoilRecovery = 14f;

    [Header("Look")]
    [SerializeField] private Color weaponColor = new Color(0.16f, 0.19f, 0.22f);
    [SerializeField] private Color accentColor = new Color(0.82f, 0.36f, 0.12f);
    [SerializeField] private Color metalColor = new Color(0.46f, 0.49f, 0.52f);
    [SerializeField] private Vector3 restPosition = new Vector3(0.17f, -0.17f, 0.45f);

    private static readonly Vector3 MagazineRestPosition = new Vector3(0f, -0.115f, 0.07f);

    private FPSPlayerController controller;
    private InputAction fireAction;
    private InputAction reloadAction;

    private Transform weaponRoot;
    private Transform magazine;
    private Transform muzzle;
    private GameObject muzzleFlash;

    private Material tracerMaterial;
    private Material impactMaterial;

    private int ammoInMagazine;
    private bool reloading;
    private float reloadStartTime;
    private float nextShotTime;
    private float flashUntil;
    private float kickAmount;
    private float emptyMessageUntil;

    private GUIStyle ammoStyle;
    private GUIStyle messageStyle;

    public int AmmoInMagazine => ammoInMagazine;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => reloading;

    private void Awake()
    {
        controller = GetComponent<FPSPlayerController>();
        if (cameraRoot == null && Camera.main != null)
            cameraRoot = Camera.main.transform;
        if (cameraRoot == null)
            cameraRoot = transform;

        ammoInMagazine = magazineSize;
        BuildWeaponModel();
    }

    private void OnEnable()
    {
        fireAction = InputSystem.actions.FindAction("Player/Attack");
        reloadAction = InputSystem.actions.FindAction("Player/Reload");
        if (fireAction == null)
            Debug.LogError("Action 'Player/Attack' introuvable. Vérifie InputSystem_Actions.", this);
        if (reloadAction == null)
            Debug.LogWarning("Action 'Player/Reload' introuvable : la touche R sera lue directement au clavier.", this);
    }

    private void OnDisable()
    {
        reloading = false;
    }

    private void Update()
    {
        bool canAct = controller == null || controller.CursorCaptured;

        if (canAct && !reloading && ReloadPressed())
            StartReload();

        if (reloading && Time.time - reloadStartTime >= reloadTime)
            FinishReload();

        if (canAct && !reloading && FirePressed() && Time.time >= nextShotTime)
        {
            if (ammoInMagazine > 0)
            {
                Shoot();
            }
            else
            {
                nextShotTime = Time.time + 0.3f;
                if (!StartReload())
                    emptyMessageUntil = Time.time + 1.2f;
            }
        }

        UpdateMuzzleFlash();
        AnimateWeapon();
    }

    // ---------------------------------------------------------------- Entrées

    private bool FirePressed()
    {
        if (fireAction == null)
            return false;
        return automatic ? fireAction.IsPressed() : fireAction.WasPressedThisFrame();
    }

    private bool ReloadPressed()
    {
        if (reloadAction != null)
            return reloadAction.WasPressedThisFrame();
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
    }

    // ---------------------------------------------------------------- Tir

    private void Shoot()
    {
        ammoInMagazine--;
        nextShotTime = Time.time + 1f / shotsPerSecond;
        flashUntil = Time.time + 0.05f;
        kickAmount = Mathf.Min(kickAmount + 0.7f, 1.4f);

        if (controller != null)
            controller.AddRecoil(recoilPitch, Random.Range(-recoilYaw, recoilYaw));

        Vector3 origin = cameraRoot.position;
        Vector3 direction = cameraRoot.forward;
        Vector3 endPoint = origin + direction * range;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            // On ignore le joueur lui-même (CharacterController).
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            endPoint = hit.point;
            bool hitTarget = false;

            if (hit.collider.GetComponentInParent<ShootingTarget>() is ShootingTarget target)
            {
                target.TakeDamage(damage);
                hitTarget = true;
            }

            if (hit.rigidbody != null)
                hit.rigidbody.AddForceAtPosition(direction * impactForce, hit.point, ForceMode.Impulse);

            SpawnImpact(hit.point, hit.normal, hitTarget);
            break;
        }

        SpawnTracer(muzzle.position, endPoint);
    }

    private bool StartReload()
    {
        bool hasReserve = infiniteReserve || reserveAmmo > 0;
        if (reloading || ammoInMagazine >= magazineSize || !hasReserve)
            return false;

        reloading = true;
        reloadStartTime = Time.time;
        return true;
    }

    private void FinishReload()
    {
        int needed = magazineSize - ammoInMagazine;
        int taken = infiniteReserve ? needed : Mathf.Min(needed, reserveAmmo);
        ammoInMagazine += taken;
        if (!infiniteReserve)
            reserveAmmo -= taken;
        reloading = false;
    }

    // ---------------------------------------------------------------- Effets

    private void SpawnImpact(Vector3 point, Vector3 normal, bool onTarget)
    {
        var impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impact.name = "Impact";
        Destroy(impact.GetComponent<Collider>());
        impact.transform.position = point + normal * 0.01f;
        impact.transform.localScale = Vector3.one * (onTarget ? 0.14f : 0.09f);
        var impactRenderer = impact.GetComponent<Renderer>();
        impactRenderer.sharedMaterial = impactMaterial;
        impactRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Destroy(impact, onTarget ? 0.2f : 4f);
    }

    private void SpawnTracer(Vector3 start, Vector3 end)
    {
        Vector3 delta = end - start;
        float length = delta.magnitude;
        if (length < 0.2f)
            return;

        var tracer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tracer.name = "Tracer";
        Destroy(tracer.GetComponent<Collider>());
        tracer.transform.position = start + delta * 0.5f;
        tracer.transform.rotation = Quaternion.LookRotation(delta);
        tracer.transform.localScale = new Vector3(0.012f, 0.012f, length);
        var tracerRenderer = tracer.GetComponent<Renderer>();
        tracerRenderer.sharedMaterial = tracerMaterial;
        tracerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Destroy(tracer, 0.04f);
    }

    private void UpdateMuzzleFlash()
    {
        bool flashing = Time.time < flashUntil;
        if (muzzleFlash.activeSelf != flashing)
        {
            muzzleFlash.SetActive(flashing);
            if (flashing)
            {
                muzzleFlash.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
                float size = Random.Range(0.07f, 0.12f);
                muzzleFlash.transform.localScale = new Vector3(size, size, size * 0.6f);
            }
        }
    }

    // ---------------------------------------------------------------- Animation

    private void AnimateWeapon()
    {
        kickAmount = Mathf.Lerp(kickAmount, 0f, 1f - Mathf.Exp(-recoilRecovery * Time.deltaTime));

        Vector3 position = restPosition + new Vector3(0f, 0f, -kickBack * kickAmount);
        Vector3 rotation = new Vector3(-kickRotation * kickAmount, 0f, 0f);
        float magazineDrop = 0f;

        if (reloading)
        {
            float t = Mathf.Clamp01((Time.time - reloadStartTime) / reloadTime);

            // L'arme s'abaisse et s'incline au début, puis remonte à la fin.
            float dip = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(t / 0.2f, (1f - t) / 0.2f)));
            position += new Vector3(-0.04f * dip, -0.09f * dip, 0f);
            rotation += new Vector3(28f * dip, 0f, 18f * dip);

            // Le chargeur sort puis revient entre 30 % et 75 % du temps de rechargement.
            float m = Mathf.Clamp01((t - 0.3f) / 0.45f);
            magazineDrop = Mathf.Sin(m * Mathf.PI);
        }

        weaponRoot.localPosition = position;
        weaponRoot.localRotation = Quaternion.Euler(rotation);
        magazine.localPosition = MagazineRestPosition + new Vector3(0f, -0.2f * magazineDrop, 0f);
    }

    // ---------------------------------------------------------------- Modèle 3D low poly

    private void BuildWeaponModel()
    {
        Material body = LowPolyUtil.CreateMaterial(weaponColor);
        Material metal = LowPolyUtil.CreateMaterial(metalColor, 0.35f);
        Material accent = LowPolyUtil.CreateMaterial(accentColor);
        tracerMaterial = LowPolyUtil.CreateMaterial(new Color(1f, 0.85f, 0.4f), 0f);
        impactMaterial = LowPolyUtil.CreateMaterial(new Color(0.95f, 0.75f, 0.25f), 0f);
        Material flashMaterial = LowPolyUtil.CreateMaterial(new Color(1f, 0.9f, 0.45f), 0f);

        var root = new GameObject("Weapon (low poly)");
        root.layer = cameraRoot.gameObject.layer;
        weaponRoot = root.transform;
        weaponRoot.SetParent(cameraRoot, false);
        weaponRoot.localPosition = restPosition;

        const PrimitiveType Cube = PrimitiveType.Cube;
        const PrimitiveType Cylinder = PrimitiveType.Cylinder;

        // Corps
        Part(Cube, "Receiver", new Vector3(0f, 0f, 0f), new Vector3(0.06f, 0.09f, 0.32f), Vector3.zero, body);
        Part(Cube, "Handguard", new Vector3(0f, 0f, 0.27f), new Vector3(0.055f, 0.065f, 0.22f), Vector3.zero, metal);
        Part(Cube, "Rail", new Vector3(0f, 0.058f, 0f), new Vector3(0.03f, 0.022f, 0.3f), Vector3.zero, metal);
        Part(Cube, "Trigger guard", new Vector3(0f, -0.058f, -0.02f), new Vector3(0.02f, 0.015f, 0.1f), Vector3.zero, body);

        // Canon (cylindres à 20 faces, couchés le long de Z)
        Part(Cylinder, "Barrel", new Vector3(0f, 0.012f, 0.5f), new Vector3(0.022f, 0.1f, 0.022f), new Vector3(90f, 0f, 0f), metal);
        Part(Cylinder, "Muzzle brake", new Vector3(0f, 0.012f, 0.61f), new Vector3(0.034f, 0.025f, 0.034f), new Vector3(90f, 0f, 0f), body);

        // Crosse, poignée, chargeur
        Part(Cube, "Stock", new Vector3(0f, -0.01f, -0.26f), new Vector3(0.05f, 0.085f, 0.2f), new Vector3(4f, 0f, 0f), body);
        Part(Cube, "Stock pad", new Vector3(0f, -0.01f, -0.365f), new Vector3(0.052f, 0.1f, 0.02f), new Vector3(4f, 0f, 0f), accent);
        Part(Cube, "Grip", new Vector3(0f, -0.105f, -0.08f), new Vector3(0.045f, 0.13f, 0.055f), new Vector3(-18f, 0f, 0f), body);
        magazine = Part(Cube, "Magazine", MagazineRestPosition, new Vector3(0.045f, 0.15f, 0.07f), new Vector3(8f, 0f, 0f), accent);

        // Organes de visée
        Part(Cube, "Rear sight", new Vector3(0f, 0.082f, -0.1f), new Vector3(0.035f, 0.03f, 0.02f), Vector3.zero, body);
        Part(Cube, "Front sight", new Vector3(0f, 0.08f, 0.34f), new Vector3(0.012f, 0.04f, 0.012f), Vector3.zero, body);

        // Bouche du canon : flash + petite lumière
        muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(weaponRoot, false);
        muzzle.localPosition = new Vector3(0f, 0.012f, 0.65f);

        muzzleFlash = LowPolyUtil.CreatePart(PrimitiveType.Cube, "Muzzle flash", muzzle, Vector3.zero,
            new Vector3(0.1f, 0.1f, 0.06f), Vector3.zero, flashMaterial, false).gameObject;
        muzzleFlash.SetActive(false);

        // La lumière est enfant du flash : elle n'existe donc que pendant les 0,05 s du tir.
        var lightObject = new GameObject("Muzzle light");
        lightObject.transform.SetParent(muzzleFlash.transform, false);
        var flashLight = lightObject.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = new Color(1f, 0.75f, 0.35f);
        flashLight.intensity = 2f;
        flashLight.range = 5f;
        flashLight.shadows = LightShadows.None;
    }

    private Transform Part(PrimitiveType type, string partName, Vector3 localPosition, Vector3 localScale, Vector3 localEuler, Material material)
    {
        return LowPolyUtil.CreatePart(type, partName, weaponRoot, localPosition, localScale, localEuler, material, false);
    }

    // ---------------------------------------------------------------- HUD

    private void OnGUI()
    {
        EnsureStyles();
        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;

        // Viseur
        GUI.color = reloading ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
        GUI.DrawTexture(new Rect(centerX - 1f, centerY - 7f, 2f, 14f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - 7f, centerY - 1f, 14f, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Munitions (en bas à droite)
        string reserveText = infiniteReserve ? "--" : reserveAmmo.ToString();
        string ammoText = ammoInMagazine + " / " + reserveText;
        Rect ammoRect = new Rect(Screen.width - 320f, Screen.height - 90f, 300f, 70f);
        DrawShadowedLabel(ammoRect, ammoText, ammoStyle, ammoInMagazine == 0 ? new Color(1f, 0.35f, 0.3f) : Color.white);

        // Messages sous le viseur
        Rect messageRect = new Rect(centerX - 200f, centerY + 40f, 400f, 40f);
        if (reloading)
        {
            DrawShadowedLabel(messageRect, "Rechargement...", messageStyle, Color.white);
            float progress = Mathf.Clamp01((Time.time - reloadStartTime) / reloadTime);
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(centerX - 60f, centerY + 82f, 120f, 6f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.75f, 0.3f);
            GUI.DrawTexture(new Rect(centerX - 60f, centerY + 82f, 120f * progress, 6f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        else if (Time.time < emptyMessageUntil)
        {
            DrawShadowedLabel(messageRect, "Plus de munitions", messageStyle, new Color(1f, 0.35f, 0.3f));
        }
        else if (ammoInMagazine <= magazineSize / 4 && ammoInMagazine > 0 && (infiniteReserve || reserveAmmo > 0))
        {
            DrawShadowedLabel(messageRect, "R : recharger", messageStyle, new Color(1f, 0.85f, 0.4f));
        }
    }

    private void EnsureStyles()
    {
        if (ammoStyle != null)
            return;

        ammoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 42,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.LowerRight
        };
        messageStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };
    }

    private static void DrawShadowedLabel(Rect rect, string text, GUIStyle style, Color color)
    {
        style.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
    }
}
