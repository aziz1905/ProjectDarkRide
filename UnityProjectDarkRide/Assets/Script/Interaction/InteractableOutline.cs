using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Komponen Garis Luar (Outline Highlight) Presisi Tinggi.
/// Otomatis membingkai fisik 3D Mesh dengan garis presisi 3.0px saat ditatap mata pemain.
/// </summary>
public class InteractableOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] [Range(0.5f, 10f)] private float outlineWidth = 3.0f;

    private List<Renderer> renderers = new List<Renderer>();
    private List<Material[]> originalMaterials = new List<Material[]>();
    private List<Material[]> outlinedMaterials = new List<Material[]>();
    private Material outlineMaterial;
    private bool isOutlined = false;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeOutline();
    }

    public void InitializeOutline()
    {
        renderers.Clear();
        originalMaterials.Clear();
        outlinedMaterials.Clear();

        Renderer[] allRends = GetComponentsInChildren<Renderer>(true);

        Shader outlineShader = Shader.Find("Custom/URPOutline");
        if (outlineShader == null)
        {
            outlineShader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        outlineMaterial = new Material(outlineShader);
        outlineMaterial.SetColor("_OutlineColor", outlineColor);
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineMaterial.SetColor("_BaseColor", outlineColor);

        foreach (Renderer rend in allRends)
        {
            if (rend is ParticleSystemRenderer) continue;
            
            // Kecualikan BolaPijar di dalam bohlam
            if (rend.gameObject.name.ToLower().Contains("bolapijar")) continue;

            renderers.Add(rend);

            Material[] origMats = rend.materials; // Ambil instance material aktif
            originalMaterials.Add(origMats);

            Material[] newMats = new Material[origMats.Length + 1];
            for (int i = 0; i < origMats.Length; i++)
            {
                newMats[i] = origMats[i];
            }
            newMats[origMats.Length] = outlineMaterial;
            outlinedMaterials.Add(newMats);
        }

        isInitialized = true;
    }

    public void RefreshMaterials()
    {
        isInitialized = false;
        InitializeOutline();
        if (isOutlined)
        {
            SetOutlineActive(true);
        }
    }

    public void SetOutlineActive(bool active)
    {
        if (!isInitialized) InitializeOutline();

        if (isOutlined == active) return;
        isOutlined = active;

        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null && i < outlinedMaterials.Count && i < originalMaterials.Count)
            {
                renderers[i].materials = isOutlined ? outlinedMaterials[i] : originalMaterials[i];
            }
        }
    }

    private void OnDisable()
    {
        SetOutlineActive(false);
    }
}
