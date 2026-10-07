using UnityEngine;

/// <summary>Cible simple pour tester les tirs dans MainGame.</summary>
public class ShootingTarget : MonoBehaviour
{
    [SerializeField, Min(1f)] private float health = 100f;
    private Renderer targetRenderer;
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
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            var material = new Material(shader);
            material.color = new Color(0.9f, 0.48f, 0.12f);
            visual.GetComponent<Renderer>().sharedMaterial = material;
        }
        targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer != null)
            originalColor = targetRenderer.material.color;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (targetRenderer != null)
        {
            targetRenderer.material.color = Color.red;
            CancelInvoke(nameof(RestoreColor));
            Invoke(nameof(RestoreColor), 0.12f);
        }
    }

    private void RestoreColor()
    {
        if (targetRenderer != null)
            targetRenderer.material.color = originalColor;
    }
}
