using UnityEngine;

/// <summary>Cible simple pour tester les tirs dans MainGame. Elle réapparaît après quelques secondes.</summary>
public class ShootingTarget : MonoBehaviour
{
    [SerializeField, Min(1f)] private float health = 100f;
    [SerializeField, Min(0.5f)] private float respawnDelay = 3f;

    private float currentHealth;
    private Renderer[] renderers;
    private Collider[] colliders;
    private Renderer flashRenderer;
    private Color originalColor;

    private void Awake()
    {
        if (transform.childCount == 0)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Low Poly Target";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(1.1f, 1.4f, 0.35f);
            visual.GetComponent<Renderer>().sharedMaterial = LowPolyUtil.CreateMaterial(new Color(0.9f, 0.48f, 0.12f));
        }

        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
        flashRenderer = renderers.Length > 0 ? renderers[0] : null;
        if (flashRenderer != null)
            originalColor = flashRenderer.material.color;
        currentHealth = health;
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f)
            return;

        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            SetAlive(false);
            Invoke(nameof(Respawn), respawnDelay);
            return;
        }

        if (flashRenderer != null)
        {
            flashRenderer.material.color = Color.red;
            CancelInvoke(nameof(RestoreColor));
            Invoke(nameof(RestoreColor), 0.12f);
        }
    }

    private void Respawn()
    {
        currentHealth = health;
        RestoreColor();
        SetAlive(true);
    }

    private void SetAlive(bool alive)
    {
        foreach (Renderer targetRenderer in renderers)
            targetRenderer.enabled = alive;
        foreach (Collider targetCollider in colliders)
            targetCollider.enabled = alive;
    }

    private void RestoreColor()
    {
        if (flashRenderer != null)
            flashRenderer.material.color = originalColor;
    }
}
