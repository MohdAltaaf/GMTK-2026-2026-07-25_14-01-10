using UnityEngine;

namespace FastFPSMovement.Utilities
{
    /// <summary>
    /// Assigns a simple, pipeline-appropriate material to this object's MeshRenderer,
    /// both at runtime and in the editor. This is what actually fixes the placeholder
    /// player showing up pink: instead of hardcoding a Built-in Render Pipeline material
    /// into the prefab (which breaks under URP/HDRP), this looks up whichever Lit shader
    /// is actually available in your project and builds a small material from it.
    /// Purely cosmetic and safe to remove once you swap in your own model/materials.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public class AutoPipelineMaterial : MonoBehaviour
    {
        [Tooltip("Base color for the generated material.")]
        [SerializeField] private Color color = new Color(0.2f, 0.55f, 0.85f);

        [Tooltip("Surface smoothness/glossiness of the generated material.")]
        [Range(0f, 1f)]
        [SerializeField] private float smoothness = 0.35f;

        private void OnEnable()
        {
            ApplyMaterial();
        }

        private void ApplyMaterial()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                return;
            }

            // Try shaders in priority order: URP, HDRP, then Built-in Standard, then a
            // guaranteed-to-exist fallback so this never leaves the object with nothing.
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("HDRP/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                return;
            }

            var material = new Material(shader) { hideFlags = HideFlags.DontSave };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }
            else if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            meshRenderer.sharedMaterial = material;
        }
    }
}
