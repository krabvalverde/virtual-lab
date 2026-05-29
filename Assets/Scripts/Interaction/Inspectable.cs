using UnityEngine;

[DisallowMultipleComponent]
public class Inspectable : MonoBehaviour
{
    public GlasswareInfo info;
    public Renderer targetRenderer;
    public Material outlineMaterial;

    private Material[] originalMaterials;
    private Material[] highlightedMaterials;
    private bool isHighlighted;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (info == null)
        {
            Debug.LogWarning($"Inspectable em {name} sem GlasswareInfo atribuído.", this);
        }
        if (outlineMaterial == null)
        {
            Debug.LogWarning($"Inspectable em {name} sem outlineMaterial atribuído.", this);
        }
        if (targetRenderer != null)
        {
            originalMaterials = targetRenderer.sharedMaterials;
            if (outlineMaterial != null)
            {
                highlightedMaterials = new Material[originalMaterials.Length + 1];
                System.Array.Copy(originalMaterials, highlightedMaterials, originalMaterials.Length);
                highlightedMaterials[originalMaterials.Length] = outlineMaterial;
            }
        }
    }

    public void SetHighlight(bool on)
    {
        if (targetRenderer == null || outlineMaterial == null) return;
        if (on == isHighlighted) return;

        targetRenderer.sharedMaterials = on ? highlightedMaterials : originalMaterials;
        isHighlighted = on;
    }
}
